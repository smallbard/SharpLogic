namespace SharpLogic;

public class Rule : Term
{
    public Rule(string functor, TermValue[] head, TermValue[] args)
        : base(functor, args)
    {
        Head = head;

        foreach (var arg in Head) arg.Parent = this;
    }

    public TermValue[] Head { get; }
}