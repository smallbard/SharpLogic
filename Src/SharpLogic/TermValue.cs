using System.Dynamic;

namespace SharpLogic;

public class TermValue : DynamicObject
{
    public TermValue(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public TermValue? Parent { get; set; }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = new Predicate("MemberAccess", new[] { this, new TermValue(binder.Name) });

        return true;
    }

    public static Predicate operator %(TermValue a, TermValue b) => new Predicate("Modulus", new[] { a, b });

    public static Predicate operator +(TermValue a, TermValue b) => new Predicate("Add", new[] { a, b });

    public static Predicate operator -(TermValue a, TermValue b) => new Predicate("Substract", new[] { a, b });

    public static Predicate operator *(TermValue a, TermValue b) => new Predicate("Multiply", new[] { a, b });

    public static Predicate operator /(TermValue a, TermValue b) => new Predicate("Divide", new[] { a, b });

    public static Predicate operator >(TermValue a, TermValue b) => new Predicate("GreaterThan", new[] { a, b });

    public static Predicate operator <(TermValue a, TermValue b) => new Predicate("LessThan", new[] { a, b });

    public static Predicate operator >=(TermValue a, TermValue b) => new Predicate("GreaterThanOrEqual", new[] { a, b });

    public static Predicate operator <=(TermValue a, TermValue b) => new Predicate("LessThanOrEqual", new[] { a, b });

    public static Predicate operator ==(TermValue a, TermValue b) => new Predicate("Equal", new[] { a, b });

    public static Predicate operator !=(TermValue a, TermValue b) => new Predicate("NotEqual", new[] { a, b });

    public static Predicate operator %(TermValue a, object b) => new Predicate("Modulus", new[] { a, new TermValue(b) });

    public static Predicate operator +(TermValue a, object b) => new Predicate("Add", new[] { a, new TermValue(b) });

    public static Predicate operator -(TermValue a, object b) => new Predicate("Substract", new[] { a, new TermValue(b) });

    public static Predicate operator *(TermValue a, object b) => new Predicate("Multiply", new[] { a, new TermValue(b) });

    public static Predicate operator /(TermValue a, object b) => new Predicate("Divide", new[] { a, new TermValue(b) });

    public static Predicate operator >(TermValue a, object b) => new Predicate("GreaterThan", new[] { a, new TermValue(b) });

    public static Predicate operator <(TermValue a, object b) => new Predicate("LessThan", new[] { a, new TermValue(b) });

    public static Predicate operator >=(TermValue a, object b) => new Predicate("GreaterThanOrEqual", new[] { a, new TermValue(b) });

    public static Predicate operator <=(TermValue a, object b) => new Predicate("LessThanOrEqual", new[] { a, new TermValue(b) });

    public static Predicate operator ==(TermValue a, object b) => new Predicate("Equal", new[] { a, new TermValue(b) });

    public static Predicate operator !=(TermValue a, object b) => new Predicate("NotEqual", new[] { a, new TermValue(b) });

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}