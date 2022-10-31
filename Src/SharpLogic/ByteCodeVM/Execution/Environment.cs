namespace SharpLogic.ByteCodeVM.Execution;

public class Environment
{
    public Environment(Environment? previousEnvironment)
    {
        Registers = new Registers();
        PreviousEnvironment = previousEnvironment;
        CP = int.MaxValue;
    }

    public Environment? PreviousEnvironment { get; init; }

    public Registers Registers { get; }

    public int CP { get; set; }

    public Stack<int>? Choices { get; set; }

    public bool InNegation { get; set; }
}