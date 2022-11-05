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
        var sb = new StringBuilder(opCode.ToString());
        for (int i = 0; i < arguments.Length; i++) sb.Append(' ').Append(arguments[i]);
        Console.WriteLine(sb);

        _code.Add((byte)opCode);
        for (var i = 0; i < arguments.Length; i++) _code.Add(arguments[i]);
    }
}