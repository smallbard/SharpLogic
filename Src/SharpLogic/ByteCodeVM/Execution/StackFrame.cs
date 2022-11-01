namespace SharpLogic.ByteCodeVM.Execution;

public class StackFrame
{
    public StackFrame(StackFrame? previousStackFrame)
    {
        Registers = new Registers();
        PreviousStackFrame = previousStackFrame;
        CP = int.MaxValue;
    }

    public StackFrame? PreviousStackFrame { get; init; }

    public Registers Registers { get; }

    public int CP { get; set; }

    public Stack<int>? Choices { get; set; }

    public bool InNegation { get; set; }
}