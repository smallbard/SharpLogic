using System.Linq.Expressions;

namespace SharpLogic;

public interface IPredicates
{
    Predicate Fail { get; }

    Predicate Cut { get; }

    Predicate Not(Term t);

    Predicate OfType<T>(TermValue tv);
}