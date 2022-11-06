using System.Dynamic;
using System.Runtime.CompilerServices;
using SharpLogic.ByteCodeVM;

namespace SharpLogic;

internal class DynamicTermBuilder : DynamicObject, IPredicates
{
    private readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();
    private readonly ASTBuilder _astBuilder;

    public DynamicTermBuilder(ASTBuilder astBuilder)
    {
        _astBuilder = astBuilder;
    }

    public Predicate Fail => new Predicate(nameof(Fail), Array.Empty<TermValue>());

    public Predicate Cut => new Predicate(nameof(Cut), Array.Empty<TermValue>());

    public Predicate Not(Term t)
    {
        var p = new Predicate(nameof(Not), new[] { t });
        _astBuilder.AddTerm(p);

        return p;
    }

    public ListPredicate Empty => new ListPredicate(nameof(Empty), Array.Empty<TermValue>());

    public ListPredicate this[TermValue h, TermValue t] 
    {
        get
        {
            var p = new ListPredicate("HeadTail", new[] { h, t });
            _astBuilder.AddTerm(p);

            return p;
        }
    }

    public Predicate OfType<T>(Variable v)
    {
        var p = new Predicate(nameof(OfType), new[] { v, new TermValue(typeof(T).GetHashCode()) });
        _astBuilder.AddTerm(p);

        return p;
    }

    public Predicate Is(Variable v, object value)
    {
        var p = new Predicate(nameof(Is), new[] { v, ConvertToTermValue(value) });
        _astBuilder.AddTerm(p);

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

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        result = null;

        var name = binder.Name;

        if (args != null && args.Length > 255) throw new SharpLogicException($"Term {name} must have less than 256 arguments.");

        if (args != null && args.Length > 0 && args[0] is ITuple tup && TupleEnumerate(tup).Any(a => a is Variable))
        {
            var ruleArgs = args.Skip(1).Select(ConvertToTermValue).ToArray();
            var head = TupleEnumerate(tup).Select(ConvertToTermValue).ToArray();
            var r = new Rule(name, head, ruleArgs);
            result = r;
            _astBuilder.AddTerm(r);
            
            return true;
        }

        var t = new Term(name, args?.Select(a => ConvertToTermValue(a))?.ToArray() ?? Array.Empty<TermValue>());
        result = t;
        _astBuilder.AddTerm(t);

        return true;
    }

    private IEnumerable<object?> TupleEnumerate(ITuple t)
    {
        for (var i = 0; i < t.Length; i++) yield return t[i];
    }

    private TermValue ConvertToTermValue(object? obj) => obj switch
    {
        TermValue tv => tv,
        _ => new TermValue(obj)
    };
}