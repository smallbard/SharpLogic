using System.ComponentModel.DataAnnotations.Schema;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic.ByteCodeVM.Compilation;

public ref struct CompilationContext
{
    public CompilationContext(ValueConstants valueConstants, ManagedConstants managedConstants, Span<byte> byte5, bool inQuery)
    {
        Code = new ByteCodeContainer();
        ValueConstants = valueConstants;
        ManagedConstants = managedConstants;
        Byte5 = byte5;
        InQuery = inQuery;
        Head = Array.Empty<TermValue>();
        Offsets = new List<(int Offset, Term Term)>();
    }

    public CompilationContext(CompilationContext ctx, Span<byte> byte5)
    {
        Code = ctx.Code;
        ValueConstants = ctx.ValueConstants;
        ManagedConstants = ctx.ManagedConstants;
        Byte5 = byte5;
        Head = ctx.Head;
        FreeVariables = ctx.FreeVariables;
        FreeRegister = ctx.FreeRegister;
        InQuery = ctx.InQuery;
        Offsets = ctx.Offsets;
    }

    public CompilationContext(CompilationContext ctx, bool inQuery)
        : this(ctx, ctx.Byte5)
    {
        InQuery = inQuery;
    }

    public ByteCodeContainer Code { get; }

    public List<(int Offset, Term Term)> Offsets { get; }

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