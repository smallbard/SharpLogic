using System.Runtime.CompilerServices;

namespace SharpLogic;

public class ASTBuilder
{
    private readonly List<Term> _terms = new List<Term>();

    public IEnumerable<Term> Terms => _terms;

    public void AddTerm(Term t)
    {
        RemoveArguments(t);
        _terms.Add(t);
    }

    private void RemoveArguments(Term t)
    {
        foreach (var arg in t.Args.Reverse())
        {
            if (_terms.Count > 0 && object.Equals(_terms[_terms.Count - 1], arg))
                _terms.RemoveAt(_terms.Count - 1);
            if (arg is Term ta)
                RemoveArguments(ta);
        }
    }

    private IEnumerable<object?> TupleEnumerate(ITuple t)
    {
        for (var i = 0; i < t.Length; i++) yield return t[i];
    }
}