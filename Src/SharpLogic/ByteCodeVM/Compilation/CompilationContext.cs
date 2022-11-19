using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic.ByteCodeVM.Compilation;

public ref struct CompilationContext
{
    public CompilationContext(ClausesIndex clausesIndex, ValueConstants valueConstants, ManagedConstants managedConstants, Span<byte> byte5, bool inQuery)
    {
        ClausesIndex = clausesIndex;
        Code = new ByteCodeContainer();
        ValueConstants = valueConstants;
        ManagedConstants = managedConstants;
        Byte5 = byte5;
        InQuery = inQuery;
        Head = Array.Empty<TermValue>();
    }

    public CompilationContext(CompilationContext ctx, Span<byte> byte5)
    {
        ClausesIndex = ctx.ClausesIndex;
        Code = ctx.Code;
        ValueConstants = ctx.ValueConstants;
        ManagedConstants = ctx.ManagedConstants;
        Byte5 = byte5;
        Head = ctx.Head;
        FreeVariables = ctx.FreeVariables;
        FreeRegister = ctx.FreeRegister;
        InQuery = ctx.InQuery;
    }

    public ByteCodeContainer Code { get; }

    public ClausesIndex ClausesIndex { get; }

    public ValueConstants ValueConstants { get; }

    public ManagedConstants ManagedConstants { get; }

    public Span<byte> Byte5 { get; }

    public bool InQuery { get; }

    public TermValue[] Head { get; set; }

    public Dictionary<string, byte>? FreeVariables { get; set; }

    public int FreeRegister { get; set; }

    public CompilationResult AppendOpCode(OpCode opCode, Span<byte> arguments)
    {
        Code.AppendOpCode(opCode, arguments);
        return CompilationResult.Success;
    }
}