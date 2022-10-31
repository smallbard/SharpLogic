namespace SharpLogic.ByteCodeVM.Execution;

public class RegisterValue
{
    public RegisterValueType Type { get; set; }

    public object? Value { get; set; }
}

public enum RegisterValueType
{
    Unbound,
    Constant,
    Variable,
}