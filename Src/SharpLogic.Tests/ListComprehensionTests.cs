using SharpLogic.ByteCodeVM.Compilation;

namespace SharpLogic.Tests;


[TestClass]
public class ListComprehensionTests
{
    [TestMethod]
    public void TailHead()
    {
        var vm = new Logic((t, p) =>
        {
            t.lenacc(p.Empty, t.A, t.A);
            t.lenacc((p[t.H, p.Tail(t.T)], t.A, t.N), p.Is(t.A1, t.A + 1), t.lenacc(t.T, t.A1, t.N));
            t.len((t.L, t.N), t.lenacc(t.L, 0, t.N));
        });
        Assert.AreEqual(3, vm.Query<int>((t, p) => t.len(new[] { 4, 5, 6 }, t.N)).FirstOrDefault());
    }

    [TestMethod]
    public void Last()
    {
        var vm = new Logic((t, p) =>
        {
            t.last(t.X, p[t.X]);
            t.last((t.X, p[t._, p.Tail(t.Y)]), t.last(t.X, t.Y));
        });
        Assert.AreEqual(6, vm.Query<int>((t, p) => t.last(t.X, new[] { 4, 5, 6 })).FirstOrDefault());
    }
}