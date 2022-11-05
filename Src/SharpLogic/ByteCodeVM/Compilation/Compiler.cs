using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace SharpLogic.ByteCodeVM.Compilation;

public class Compiler
{
    public (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) Compile(IEnumerable<Term> terms, ValueConstants valueConstants, ManagedConstants managedConstants, bool inQuery = false)
    {
        Console.WriteLine("Compilation :");

        var functorOffsets = new Dictionary<string, Dictionary<int, List<int>>>();
        GetOffsetsDelegate getOffsets = (functor, arity) => 
            !functorOffsets.TryGetValue(functor, out var arityOffsets) || !arityOffsets.TryGetValue(arity, out var offsets) 
                ? Enumerable.Empty<int>() : offsets;

        var context = new CompilationContext(functorOffsets, valueConstants, managedConstants, stackalloc byte[5], inQuery);
        
        foreach (var term in terms)
        {
            var result = Compile(term, ref context);
            if (!result.Succeed) throw new SharpLogicException(result.ErrorMessage!);
        }

        return (context.Code, getOffsets);
    }

    private CompilationResult Compile(TermValue tv, ref CompilationContext context) => tv switch
    {
        Rule r => CompileRule(r, ref context),
        Term{Parent: null} t => CompileFact(t, ref context),
        Predicate p => CompilePredicate(p, ref context),        
        Term t => CompileGoal(t, context),
        _ => CompilationResult.Error($"Invalid {tv.GetType()} {tv}")
    };

    private CompilationResult CompileRule(Rule r, ref CompilationContext context)
    {
        context.Head = r.Head;
        if (!context.InQuery) context.AddOffset(r.Functor, r.Head.Length);

        // Unify head constants
        for (var i = 0; i < context.Head.Length; i++)
            if (r.Head[i].GetType() == typeof(TermValue))
            {
                var cr = CompileUnify(context.Head[i], (byte)i, context);
                if (!cr.Succeed) return cr;
            }

        var goals = r.Args.OfType<Term>();

        // Variable declarations
        context.FreeVariables = new Dictionary<string, byte>();
        context.FreeRegister = context.Head.Length;

        foreach (var variable in goals.SelectMany(g => g.Args).Where(arg => arg is Variable).Select(arg => (Variable)arg)
            .Union(goals.SelectMany(g => g.Args).Where(arg => arg is Predicate p && p.Functor == "Assignment").Select(p => (Variable)((Predicate)p).Args[0]))) // because of dynamic cache, assignment is given instead of variable
            if (!context.FreeVariables.ContainsKey(variable.VariableName) && Array.IndexOf(context.Head, variable) == -1)
            {
                context.Code.AppendOpCode(OpCode.NewVar, WriteIntByte(context.Byte5, context.ManagedConstants.AddConstant(variable.VariableName),(byte)context.FreeRegister));
                context.FreeVariables[variable.VariableName] = (byte)context.FreeRegister++;
            }

        foreach (var goal in goals)
        {
            if (goal.Args.Length > 255) return CompilationResult.Error("Goal can't have more than 255 arguments.");

            var cr = Compile(goal, ref context);
            if (!cr.Succeed) return cr;
        }

        // duplicate head terms
        var dupHeadTerms = context.Head.Select((h, i) => (h, i)).GroupBy(hi => hi.h).Where(hig => hig.Count() > 1);
        if (dupHeadTerms.Any())
        {
            var byte2 = context.Byte5.Slice(0, 2);
            foreach (var dupHeadTerm in dupHeadTerms)
            {
                var regToDup = dupHeadTerm.First().i;
                byte2[0] = (byte)regToDup;
                foreach (var regDest in dupHeadTerm.Skip(1).Select(hi => hi.i))
                {
                    byte2[1] = (byte)regDest;
                    context.Code.AppendOpCode(OpCode.UnifyReg, byte2);
                }
            }
        }

        if (!context.InQuery) context.Code.AppendOpCode(OpCode.Proceed, Span<byte>.Empty);

        return CompilationResult.Success;
    }

    private CompilationResult CompileGoal(Term goal, in CompilationContext context)
    {
        context.Code.AppendOpCode(OpCode.NewEnvironment, Span<byte>.Empty);

        for (var i = 0; i < goal.Args.Length; i++)
        {
            var arg = goal.Args[i];
            if (arg is Variable v)
            {
                var cr = CompileVariable(v, (byte)i, context);
                if (!cr.Succeed) return cr;
            }
            else if (arg is Predicate assign && assign.Functor == "Assignment") // because of dynamic cache, assignment is given instead of variable
            {
                var cr = CompileVariable((Variable)assign.Args[0], (byte)i, context);
                if (!cr.Succeed) return cr;
            }
            else if (arg is Predicate p)
            {
                throw new NotImplementedException();
            }
            else if (arg is Term)
                return CompilationResult.Error("A term can't be a argument of another term. A predicate should be used.");
            else
            {
                var cr = CompileUnify(arg, (byte)i, context);
                if (!cr.Succeed) return cr;
            }
        }

        var byte4 = context.Byte5.Slice(0, 4);
        WriteInt(byte4, context.ManagedConstants.AddConstant(goal.Functor));
        context.Code.AppendOpCode(OpCode.Goal, byte4);

        return CompilationResult.Success;
    }

    private CompilationResult CompileVariable(Variable variable, byte aYIndex, in CompilationContext context)
    {
        var byte2 = context.Byte5.Slice(0, 2);
        var registerIndexInt = Array.IndexOf(context.Head, variable);
        var registerIndex = (byte)registerIndexInt;
        if (registerIndexInt == -1 && !context.FreeVariables!.TryGetValue(variable.VariableName, out registerIndex)) return CompilationResult.Error($"Variable {variable.VariableName} shoulde have been already define.");

        byte2[0] = (byte)registerIndex;
        byte2[1] = (byte)aYIndex;
        context.Code.AppendOpCode(OpCode.StackPxToAy, byte2);

        return CompilationResult.Success;
    }

    private CompilationResult CompileFact(Term t, ref CompilationContext context)
    {
        if (t.Args.Any(a => a is Variable)) // body less rule
            return CompileRule(new Rule(t.Functor, t.Args, Array.Empty<TermValue>()), ref context);

        context.AddOffset(t.Functor, t.Args.Length);

        Span<byte> byte5 = stackalloc byte[5];

        for (var i = 0; i < t.Args.Length; i++)
        {
            var cr = CompileUnify(t.Args[i], (byte)i, context);
            if(!cr.Succeed) return cr;
        }
         
        context.Code.AppendOpCode(OpCode.Proceed, Span<byte>.Empty);

        return CompilationResult.Success;
    }

    private CompilationResult CompileUnify(TermValue tv, byte rxIndex, in CompilationContext context) => tv.Value switch
    {
        null => context.AppendOpCode(OpCode.UnNull, Slice(context.Byte5, (byte)rxIndex)),
        bool b => context.AppendOpCode(b ? OpCode.UnTrue : OpCode.UnFalse, Slice(context.Byte5, (byte)rxIndex)),
        Variable or Term => CompilationResult.Error($"A value was expected, not a {tv.GetType().Name} : {tv}."),
        _ => context.ValueConstants.TryAddBoxedConstant(tv.Value, out var index) ? 
            CompileUnifyConstant(true, rxIndex, index, context) : 
            CompileUnifyConstant(false, rxIndex, context.ManagedConstants.AddConstant(tv.Value), context)
    };

    private CompilationResult CompileUnifyConstant(bool isValue, byte rxIndex, int cstIndex, in CompilationContext context) => (isValue, cstIndex) switch
    {
        (true, > 254) => context.AppendOpCode(OpCode.UnValCstLgIdx, WriteIntByte(context.Byte5, cstIndex, rxIndex)),
        (true, _) => context.AppendOpCode(OpCode.UnValCst, Slice(context.Byte5, (byte)cstIndex, rxIndex)),
        (false, > 254) => context.AppendOpCode(OpCode.UnRefCstLgIdx, WriteIntByte(context.Byte5, cstIndex, rxIndex)),
        (false, _) => context.AppendOpCode(OpCode.UnRefCst, Slice(context.Byte5, (byte)cstIndex, rxIndex)),
    };

    private CompilationResult CompilePredicate(Predicate p, ref CompilationContext context) => p.Functor switch
    {
        nameof(IPredicates.Fail) => context.AppendOpCode(OpCode.Fail, Span<byte>.Empty),
        nameof(IPredicates.Not) => CompileNot(p, ref context),
        "GreaterThan" or "LessThan" or "GreaterThanOrEqual" or "LessThanOrEqual" or "Equal" or "NotEqual" or "Modulus" or "Add" or "Substract" or "Multiply" or "Divide" => CompileBinaryOperator(p, ref context),
        nameof(IPredicates.Cut) => context.AppendOpCode(OpCode.Cut, Span<byte>.Empty),
        nameof(IPredicates.Is) => CompileIs(p, ref context),
        nameof(IPredicates.OfType) => CompileOfType(p, context),
        "MemberAccess" => CompileMemberAccess(p, ref context),

        _ => CompilationResult.Error($"Unsupported predicate {p.Functor}.")
    };

    private CompilationResult CompileNot(Predicate p, ref CompilationContext context)
    {
        context.Code.AppendOpCode(OpCode.SwitchNot, Span<byte>.Empty);
        
        var cr = CompileGoal((Term)p.Args[0], context);
        if (!cr.Succeed) return cr;

        context.Code.AppendOpCode(OpCode.SwitchNot, Span<byte>.Empty);

        return CompilationResult.Success;
    }

    private CompilationResult CompileBinaryOperator(Predicate p, ref CompilationContext context)
    {
        var byte3 = context.Byte5.Slice(0, 3);
        var ctxOperand = new CompilationContext(context, stackalloc byte[5]);
        for (var i = 0; i < p.Args.Length; i ++)
        {
            var arg = p.Args[i];
            if (arg is Variable v)
                byte3[i] = GetVariableRegIndex(v, ctxOperand);
            else if (arg is Predicate pArg)
            {
                CompilePredicate(pArg, ref ctxOperand);
                byte3[i] = (byte)(context.FreeRegister - 1);
            }
            else
            {
                CompileUnify(arg, (byte)context.FreeRegister, ctxOperand);
                byte3[i] = (byte)context.FreeRegister++;
            }
        }

        context.FreeRegister = ctxOperand.FreeRegister;

        byte3[2] = (byte)context.FreeRegister++;

        return context.AppendOpCode(Enum.Parse<OpCode>(p.Functor), byte3);
    }

    private CompilationResult CompileIs(Predicate p, ref CompilationContext context)
    {
        if (!(p.Args[0] is Variable assignedVar)) return CompilationResult.Error("Left operand of an assignment must be a variable.");

        var assignedVarReg = GetVariableRegIndex(assignedVar, context);

        if (p.Args[1] is Predicate assignedOperation)
        {
            var cr = CompilePredicate(assignedOperation, ref context);
            if (!cr.Succeed) return cr;

            var byte2 = context.Byte5.Slice(0, 2);
            byte2[0] = (byte)(context.FreeRegister - 1);
            byte2[1] = assignedVarReg;

            context.Code.AppendOpCode(OpCode.UnifyReg, byte2);
        }
        else if (p.Args[1] is Term)
            return CompilationResult.Error("Right operand of an assignment can't be a fact.");
        else if (p.Args[1] is Variable valueVar)
            throw new NotImplementedException();
        else if (p.Args[1] is TermValue cst)
            CompileUnify(cst,assignedVarReg, context);

        return CompilationResult.Success;
    }

    private CompilationResult CompileOfType(Predicate p, in CompilationContext context)
    {
        WriteInt(context.Byte5, (int)p.Args[1].Value!);
        context.Byte5[4] = GetVariableRegIndex((Variable)p.Args[0], context);

        return context.AppendOpCode(OpCode.OfType, context.Byte5);
    }

    private CompilationResult CompileMemberAccess(Predicate p, ref CompilationContext context)
    {
        var objRegister = 0;
        if (p.Args[0] is Predicate pOwner)
        {
            var cr = CompilePredicate(pOwner, ref context);
            if (!cr.Succeed) return cr;
            objRegister = context.FreeRegister - 1;
        }
        else if (p.Args[0] is Variable v)
            objRegister = GetVariableRegIndex(v, context);
        else
            CompileUnify(p.Args[0], (byte)(objRegister = context.FreeRegister++), context);

        var memberNameIndex = context.ManagedConstants.AddConstant(p.Args[1].Value!);

        Span<byte> byte6 = stackalloc byte[6];
        WriteInt(byte6, memberNameIndex);
        byte6[4] = (byte)objRegister;
        byte6[5] = (byte)context.FreeRegister++;

        context.Code.AppendOpCode(OpCode.MbAccess, byte6);

        return CompilationResult.Success;
    }

    private byte GetVariableRegIndex(Variable v, in CompilationContext context)
    {
         var varRegIndex = Array.IndexOf(context.Head, v);
         if (varRegIndex == -1) varRegIndex = context.FreeVariables![v.VariableName];
        return (byte)varRegIndex;
    }

    private Span<byte> Slice(Span<byte> byteSpan, params byte[] bytes)
    {
        var slice = byteSpan.Slice(0, bytes.Length);
        for (var i = 0; i < bytes.Length; i++) slice[i] = bytes[i];
        return slice;
    }

    private Span<byte> WriteIntByte(Span<byte> byte5, int i, byte b)
    {
         if (BitConverter.IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(byte5, i);
        else
            BinaryPrimitives.WriteInt32BigEndian(byte5, i);

        byte5[4] = b;

        return byte5;
    }

    private void WriteInt(Span<byte> bytes, int value)
    {
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        else
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
    }
}

public delegate IEnumerable<int> GetOffsetsDelegate(string functor, int arity);