namespace SharpLogic;

public class Predicate : Term
{
    public Predicate(string functor, TermValue[] args)
        : base(functor, args)
    {

    }

    public override Term CloneWithNewArgs(TermValue[] args)
    {
        return new Predicate(Functor, args);
    }
}