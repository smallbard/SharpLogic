namespace SharpLogic;

public class Rule : Term
{
    public Rule(string functor, TermValue[] head, TermValue[] args)
        : base(functor, args)
    {
        Head = head;
    }

    public TermValue[] Head { get; }
}