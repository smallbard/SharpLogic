namespace SharpLogic;

public interface IFactAndRule
{
    void AddRule(string functor, TermValue[] head, IEnumerable<Term> goals);

    void AddFact(string functor, TermValue[] arguments);
}