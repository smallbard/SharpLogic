namespace SharpLogic.ByteCodeVM.Execution;

public class Registers
{
    private readonly Dictionary<int, RegisterValue> _registers = new Dictionary<int, RegisterValue>();

    public RegisterValue this[int index] => _registers.ContainsKey(index) ? _registers[index] : _registers[index] = new RegisterValue();

    public int Count => _registers.Count;

    public IEnumerable<QueryVariable> GetVariables()
    {
        return _registers.Values.Where(rv => rv.Type == RegisterValueType.Variable).Select(rv => (QueryVariable)rv.Value!);
    }
}