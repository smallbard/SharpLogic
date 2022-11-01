namespace SharpLogic.ByteCodeVM.Execution;

public class RegisterValue
{
    private object? _value;

    public RegisterValueType Type { get; private set; }

    public object? Value
    {
        get => _value;
        set
        {
            _value = value;
            if (value is QueryVariable)
                Type = RegisterValueType.Variable;
            else
                Type = RegisterValueType.Constant;
        }
    }
}

public enum RegisterValueType
{
    Unbound,
    Constant,
    Variable,
}