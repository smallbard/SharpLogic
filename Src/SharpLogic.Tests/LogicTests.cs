namespace SharpLogic.Tests;

[TestClass]
public class LogicTests
{
    [TestMethod]
    public void GrandFatherExample()
    {
        // father("Tywin", "Jaime")
        // father("Jaime", "Joffrey")
        // grandFather(X,Y) :- father(X, Z), father(Z,Y).
        var vm = new Logic((t, p) =>
        {
            t.father("Tywin", "Jaime");
            t.father("Jaime", "Joffrey");
            t.grandFather((t.X, t.Y), t.father(t.X, t.Z), t.father(t.Z, t.Y));
        });

        var query = vm.Query<string>((t, p) => t.grandFather(t.X, "Joffrey"));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Tywin", result[0]);

        var query2 = vm.Query<string>((t, p) => t.grandFather("Tywin", t.X));
        result = query2.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Joffrey", result[0]);
    }

    [TestMethod]
    public void DeterministRule()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1.5, 2.5, 3.5);
            t.rule((t.X, t.Y, t.Z), t.fact(t.X, t.Y, t.Z));
        });

        var query = vm.Query<(double, double, double)>((t, p) => t.rule(t.Item1, t.Item2, t.Item3));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual((1.5, 2.5, 3.5), result[0]);
    }

    [TestMethod]
    public void FindAFact()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.fact(2, "two");
            t.fact(3, "three");
        });

        Assert.IsTrue(vm.Any((t, p) => t.fact(2, "two")));
    }

    [TestMethod]
    public void FactNotFound()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.fact(2, "two");
            t.fact(3, "three");
        });

        Assert.IsFalse(vm.Any((t, p) => t.fact(233, "two")));
        Assert.IsFalse(vm.Any((t, p) => t.fact(2, "forty")));
    }

    [TestMethod]
    public void SimpleFacts()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.fact(2, "two");
            t.fact(3, "three");
        });

        var query = vm.Query<(int, string)>((t, p) => t.fact(t.Item1, t.Item2));
        var result = query.ToList();
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual((1, "one"), result[0]);
        Assert.AreEqual((2, "two"), result[1]);
        Assert.AreEqual((3, "three"), result[2]);
    }

    [TestMethod]
    public void SimpleFacts_InverseVariablesProjection()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.fact(2, "two");
            t.fact(3, "three");
        });

        var query = vm.Query<(string, int)>((t, p) => t.fact(t.Item2, t.Item1));
        var result = query.ToList();
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(("one", 1), result[0]);
        Assert.AreEqual(("two", 2), result[1]);
        Assert.AreEqual(("three", 3), result[2]);
    }

    [TestMethod]
    public void ExtractValuesFromFact()
    {
        var vm = new Logic((t, p) =>
        {
            t.fact(1, "one");
            t.fact(2, "two");
            t.fact(3, "three");
        });

        var query = vm.Query<string>((t, p) => t.fact(2, t.X));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("two", result[0]);

        var query2 = vm.Query<int>((t, p) => t.fact(t.X, "two"));
        var resultInt = query2.ToList();
        Assert.AreEqual(1, resultInt.Count);
        Assert.AreEqual(2, resultInt[0]);
    }

    [TestMethod]
    public void VariableScope()
    {
        var vm = new Logic((t, p) =>
        {
            t.bridge(1, 2);
            t.bridge(2, 3);
            t.bridge(3, 4);
            t.path1((t.X, t.Y), t.bridge(t.X, t.Z), t.bridge(t.Z, t.Y));
            t.path2((t.X, t.Y), t.path1(t.X, t.Z), t.bridge(t.Z, t.Y));
        });

        var query = vm.Query<(int, int)>((t, p) => t.path2(t.Item1, t.Item2));
        var result = query.ToList();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result[0].Item1);
        Assert.AreEqual(4, result[0].Item2);
    }
}