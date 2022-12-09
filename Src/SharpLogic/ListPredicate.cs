namespace SharpLogic;

public class ListPredicate : Predicate
{
    public ListPredicate(string functor, TermValue[] args)
        : base(functor, args)
    { }

    public override Term CloneWithNewArgs(TermValue[] args)
    {
        return new ListPredicate(Functor, args);
    }
}