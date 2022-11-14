using SharpLogic.ByteCodeVM.Compilation;

public struct InstructionPointer
{
    required public int P;

    required public ReadOnlyMemory<byte> Code;

    public static InstructionPointer operator +(InstructionPointer ip, int offset)
    {
        var newP = ip.P + offset;
        if (newP >= ip.Code.Length) return Invalid;

        return new InstructionPointer { P = newP, Code = ip.Code };
    } 

    public static readonly InstructionPointer Invalid = new InstructionPointer { P = int.MaxValue, Code = ReadOnlyMemory<byte>.Empty };
}