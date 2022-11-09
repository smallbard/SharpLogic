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
    };

    private readonly (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) _factAndRule;
    private readonly ReadOnlyMemory<byte> _queryCode;

    public ExecutionCodeContainer((ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) factAndRule, ReadOnlyMemory<byte> queryCode)
    {
        _factAndRule = factAndRule;
        _queryCode = queryCode;

        CodeLength = _factAndRule.Code.CodeLength + queryCode.Length;
    }

    public int StartPoint => _factAndRule.Code.CodeLength;

    public int CodeLength { get; }

    public OpCode GetInstruction(int p, out ReadOnlySpan<byte> arguments)
    {
        var code = _factAndRule.Code.Code.Span;
        var realP = p;
        if (p >= _factAndRule.Code.CodeLength)
        {
            code = _queryCode.Span;
            realP = p - _factAndRule.Code.CodeLength;
        }

        var opCode = code[realP];
        var argumentsSize = OpCodeArgumentSizes[opCode];

        arguments = argumentsSize == 0 ? Span<byte>.Empty : code.Slice(realP + 1, argumentsSize);

        return (OpCode)opCode;
    }

    public IEnumerable<int> GetOffsets(string functor, int arity)
    {
        return _factAndRule.GetOffsets(functor, arity);
    }
}