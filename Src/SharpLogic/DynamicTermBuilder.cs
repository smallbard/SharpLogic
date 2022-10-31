using System.Dynamic;
using System.Runtime.CompilerServices;
using SharpLogic.ByteCodeVM;

namespace SharpLogic;

internal class DynamicTermBuilder : DynamicObject, IPredicates
{
    private readonly IFactAndRule? _factAndRule;
    private readonly Action<Term>? _addTermToQuery;
    private readonly List<Term> _potentialFacts = new List<Term>();
    private readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();
    private readonly Dictionary<object, Predicate> _assignments = new Dictionary<object, Predicate>();

    public DynamicTermBuilder(IFactAndRule? factAndRule, Action<Term>? addTermToQuery)
    {
        _factAndRule = factAndRule;
        _addTermToQuery = addTermToQuery;
    }

    public Predicate Fail => new Predicate(nameof(Fail), Array.Empty<TermValue>());

    public Predicate Cut => new Predicate(nameof(Cut), Array.Empty<TermValue>());

    public Predicate Not(Term t)
    {
        var args = new[] { t };
        RemainingFactsAndArgumentTerms(args);

        var p = new Predicate(nameof(Not), args);
        _potentialFacts.Add(p);

        return p;
    }

    public Predicate OfType<T>(TermValue tv)
    {
        if (!(tv is Variable)) throw new SharpLogicException("OfType predicate can be use only on variable.");

        var p = new Predicate(nameof(OfType), new[] { tv, new TermValue(typeof(T).GetHashCode()) });
        _potentialFacts.Add(p);

        return p;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        var name = binder.Name;

        if (_variables.ContainsKey(name))
            result = _variables[name];
        else
            result = _variables[name] = new Variable(name);

        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (!_variables.TryGetValue(binder.Name, out Variable? v))
            _variables[binder.Name] = v = new Variable(binder.Name);

        if (value != null)
        {
            var tv = value as TermValue ?? new TermValue(value);
            _assignments[value] = new Predicate("Assignment", new[] { v, tv });
        }

        return true;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        result = null;

        var name = binder.Name;

        if (args != null && args.Length > 255) throw new SharpLogicException($"Term {name} must have less than 256 arguments.");

        var termArgs = args?.Select(t => t != null && _assignments.ContainsKey(t) ? GetAndRemoveAssignment(t) : t).OfType<Term>().ToList() ?? Enumerable.Empty<Term>();

        if (args != null && args.Length > 0 && args[0] is ITuple head && termArgs.Any())
        {
            if (_addTermToQuery != null) throw new SharpLogicException("Rule can't be created by query.");

            RemainingFactsAndArgumentTerms(termArgs);

            _factAndRule!.AddRule(
                name,
                TupleEnumerate(head).Select(ConvertToTermValue).ToArray(),
                termArgs);
        }
        else if (args != null && args.Length > 0 && args[0] is ITuple)
            throw new SharpLogicException("Rule must have a body.");
        else
        {
            var t = new Term(name, args == null ? Array.Empty<TermValue>() : args.Select(ConvertToTermValue).ToArray());
            _potentialFacts.Add(t);
            result = t;
        }

        return true;
    }

    public void CreateRemainingFacts()
    {
        foreach (var fact in _potentialFacts)
        {
            if (fact is Predicate && _addTermToQuery == null) throw new SharpLogicException("A predicate can't be a fact. It must be used in a rule.");
            var termValues = new TermValue[fact.Args.Length];
            for (var i = 0; i < fact.Args.Length; i++) termValues[i] = ConvertToTermValue(fact.Args[i]);

            if (_factAndRule != null)
                _factAndRule.AddFact(fact.Functor, termValues);
            else if (_addTermToQuery != null)
                _addTermToQuery(fact);
        }
    }

    private Predicate GetAndRemoveAssignment(object t)
    {
        var assignment = _assignments[t];
        _assignments.Remove(t);
        return assignment;
    }

    private void RemainingFactsAndArgumentTerms(IEnumerable<Term> termArgs)
    {
        var argumentTerms = new HashSet<Term>();
        GetArgumentTerms(termArgs, argumentTerms);
        _potentialFacts.RemoveAll(t => argumentTerms.Contains(t));

        CreateRemainingFacts();
        _potentialFacts.Clear();
    }

    private void GetArgumentTerms(IEnumerable<Term> terms, HashSet<Term> argumentTerms)
    {
        foreach (var term in terms)
        {
            if (term is Predicate && term.Args != null)
                GetArgumentTerms(term.Args.OfType<Term>(), argumentTerms);

            argumentTerms.Add(term);
        }
    }

    private TermValue ConvertToTermValue(object? obj)
    {
        if (obj is TermValue tv) return tv;
        return new TermValue(obj);
    }

    private IEnumerable<object?> TupleEnumerate(ITuple t)
    {
        for (var i = 0; i < t.Length; i++) yield return t[i];
    }
}