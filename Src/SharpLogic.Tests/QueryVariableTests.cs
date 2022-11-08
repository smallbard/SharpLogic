using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.Tests;

[TestClass]
public class QueryVariableTests
{
    private readonly StackFrame _stackFrame = new StackFrame(null);

    [TestMethod]
    public void Bind()
    {
        var v = new QueryVariable("v");

        Assert.IsFalse(v.IsBound);
        Assert.ThrowsException<SharpLogicException>(() => v.Value);

        v.Bind(7, _stackFrame);
        Assert.IsTrue(v.IsBound);
        Assert.AreEqual(7, v.Value);
    }

    [TestMethod]
    public void Unbind()
    {
        var v = new QueryVariable("v");
        v.Bind(7, _stackFrame);

        v.Unbind(new StackFrame(null));

        Assert.IsTrue(v.IsBound);
        Assert.AreEqual(7, v.Value);

        v.Unbind(_stackFrame);

        Assert.IsFalse(v.IsBound);
        Assert.ThrowsException<SharpLogicException>(() => v.Value);
    }

    [TestMethod]
    public void EquivalentVariables()
    {
        var v1 = new QueryVariable("v1");
        var v2 = new QueryVariable("v2");
        var v3 = new QueryVariable("v3");
        var v4 = new QueryVariable("v4");

        QueryVariable.MakeEquivalent(v1, v4, _stackFrame);
        QueryVariable.MakeEquivalent(v1, v2, _stackFrame);
        QueryVariable.MakeEquivalent(v3, v2, _stackFrame);

        v2.Bind(6, _stackFrame);

        Assert.IsTrue(v1.IsBound);
        Assert.IsTrue(v2.IsBound);
        Assert.IsTrue(v3.IsBound);
        Assert.IsTrue(v4.IsBound);

        Assert.AreEqual(6, v1.Value);
        Assert.AreEqual(6, v2.Value);
        Assert.AreEqual(6, v3.Value);
        Assert.AreEqual(6, v4.Value);

        v3.Unbind(_stackFrame);

        Assert.IsFalse(v1.IsBound);
        Assert.IsFalse(v2.IsBound);
        Assert.IsFalse(v3.IsBound);
        Assert.IsFalse(v4.IsBound);
    }
}