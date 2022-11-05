namespace SharpLogic.ByteCodeVM.Compilation;

public ref struct CompilationContext
{
    private readonly Dictionary<string, Dictionary<int, List<int>>> _functorOffsets;

    public CompilationContext(Dictionary<string, Dictionary<int, List<int>>> functorOffsets, ValueConstants valueConstants, ManagedConstants managedConstants, Span<byte> byte5, bool inQuery)
    {
        _functorOffsets = functorOffsets;
        Code = new ByteCodeContainer();
        ValueConstants = valueConstants;
        ManagedConstants = managedConstants;
        Byte5 = byte5;
        InQuery = inQuery;
        Head = Array.Empty<TermValue>();
    }

    public CompilationContext(CompilationContext ctx, Span<byte> byte5)
    {
        _functorOffsets = ctx._functorOffsets;
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

    public ValueConstants ValueConstants { get; }

    public ManagedConstants ManagedConstants { get; }

    public Span<byte> Byte5 { get; }

    public bool InQuery { get; }

    public TermValue[] Head { get; set; }

    public Dictionary<string, byte>? FreeVariables { get; set; }

    public int FreeRegister { get; set; }

    public void AddOffset(string functor, int arity)
    {
        if (!_functorOffsets.TryGetValue(functor, out var offsetsByArity)) _functorOffsets[functor] = offsetsByArity = new Dictionary<int, List<int>>();
        if (!offsetsByArity.TryGetValue(arity, out var offsets)) offsetsByArity[arity] = offsets = new List<int>();

        offsets.Add(Code.CodeLength);
    }

    public CompilationResult AppendOpCode(OpCode opCode, Span<byte> arguments)
    {
        Code.AppendOpCode(opCode, arguments);
        return CompilationResult.Success;
    }
}