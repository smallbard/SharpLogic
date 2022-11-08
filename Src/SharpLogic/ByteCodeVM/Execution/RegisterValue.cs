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
            Type = value is QueryVariable ? RegisterValueType.Variable : RegisterValueType.Constant;
        }
    }

    public object? RealValue
    {
        get
        {
            if (Type == RegisterValueType.Unbound) throw new SharpLogicException("Register must be bound.");

            if (Value is QueryVariable v)
            {
                if (!v.IsBound) throw new SharpLogicException($"Variable must be bound : '{v.Name}'.");

                if (v.Value is ValueTuple<ReadOnlyMemory<byte>, Type> cstVar)
                    return ValueConstants.GetRealValueConstant(cstVar);

                return v.Value;
            }

            if (Value is ValueTuple<ReadOnlyMemory<byte>, Type> cst)
                return ValueConstants.GetRealValueConstant(cst);

            return Value;
        }
    }
}

public enum RegisterValueType
{
    Unbound,
    Constant,
    Variable,
}