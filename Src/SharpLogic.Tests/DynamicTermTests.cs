namespace SharpLogic.Tests;

[TestClass]
public class DynamicTermTests
{
    [TestMethod]
    public void AssertaAndAssertz()
    {
        var vm = new Logic((t, p) => {});

        Assert.IsFalse(vm.Any((t, p) => t.dynt(5, 4)));

        Assert.IsTrue(vm.Any((t, p) =>
        {
            p.Asserta(t.dynt(5, 4));
            p.Assertz(t.dynt(5, 6));
        }));

        Assert.IsTrue(vm.Any((t, p) => t.dynt(5, 4)));
        Assert.IsTrue(vm.Any((t, p) => t.dynt(5, 6)));

        var lst = vm.Query<int>((t, p) => t.dynt(5, t.X)).ToList();
        Assert.AreEqual(2, lst.Count);
        CollectionAssert.AreEquivalent(new[] { 4, 6 }, lst);
    }

    [TestMethod]
    public void WithInstantiatedVariables()
    {
        var vm = new Logic((t, p) => t.newBorn(t.X, p.Assertz(t.born(t.X, DateTime.Now))));

        Assert.IsTrue(vm.Any((t, p) => t.newBorn("Leonie")));

        Assert.IsTrue(vm.Any((t, p) => t.born("Leonie", t.D)));
    }
}