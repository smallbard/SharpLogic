namespace SharpLogic.ByteCodeVM.Execution;

public class QueryVariable
{
    private object? _value;
    private StackFrame? _bindStackFrame;
    private List<QueryVariable>? _equivalentVariables;

    public QueryVariable(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    public object? Value 
    { 
        get
        {
            if (!IsBound) throw new SharpLogicException("Variable is unbound.");
            return _value;
        }
        private set => _value = value;
    }

    public bool IsBound => _bindStackFrame != null;

    public void Bind(object? value, StackFrame currentStackFrame)
    {
        if (IsBound) throw new SharpLogicException("Variable already bound.");

        Value = value;
        _bindStackFrame = currentStackFrame;

        if (_equivalentVariables != null)
            foreach(var v in _equivalentVariables.Where(ev => !ev.IsBound)) v.Bind(value, currentStackFrame);
    }

    public void Unbind(StackFrame stackFrame)
    {
        if (stackFrame == _bindStackFrame)
        {
            Value = null;
            _bindStackFrame = null;

            if (_equivalentVariables != null)
                foreach(var v in _equivalentVariables.Where(ev => ev != this)) v.Unbind(stackFrame);
        }
    }

    public static void MakeEquivalent(QueryVariable v1, QueryVariable v2, StackFrame currentStackFrame)
    {
        v1._equivalentVariables = v2._equivalentVariables = 
            (v1._equivalentVariables ?? Enumerable.Empty<QueryVariable>())
            .Union(v2._equivalentVariables ?? Enumerable.Empty<QueryVariable>())
            .Union(new[] { v1, v2 }).Distinct().ToList();
    }
}