using System.Diagnostics.Tracing;

namespace SharpLogic.ByteCodeVM.Definition;

public class PredicateTranslator
{
    private static readonly HashSet<string> _binaryOperators = new HashSet<string> { "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual", "Equal", "NotEqual", "Modulus", "Add", "Substract", "Multiply", "Divide"};

    private readonly ByteCodeContainer _codeContainer;
    private readonly AppendGoalDelegate _appendGoal;
    private readonly TermValue[] _head;
    private readonly ManagedConstants _managedConstants;
    private readonly ValueConstants _valueConstants;
    private readonly Dictionary<string, byte> _freeVariables;

    public PredicateTranslator(ByteCodeContainer codeContainer, AppendGoalDelegate appendGoal, TermValue[] head, ManagedConstants managedConstants, ValueConstants valueConstants, Dictionary<string, byte> freeVariables)
    {
        _codeContainer = codeContainer;
        _appendGoal = appendGoal;
        _head = head;
        _managedConstants = managedConstants;
        _valueConstants = valueConstants;
        _freeVariables = freeVariables;
    }

    public void Translate(Span<byte> byte5, Predicate p, ref byte freeRegister)
    {
        if (p.Functor == nameof(IPredicates.Fail))
            _codeContainer.AppendOpCode(OpCode.Fail, Span<byte>.Empty);
        else if (p.Functor == nameof(IPredicates.Not))
        {
            _codeContainer.AppendOpCode(OpCode.SwitchNot, Span<byte>.Empty);
            _appendGoal(byte5, _head, _freeVariables, (Term)p.Args[0].Value!, ref freeRegister);
            _codeContainer.AppendOpCode(OpCode.SwitchNot, Span<byte>.Empty);
        }
        else if (_binaryOperators.Contains(p.Functor))
            BinaryOperator(byte5, p, Enum.Parse<OpCode>(p.Functor), ref freeRegister);
        else if (p.Functor == nameof(IPredicates.Cut))
            _codeContainer.AppendOpCode(OpCode.Cut, Span<byte>.Empty);
        else if (p.Functor == "Assignment")
            Assignment(byte5, p, ref freeRegister);
        else if (p.Functor == nameof(IPredicates.OfType))
        {
            ByteCodeGen.WriteInt(byte5, (int)p.Args[1].Value!);
            byte5[4] = GetVariableRegIndex((Variable)p.Args[0]);

            _codeContainer.AppendOpCode(OpCode.OfType, byte5);
        }
        else if (p.Functor == "MemberAccess")
        {
            var objRegister = 0;
            if (p.Args[0] is Predicate)
                objRegister = freeRegister - 1;
            else if (p.Args[0] is Variable v)
                objRegister = GetVariableRegIndex(v);
            else
                ByteCodeGen.UnifyConstant(_codeContainer, p.Args[0], objRegister = freeRegister++, byte5, _valueConstants, _managedConstants);

            var memberNameIndex = _managedConstants.AddConstant(p.Args[1].Value!);

            Span<byte> byte6 = stackalloc byte[6];
            ByteCodeGen.WriteInt(byte6, memberNameIndex);
            byte6[4] = (byte)objRegister;
            byte6[5] = freeRegister++;

            _codeContainer.AppendOpCode(OpCode.MbAccess, byte6);
        }
        else
            throw new SharpLogicException($"Unsupported predicate {p.Functor}.");
    }

    private byte GetVariableRegIndex(Variable v)
    {
         var varRegIndex = -1;
         if (_head != null) varRegIndex = Array.IndexOf(_head, v);
         if (varRegIndex == -1) varRegIndex = _freeVariables[v.VariableName];
        return (byte)varRegIndex;
    }

    private void Assignment(Span<byte> byte5, Predicate p, ref byte freeRegister)
    {
        if (!(p.Args[0] is Variable assignedVar)) throw new SharpLogicException("Left operand of an assignment must be a variable.");

        var assignedVarReg = GetVariableRegIndex(assignedVar);

        if (p.Args[1] is Predicate assignedOperation)
        {
            Translate(byte5, assignedOperation, ref freeRegister);

            var byte2 = byte5.Slice(0, 2);
            byte2[0] = (byte)(freeRegister - 1);
            byte2[1] = assignedVarReg;

            _codeContainer.AppendOpCode(OpCode.UnifyReg, byte2);
        }
        else if (p.Args[1] is Term)
            throw new SharpLogicException("Right operand of an assignment can't be a fact.");
        else if (p.Args[1] is Variable valueVar)
            throw new NotImplementedException();
        else if (p.Args[1] is TermValue cst)
            ByteCodeGen.UnifyConstant(_codeContainer, cst, assignedVarReg, byte5, _valueConstants, _managedConstants);
    }
    
    private void BinaryOperator(Span<byte> byte5, Predicate p, OpCode operatorCode, ref byte freeRegister)
    {
        var byte3 = byte5.Slice(0, 3);
        Span<byte> nbyte5 = stackalloc byte[5];
        
        for (var i = 0; i < p.Args.Length; i ++)
        {
            var arg = p.Args[i];
            if (arg is Variable v)
                byte3[i] = GetVariableRegIndex(v);
            else if (arg is Predicate pArg && _binaryOperators.Contains(pArg.Functor))
            {
                BinaryOperator(nbyte5, pArg, Enum.Parse<OpCode>(pArg.Functor), ref freeRegister);
                byte3[i] = (byte)(freeRegister - 1);
            }
            else
            {
                throw new NotImplementedException();
                //ByteCodeGen.UnifyConstant(_codeContainer, arg, i, byte5, _valueConstants, _managedConstants);
            }
        }

        byte3[2] = freeRegister++;

        _codeContainer.AppendOpCode(operatorCode, byte3);
    }

    public delegate void AppendGoalDelegate(Span<byte> byte5, TermValue[] head, Dictionary<string, byte> freeVariables, Term goal, ref byte freeRegister);
}