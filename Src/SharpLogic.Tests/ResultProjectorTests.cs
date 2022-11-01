using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.Tests;

[TestClass]
public class ResultProjectorTests
{
    [TestMethod]
    public void ValueTupleProjection()
    {
        var stackFrame = new StackFrame(null);
        var projectorTuple2 = new ResultProjector<(int, string)>(stackFrame);

        var v1 = new QueryVariable("Item1");
        v1.Bind(5, stackFrame);
        var v2 = new QueryVariable("Item2");
        v2.Bind("test", stackFrame);
        stackFrame.Registers[0].Value = v2;
        stackFrame.Registers[1].Value = v1;

        var (i, s) = projectorTuple2.Result;

        Assert.AreEqual(5, i);
        Assert.AreEqual("test", s);

        var projectorTuple3 = new ResultProjector<(int, string, double)>(stackFrame);
        var v3 = new QueryVariable("Item3");
        v3.Bind(2.0, stackFrame);
        stackFrame.Registers[2].Value = v3;

        var (i2, s2, d) = projectorTuple3.Result;
        Assert.AreEqual(5, i2);
        Assert.AreEqual("test", s2);
        Assert.AreEqual(2.0, d);
    }

    [TestMethod]
    public void StringProjection()
    {
        var stackFrame = new StackFrame(null);
        var projector = new ResultProjector<string>(stackFrame);

        var v = new QueryVariable("v");
        v.Bind("test", stackFrame);

        stackFrame.Registers[0].Value = v;

        Assert.AreEqual("test", projector.Result);

        v.Unbind(stackFrame);
        v.Bind(5, stackFrame);

        Assert.AreEqual("5", projector.Result);
    }

    [DataTestMethod]  
    [DataRow(int.MaxValue)]
    [DataRow(short.MaxValue)]
    [DataRow(long.MaxValue)]
    [DataRow(float.MaxValue)]
    [DataRow(char.MaxValue)]
    public void PrimitiveProjection(object value)
    {
        var stackFrame = new StackFrame(null);
        var projector = typeof(ResultProjector<>).MakeGenericType(value.GetType()).GetConstructors().First().Invoke(new[] { stackFrame });

        var v = new QueryVariable("v");
        v.Bind(value, stackFrame);

        stackFrame.Registers[0].Value = v;

        Assert.AreEqual(value, projector.GetType().GetProperty("Result")!.GetValue(projector));
    }

    [TestMethod]
    public void DecimalProjection()
    {
        var stackFrame = new StackFrame(null);
        var projector = new ResultProjector<decimal>(stackFrame);

        var v = new QueryVariable("v");
        v.Bind(3.5m, stackFrame);

        stackFrame.Registers[0].Value = v;

        Assert.AreEqual(3.5m, projector.Result);
    }

    [TestMethod]
    public void ClassProjection()
    {
        var stackFrame = new StackFrame(null);
        var projector = new ResultProjector<TestClass>(stackFrame);

        var idVar = new QueryVariable(nameof(TestClass.Id));
        idVar.Bind(7, stackFrame);

        var nameVar = new QueryVariable(nameof(TestClass.Name));
        nameVar.Bind("myName", stackFrame);

        stackFrame.Registers[0].Value = idVar;
        stackFrame.Registers[1].Value = nameVar;

        var r = projector.Result;
        Assert.IsNotNull(r);
        Assert.AreEqual(7, r.Id);
        Assert.AreEqual("myName", r.Name);
    }

    [TestMethod]
    public void StructProjection()
    {
        var stackFrame = new StackFrame(null);
        var projector = new ResultProjector<TestStruct>(stackFrame);

        var idVar = new QueryVariable(nameof(TestStruct.Id));
        idVar.Bind(7, stackFrame);

        var nameVar = new QueryVariable(nameof(TestStruct.Name));
        nameVar.Bind("myName", stackFrame);

        stackFrame.Registers[0].Value = idVar;
        stackFrame.Registers[1].Value = nameVar;

        var r = projector.Result;
        Assert.IsNotNull(r);
        Assert.AreEqual(7, r.Id);
        Assert.AreEqual("myName", r.Name);
    }

    public class TestClass
    {
        public int Id { get; set; }

        public string? Name{ get; set; }
    }

    public struct TestStruct
    {
        public int Id { get; set; }

        public string? Name{ get; set; }
    }
}