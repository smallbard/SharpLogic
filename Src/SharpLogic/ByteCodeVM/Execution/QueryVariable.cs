namespace SharpLogic.ByteCodeVM.Execution;

public class QueryVariable
{
    private StackFrame? _bindStackFrame;

    public QueryVariable(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    public object? Value { get; private set; }

    public bool IsBound => _bindStackFrame != null;

    public void Bind(object? value, StackFrame currentStackFrame)
    {
        if (IsBound) throw new SharpLogicException("Variable already bound.");

        Value = value;
        _bindStackFrame = currentStackFrame;
    }

    public void Unbind(StackFrame stackFrame)
    {
        if (stackFrame == _bindStackFrame)
        {
            Value = null;
            _bindStackFrame = null;
        }
    }
}