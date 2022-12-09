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
            if (!Instantiated) throw new SharpLogicException("Variable is uninstantiated.");
            return _value;
        }
        private set => _value = value;
    }

    public bool Instantiated => _bindStackFrame != null;

    public void Instantiate(object? value, StackFrame currentStackFrame)
    {
        if (Instantiated) throw new SharpLogicException("Variable already instantiated.");

        Value = value;
        _bindStackFrame = currentStackFrame;

        if (_equivalentVariables != null)
            foreach(var v in _equivalentVariables.Where(ev => !ev.Instantiated)) v.Instantiate(value, currentStackFrame);

        currentStackFrame.VariablesUninstantiated += (s, a) =>
        {
            Value = null;
            _bindStackFrame = null;
        };
    }

    public static void MakeEquivalent(QueryVariable v1, QueryVariable v2, StackFrame currentStackFrame)
    {
        v1._equivalentVariables = v2._equivalentVariables = 
            (v1._equivalentVariables ?? Enumerable.Empty<QueryVariable>())
            .Union(v2._equivalentVariables ?? Enumerable.Empty<QueryVariable>())
            .Union(new[] { v1, v2 }).Distinct().ToList();
    }
}