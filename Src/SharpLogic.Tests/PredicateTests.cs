using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SharpLogic.Tests;

[TestClass]
public class PredicateTests
{
    [TestMethod]
    public void Fail()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.failedRule((t.X, t.Y), t.fact(t.X, t.Y), p.Fail);
        });

        Assert.IsFalse(vm.Any((t, p) => t.failedRule(1, "one")));
    }

    [TestMethod]
    public void NotInRule()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.negate((t.X, t.Y), p.Not(t.fact(t.X, t.Y)));
        });

        Assert.IsTrue(vm.Any((t, p) => t.negate(48, "sixty")));
        Assert.IsFalse(vm.Any((t, p) => t.negate(1, "one")));
    }

    [TestMethod]
    public void NotInQuery()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
        });

        Assert.IsTrue(vm.Any((t, p) => p.Not(t.fact(48, "sixty"))));
        Assert.IsFalse(vm.Any((t, p) => p.Not(t.fact(1, "one"))));
    }

    [TestMethod]
    public void CutAndGreaterThan()
    {
        var vm = new Logic((t, p) =>
        {
            t.max((t.X, t.Y, t.X), t.X > t.Y, p.Cut);
            t.max(t.X, t.Y, t.Y);
        });

        var query = vm.Query<int>((t, p) => t.max(6, 3, t.Max));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(6, result[0]);

        var query2 = vm.Query<int>((t, p) => t.max(2, 9, t.Max));
        result = query2.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(9, result[0]);
    }

    [TestMethod]
    public void Assignment()
    {
        var vm = new Logic((t, p) =>
        {
            t.gcd((t.X, 0, t.X), p.Cut);
            t.gcd((0, t.X, t.X), p.Cut);
            t.gcd((t.X, t.Y, t.D), t.X <= t.Y, p.Cut, t.Z = t.Y - t.X, t.gcd(t.X, t.Z, t.D));
            t.gcd((t.X, t.Y, t.D), t.gcd(t.Y, t.X, t.D));
        });

        var query = vm.Query<int>((t, p) => t.gcd(6, 0, t.X));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(6, result[0]);

        var query2 = vm.Query<int>((t, p) => t.gcd(0, 6, t.X));
        result = query2.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(6, result[0]);

        var query3 = vm.Query<int>((t, p) => t.gcd(6, 15, t.X));
        result = query3.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(3, result[0]);
    }

    [TestMethod]
    public void OfType()
    {
        var vm = new Logic((t, p) =>
        {
            t.type((t.X, t.Y), p.OfType<int>(t.X), p.Cut, t.Y = "int!");
            t.type((t.X, t.Y), p.OfType<string>(t.X), p.Cut, t.Y = "string!");
            t.type((t.X, t.Y), t.Y = "unknown!");
        });

        var query = vm.Query<string>((t, p) => t.type(5, t.X));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("int!", result[0]);

        var query2 = vm.Query<string>((t, p) => t.type("test", t.X));
        result = query2.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("string!", result[0]);

        var query3 = vm.Query<string>((t, p) => t.type(3.2, t.X));
        result = query3.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("unknown!", result[0]);
    }

    [TestMethod]
    public void PropertyAccess()
    {
        var vm = new Logic((t, p) =>
        {
            t.extract((t.X, t.Y), t.Y = t.X.Login);
        });

        var query = vm.Query<string>((t, p) => t.extract(new User("test", 1), t.X));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("test", result[0]);
    }

    private record User(string Login, int Id);
}