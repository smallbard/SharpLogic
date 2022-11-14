using SharpLogic.ByteCodeVM.Compilation;

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
        0, //SwitchNot
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

    private readonly KbdCodeContainer _kbdCodeContainer;
    private readonly ManagedConstants _managedConstants;
    private readonly ValueConstants _valueConstants;
    private readonly Compiler _compiler;

    public ExecutionCodeContainer(KbdCodeContainer kbdCodeContainer, ReadOnlyMemory<byte> queryCode, ManagedConstants managedConstants, ValueConstants valueConstants)
    {
        _kbdCodeContainer = kbdCodeContainer;
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

    public IEnumerable<InstructionPointer> GetOffsets(string functor, int arity)
    {
        return _kbdCodeContainer.SelectMany(c => c.GetOffsets(functor, arity).Select(p => new InstructionPointer { P = p, Code = c.Code }));
    }

    public void Asserta(Term t)
    {
        var compiled = _compiler.Compile(new[] { t }, _valueConstants, _managedConstants);
        _kbdCodeContainer.InsertFirst((compiled.Code.Code.Slice(0, compiled.Code.CodeLength), compiled.GetOffsets));
    }

    public void Assertz(Term t)
    {
        var compiled = _compiler.Compile(new[] { t }, _valueConstants, _managedConstants);
        _kbdCodeContainer.InsertLast((compiled.Code.Code.Slice(0, compiled.Code.CodeLength), compiled.GetOffsets));
    }
}