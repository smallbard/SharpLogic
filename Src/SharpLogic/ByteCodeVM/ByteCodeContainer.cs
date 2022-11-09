using System.Text;

namespace SharpLogic.ByteCodeVM;

public class ByteCodeContainer
{
    private const int cstCodeBufferSize = 500;

    private List<byte> _code;

    public ByteCodeContainer()
    {
        _code = new List<byte>(cstCodeBufferSize);
    }

    public ReadOnlyMemory<byte> Code => _code.ToArray();

    public int CodeLength => _code.Count;

    public void AppendOpCode(OpCode opCode, Span<byte> arguments)
    {
        _code.Add((byte)opCode);
        for (var i = 0; i < arguments.Length; i++) _code.Add(arguments[i]);
    }
}