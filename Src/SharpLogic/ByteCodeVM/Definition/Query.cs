using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography;

using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Definition;

public class Query<T> : IQuery<T>
{
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly FactAndRule _factAndRule;
    private readonly List<Term> _goals;
    private readonly ByteCodeContainer _queryCode;
    private readonly PredicateTranslator _predicateTranslator;
    private readonly Dictionary<string, byte> _variables;

    public Query(ValueConstants valueConstants, ManagedConstants managedConstants, FactAndRule factAndRule)
    {
        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _factAndRule = factAndRule;
        _goals = new List<Term>();
        _queryCode = new ByteCodeContainer();
        _variables = new Dictionary<string, byte>();

        _predicateTranslator = new PredicateTranslator(_queryCode, (Span<byte> byte5, TermValue[] head, Dictionary<string, byte> freeVariables, Term goal, ref byte freeRegister) => AppendGoal(byte5, goal, ref freeRegister), Array.Empty<TermValue>(), managedConstants, valueConstants, _variables);
    }

    public void AddTerm(Term t)
    {
        _goals.Add(t);
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (_goals.Count == 0) return Enumerable.Empty<T>().GetEnumerator();
        return CompileAndExecute();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private ByteCodeExecutor<T> CompileAndExecute()
    {
        Span<byte> byte5 = stackalloc byte[5];
        byte freeRegister = 0;

        foreach (var variable in _goals.SelectMany(g => g.Args).Where(arg => arg is Variable).Select(arg => (Variable)arg))
            if (!_variables.ContainsKey(variable.VariableName))
            {
                ByteCodeGen.WriteInt(byte5, _managedConstants.AddConstant(variable.VariableName));
                byte5[4] = freeRegister;
                _queryCode.AppendOpCode(OpCode.NewVar, byte5);
                _variables[variable.VariableName] = freeRegister++;
            }

        foreach (var goal in _goals)
        {
            if (goal.Args.Length > 255) throw new SharpLogicException("Goal can't have more than 255 arguments.");

            if (goal is Predicate p)
                _predicateTranslator.Translate(byte5, p, ref freeRegister);
            else
                AppendGoal(byte5, goal, ref freeRegister);
        }

        _queryCode.AppendOpCode(OpCode.Proceed, Span<byte>.Empty);

        return new ByteCodeExecutor<T>(_valueConstants, _managedConstants, _factAndRule, _queryCode.Code);
    }

    private void AppendGoal(Span<byte> byte5, Term goal, ref byte freeRegister)
    {
        _queryCode.AppendOpCode(OpCode.NewEnvironment, Span<byte>.Empty);

        for (var i = 0; i < goal.Args.Length; i++)
        {
            var arg = goal.Args[i];
            if (arg is Variable v)
                ByteCodeGen.PrepareVariableForGoalInQuery(byte5, _queryCode, _variables, _managedConstants, v, (byte)i, ref freeRegister);
            else if (arg is Predicate)
                throw new NotImplementedException();
            else
                ByteCodeGen.UnifyConstant(_queryCode, arg, i, byte5, _valueConstants, _managedConstants);
            
        }

        var byte4 = byte5.Slice(0, 4);
        ByteCodeGen.WriteInt(byte4, _managedConstants.AddConstant(goal.Functor));
        _queryCode.AppendOpCode(OpCode.Goal, byte4);
    }
}