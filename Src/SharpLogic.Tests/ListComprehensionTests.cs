namespace SharpLogic;

/*
[TestClass]
public class ListComprehensionTests
{
    [TestMethod]
    public void ListIsEmpty()
    {
        using var vm = new Logic((t, p) => {});
        using var query = vm.Query<IEnumerable<int>>((t, p) => p.Empty<int>(t.X));

        var result = query.FirstOrDefault();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public void IsMemberExample()
    {
        using var vm = new Logic((t, p) =>
        {
            t.member((t.X, t.L), p.Empty(t.L), p.Fail);
            t.member((t.X, t.L), p.Head(t.X, t.L));
            t.member((t.X, t.L), p.Tail(t.T, t.L), t.member(t.X, t.T));
        });

        Assert.IsTrue(vm.Any((t, p) => t.member(4, Enumerable.Range(1, 10))));
        Assert.IsFalse(vm.Any((t, p) => t.member(233, Enumerable.Range(1, 10))));
    }

    [TestMethod]
    public void GetMemberExample()
    {
        using var vm = new Logic((t, p) =>
        {
            t.member((t.L, t.X), p.Empty(t.L), p.Fail);
            t.member((t.L, t.X), p.Head(t.L, t.X));
            t.member((t.L, t.X), p.Tail(t.L, t.T), t.member(t.T, t.X));
        });

        using var query = vm.Query<int>((t, p) => t.member(t.M, Enumerable.Range(1, 10)));
        var result = query.ToList();
        Assert.AreEqual(10, result.Count);
        foreach(var n in Enumerable.Range(1, 10)) CollectionAssert.Contains(result, n);
    }

    [TestMethod]
    public void ListManualConstruction()
    {
        using var vm = new Logic((t, p) =>
        {
            t.Add((t.L, t.X, t.LR), p.Empty(t.L), p.Head(t.LR, t.X));
            t.Add((t.L, t.X, t.LR), p.Tail(t.LR, t.L), p.Head(t.LR, t.X));
        });

        using var query = vm.Query<IEnumerable<int>>((t, p) =>
        {
            t.Add(Enumerable.Empty<int>(), 1, t.L);
            t.Add(Enumerable.Empty<int>(), 3, t.L);
        });
        var result = query.First().ToList();

        Assert.AreEqual(2, result.Count());
        CollectionAssert.Contains(result, 1);
        CollectionAssert.Contains(result, 3);
    }
}*/