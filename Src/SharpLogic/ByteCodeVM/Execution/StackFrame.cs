namespace SharpLogic.ByteCodeVM.Execution;

public class StackFrame
{
    public StackFrame(StackFrame? previousStackFrame)
    {
        Registers = new Registers();
        PreviousStackFrame = previousStackFrame;
        CP = InstructionPointer.Invalid;
    }

    public event EventHandler? VariablesUninstantiated;

    public StackFrame? PreviousStackFrame { get; init; }

    public Registers Registers { get; }

    public InstructionPointer CP { get; set; }

    public IEnumerator<InstructionPointer>? Choices { get; set; }

    public void UninstantiateVariables()
    {
        VariablesUninstantiated?.Invoke(this, EventArgs.Empty);
    }
}