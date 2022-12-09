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

        Assert.IsFalse(v.Instantiated);
        Assert.ThrowsException<SharpLogicException>(() => v.Value);

        v.Instantiate(7, _stackFrame);
        Assert.IsTrue(v.Instantiated);
        Assert.AreEqual(7, v.Value);
    }

    [TestMethod]
    public void Unbind()
    {
        var v = new QueryVariable("v");
        v.Instantiate(7, _stackFrame);

        new StackFrame(null).UninstantiateVariables();

        Assert.IsTrue(v.Instantiated);
        Assert.AreEqual(7, v.Value);

        _stackFrame.UninstantiateVariables();

        Assert.IsFalse(v.Instantiated);
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

        v2.Instantiate(6, _stackFrame);

        Assert.IsTrue(v1.Instantiated);
        Assert.IsTrue(v2.Instantiated);
        Assert.IsTrue(v3.Instantiated);
        Assert.IsTrue(v4.Instantiated);

        Assert.AreEqual(6, v1.Value);
        Assert.AreEqual(6, v2.Value);
        Assert.AreEqual(6, v3.Value);
        Assert.AreEqual(6, v4.Value);

        _stackFrame.UninstantiateVariables();

        Assert.IsFalse(v1.Instantiated);
        Assert.IsFalse(v2.Instantiated);
        Assert.IsFalse(v3.Instantiated);
        Assert.IsFalse(v4.Instantiated);
    }
}