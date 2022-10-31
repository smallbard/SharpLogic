namespace SharpLogic;

public interface IQuery<T> :  IEnumerable<T>
{
    void AddTerm(Term t);
}