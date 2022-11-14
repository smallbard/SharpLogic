using System.Linq.Expressions;

namespace SharpLogic;

public interface IPredicates
{
    Predicate Fail { get; }

    Predicate Cut { get; }

    Predicate Not(Term t);

    Predicate OfType<T>(Variable v);

    Predicate Is(Variable v, object value);

    //Predicate Capture<T>(TermValue tv, Action<T> exec);

    ListPredicate this[params object[] elements]{ get; }

    ListPredicate Empty { get; }

    ListPredicate Tail(object v);

    Predicate Asserta(Term t);

    Predicate Assertz(Term t);
}