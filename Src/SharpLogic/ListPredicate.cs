namespace SharpLogic;

public class ListPredicate : Predicate
{
    public ListPredicate(string functor, TermValue[] args)
        : base(functor, args)
    { }
}