# SharpLogic

SharpLogic integrates Prolog with C#. There are no built-in strings, but rather a Linq-like approach to fully exploit the capabilities of the C# language with a Prolog virtual machine implemented in C#.

## How to use it ?

First define facts and rules :

```csharp
var vm = new Logic((t, p) =>
{
    t.father("Tywin", "Jaime");
    t.father("Jaime", "Joffrey");
    t.grandFather((t.X, t.Y), t.father(t.X, t.Z), t.father(t.Z, t.Y));
});
```

Secondly, make some queries :

```csharp
var query = vm.Query<string>((t, p) => t.grandFather(t.X, "Joffrey"));
foreach (var result in query)
{
    // query is an IEnumerable<string>
}
```

Or use the Any method for simpler queries :

```csharp
bool result = vm.Any((t, p) => t.father("Jaime", "Joffrey"));
```

Check the unit tests for more examples.

## How it works ?

### Fact, rule and query definitions

As it can be seen in _Logic_ constructor and in Query method, a method is called with two parameters, named t and p in the examples :

- t is a dynamic object which lets you express some Prolog like code without prior fake class and method definitions.
- p is an instance of _IPredicates_. As the name suggests, it gives access to all supported predicates to build rules and queries.

> It's not usually good practice to use dynamic object in C# but here it comes in handy. As Prolog is a declarative language, dynamic evaluation is used here to build an AST. Think of System.Linq.Expressions but dynamic.

Loops and other c# constructs can be used to define fact and rules (for example, facts can be read from a database).

### Execution

Facts, rules and queries are compiled to a byte code and executed by a virtual machine freely inspired by the Warren's Abstract Machine : some principles are the same, opcode instructions are not and c# integration definitely impacts the design.

Queries are lazy evaluated, so you are free to generate infinite results : next solution is determined when MoveNext is called on the enumerator (queries implement IEnumerable\<T\>).

_Logic_ is thread safe : multiple queries can run at the same time on one instance of _Logic_. Even when dynamic clause assertions are used (_p.Asserta_ and _p.Assertz_).

## Supported predicates and operators

### Fail

The predicate _Fail_ leads to the failure of a rule.

```csharp
var vm = new Logic((t, p) =>
{
    t.fact(1, "one");
    t.failedRule((t.X, t.Y), t.fact(t.X, t.Y), p.Fail);
});

Console.WriteLine(vm.Any((t, p) => t.failedRule(1, "one"))); // display False
```

### Not

The predicate _Not_ applies a negation to the term passed as argument.

```csharp
var vm = new Logic((t, p) =>
{
    t.fact(1, "one");
    t.negate((t.X, t.Y), p.Not(t.fact(t.X, t.Y)));
});

if (vm.Any((t, p) => t.negate(1, "one")))
{
    Console.WriteLine("one!");
}
else if (vm.Any((t, p) => t.negate(48, "sixty")))
{
    Console.WriteLine("sixty!");
}

// display sixty
```

### Cut

The predicate _cut_ always succeeds but cannot be backtracked. It prunes the search space.

```csharp
var vm = new Logic((t, p) =>
{
    t.max((t.X, t.Y, t.X), t.X > t.Y, p.Cut);
    t.max(t.X, t.Y, t.Y);
});

var query = vm.Query<int>((t, p) => t.max(6, 3, t.Max));
Console.WriteLine(query.First());

// display 6
```

> As you see in this example, comparison operators are supported : <, >, <=, >=, == and !=.

### Is

In Prolog, _is_ succeeds if the left operand is the value to which the right operand evaluates, and it is generally used with an unbound left operand.

```csharp
var vm = new Logic((t, p) =>
{
    t.gcd((t.X, 0, t.X), p.Cut);
    t.gcd((0, t.X, t.X), p.Cut);
    t.gcd((t.X, t.Y, t.D), t.X <= t.Y, p.Cut, p.Is(t.Z, t.Y - t.X), t.gcd(t.X, t.Z, t.D));
    t.gcd((t.X, t.Y, t.D), t.gcd(t.Y, t.X, t.D));
});

var query = vm.Query<int>((t, p) => t.gcd(6, 15, t.X));
Console.WriteLine(query.First());

// display 3
```

### OfType

The predicate OfType succeeds if the value type of a bound variable is the type argument of OfType. It fails if the variable is not bound or the value type is not the correct one.

```csharp
var vm = new Logic((t, p) =>
{
    t.type((t.X, t.Y), p.OfType<int>(t.X), p.Cut, t.Y = "int!");
    t.type((t.X, t.Y), p.OfType<string>(t.X), p.Cut, t.Y = "string!");
    t.type((t.X, t.Y), t.Y = "unknown!");
});

var query = vm.Query<string>((t, p) => t.type(5, t.X));
Console.WriteLine(query.First());

// display int!
```

### Property or field access

As rules and queries are dynamically typed, property or field access can fail in some cases :

- property or field doesn't exist in the value type
- the variable is not bound.

```csharp
var vm = new Logic((t, p) =>
{
    t.extract((t.X, t.Y), t.Y = t.X.Login);
});

var query = vm.Query<string>((t, p) => t.extract(new User("test", 1), t.X));
Console.WriteLine(query.First());

// display test
```

### List comprehension

Unify with empty list is done by p.Empty.
A list pattern can be build with p[ .. ]. p.Tail() can be used inside a list pattern to unify with the list tail.

```csharp
var vm = new Logic((t, p) =>
{
    t.lenacc(p.Empty, t.A, t.A);
    t.lenacc((p[t.H, p.Tail(t.T)], t.A, t.N), p.Is(t.A1, t.A + 1), t.lenacc(t.T, t.A1, t.N));
    t.len((t.L, t.N), t.lenacc(t.L, 0, t.N));
});

Console.WriteLine(vm.Query<int>((t, p) => t.len(new[] { 4, 5, 6 }, t.N)).FirstOrDefault());

// display 3
```

Another example with a discard :

```csharp
var vm = new Logic((t, p) =>
{
    t.last(t.X, p[t.X]);
    t.last((t.X, p[t._, p.Tail(t.Y)]), t.last(t.X, t.Y));
});

Console.WriteLine(vm.Query<int>((t, p) => t.last(t.X, new[] { 4, 5, 6 })).FirstOrDefault());

// display 6
```

For these predicates to work, values must implement _System.Collections.Generics.IEnumerable_.

### Asserta and Assertz

Assert a fact or a rule in the database, either in first position, _p.Asserta_, or in last position, _p.Assertz_. These new elements are then available for all queries.

```csharp
var vm = new Logic((t, p) => t.newBorn(t.X, p.Assertz(t.born(t.X, DateTime.Now))));
```

## Remaining tasks

- More predicates in order to be as close as possible to the prolog ISO standard.
- The first objective is correctness but then optimization : clause indexing, last call optimization, reduce boxing and reflection usage...
