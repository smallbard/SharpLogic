using SharpLogic.ByteCodeVM.Compilation;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic.ByteCodeVM.Execution;

public class ExecutionCodeContainer
{
    private static readonly int[] OpCodeArgumentSizes = new[]
    {
        2, //UnValCst
        2, //UnRefCst
        5, //UnValCstLgIdx
        5, //UnRefCstLgIdx
        1, //UnTrue
        1, //UnFalse
        1, //UnNull
        2, //UnifyReg
        1, //UnifyEmpty
        2, //UnifyHead
        3, //UnifyNth
        2, //UnifyTail
        5, //UnifyLen

        2, //StackPxToAy
        0, //NewEnvironment
        4, //Goal
        0, //Proceed
        0, //Fail
        3, //GreaterThan
        3, //LessThan
        3, //GreaterThanOrEqual
        3, //LessThanOrEqual
        3, //Equal
        3, //NotEqual

        3, //Add
        3, //Substract
        3, //Multiply
        3, //Divide
        3, //Modulus

        0, //Cut
        5, //NewVar
        5, //OfType
        6, //MbAccess

        4, //Asserta
        4, //Assertz
    };

    private readonly ClausesIndex _clausesIndex;
    private readonly ManagedConstants _managedConstants;
    private readonly ValueConstants _valueConstants;
    private readonly Compiler _compiler;

    public ExecutionCodeContainer(ClausesIndex clausesIndex, ReadOnlyMemory<byte> queryCode, ManagedConstants managedConstants, ValueConstants valueConstants)
    {
        _clausesIndex = clausesIndex;
        _managedConstants = managedConstants;
        _valueConstants = valueConstants;
        _compiler = new Compiler();

        StartPoint = new InstructionPointer { P = 0, Code = queryCode };
    }

    public InstructionPointer StartPoint  { get; }

    public OpCode GetInstruction(InstructionPointer ip, out ReadOnlySpan<byte> arguments)
    {
        var p = ip.P;
        var code = ip.Code.Span;
        
        var opCode = code[p];
        var argumentsSize = OpCodeArgumentSizes[opCode];

        arguments = argumentsSize == 0 ? Span<byte>.Empty : code.Slice(p + 1, argumentsSize);

        return (OpCode)opCode;
    }

    public IEnumerable<InstructionPointer> GetOffsets(string functor, Registers registers)
    {
        return _clausesIndex.GetOffsets(functor, registers);
    }

    public void Asserta(Term t)
    {
        var (codeContainer, offsets) = _compiler.Compile(new[] { t }, _valueConstants, _managedConstants);
        var code = codeContainer.Code.Slice(0, codeContainer.CodeLength);

        _clausesIndex.AddOffsets(offsets, code, IndexingMode.Insert);
    }

    public void Assertz(Term t)
    {
        var (codeContainer, offsets) = _compiler.Compile(new[] { t }, _valueConstants, _managedConstants);
        var code = codeContainer.Code.Slice(0, codeContainer.CodeLength);

        _clausesIndex.AddOffsets(offsets, code, IndexingMode.Append);
    }
}