using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using System.Xml;

namespace SharpLogic.ByteCodeVM.Definition;

public class FactAndRule : IFactAndRule
{
    private readonly Dictionary<string, Dictionary<int, List<int>>> _functorOffsets = new Dictionary<string, Dictionary<int, List<int>>>();
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly ByteCodeContainer _codeContainer;

    public FactAndRule(ValueConstants valueConstants, ManagedConstants managedConstants)
    {
        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _codeContainer = new ByteCodeContainer();
    }

    public ReadOnlyMemory<byte> Code => _codeContainer.Code;

    public IEnumerable<int> GetOffsets(string functor, int arity)
    {
        if (!_functorOffsets.TryGetValue(functor, out var arityOffsets) || !arityOffsets.TryGetValue(arity, out var offsets)) 
            return Enumerable.Empty<int>();
        return offsets;
    }

    public void AddRule(string functor, TermValue[] head, IEnumerable<Term> goals)
    {
        Span<byte> byte5 = stackalloc byte[5];
        
        AddOffset(functor, head.Length);

        for (var i = 0; i < head.Length; i++)
            if (head[i].GetType() == typeof(TermValue))
                ByteCodeGen.UnifyConstant(_codeContainer, head[i], i, byte5, _valueConstants, _managedConstants);

        var freeVariables = new Dictionary<string, byte>();
        var predicateTranslator = new PredicateTranslator(_codeContainer, AppendGoal, head, _managedConstants, _valueConstants, freeVariables);
        byte freeRegister = (byte)head.Length;

        foreach (var variable in goals.SelectMany(g => g.Args).Where(arg => arg is Variable).Select(arg => (Variable)arg)
            .Union(goals.SelectMany(g => g.Args).Where(arg => arg is Predicate p && p.Functor == "Assignment").Select(p => (Variable)((Predicate)p).Args[0]))) // because of dynamic cache, assignment is given instead of variable
            if (!freeVariables.ContainsKey(variable.VariableName) && Array.IndexOf(head, variable) == -1)
            {
                ByteCodeGen.WriteInt(byte5, _managedConstants.AddConstant(variable.VariableName));
                byte5[4] = freeRegister;
                _codeContainer.AppendOpCode(OpCode.NewVar, byte5);
                freeVariables[variable.VariableName] = freeRegister++;
            }

        foreach (var goal in goals)
        {
            if (goal.Args.Length > 255) throw new SharpLogicException("Goal can't have more than 255 arguments.");

            if (goal is Predicate p)
                predicateTranslator.Translate(byte5, p, ref freeRegister);
            else
                AppendGoal(byte5, head, freeVariables, goal, ref freeRegister);
        }

        // duplicate head term
        var dupHeadTerms = head.Select((h, i) => (h, i)).GroupBy(hi => hi.h).Where(hig => hig.Count() > 1);
        if (dupHeadTerms.Any())
        {
            var byte2 = byte5.Slice(0, 2);
            foreach (var dupHeadTerm in dupHeadTerms)
            {
                var regToDup = dupHeadTerm.First().i;
                byte2[0] = (byte)regToDup;
                foreach (var regDest in dupHeadTerm.Skip(1).Select(hi => hi.i))
                {
                    byte2[1] = (byte)regDest;
                    _codeContainer.AppendOpCode(OpCode.UnifyReg, byte2);
                }
            }
        }

        _codeContainer.AppendOpCode(OpCode.Proceed, Span<byte>.Empty);
    }

    public void AddFact(string functor, TermValue[] arguments)
    {
        if (arguments.Any(a => a is Variable))
        {
            // body less rule
            AddRule(functor, arguments, Enumerable.Empty<Term>());
            return;
        }

        AddOffset(functor, arguments.Length);

        Span<byte> byte5 = stackalloc byte[5];
        
        for (var i = 0; i < arguments.Length; i++) ByteCodeGen.UnifyConstant(_codeContainer, arguments[i], i, byte5, _valueConstants, _managedConstants);

        _codeContainer.AppendOpCode(OpCode.Proceed, Span<byte>.Empty);
    }

    private void AppendGoal(Span<byte> byte5, TermValue[] head, Dictionary<string, byte> freeVariables, Term goal, ref byte freeRegister)
    {
        _codeContainer.AppendOpCode(OpCode.NewEnvironment, Span<byte>.Empty);

        var predicateTranslator = new PredicateTranslator(_codeContainer, AppendGoal, head, _managedConstants, _valueConstants, freeVariables);

        for (var i = 0; i < goal.Args.Length; i++)
        {
            var arg = goal.Args[i];
            if (arg is Variable v)
                ByteCodeGen.PrepareVariableForGoalInRule(byte5, _codeContainer, head, freeVariables, _managedConstants, v, (byte)i, ref freeRegister);
            else if (arg is Predicate p && p.Functor == "Assignment") // because of dynamic cache, assignment is given instead of variable
                ByteCodeGen.PrepareVariableForGoalInRule(byte5, _codeContainer, head, freeVariables, _managedConstants, (Variable)p.Args[0], (byte)i, ref freeRegister);
            else if (arg is Term)
                throw new SharpLogicException("A term can't be a argument of another term. A predicate should be used.");
            else
                ByteCodeGen.UnifyConstant(_codeContainer, arg, i, byte5, _valueConstants, _managedConstants);
        }

        var byte4 = byte5.Slice(0, 4);
        ByteCodeGen.WriteInt(byte4, _managedConstants.AddConstant(goal.Functor));
        _codeContainer.AppendOpCode(OpCode.Goal, byte4);
    }

    private void AddOffset(string functor, int arity)
    {
        if (!_functorOffsets.TryGetValue(functor, out var offsetsByArity)) _functorOffsets[functor] = offsetsByArity = new Dictionary<int, List<int>>();
        if (!offsetsByArity.TryGetValue(arity, out var offsets)) offsetsByArity[arity] = offsets = new List<int>();

        offsets.Add(_codeContainer.CodeLength);
    }
}