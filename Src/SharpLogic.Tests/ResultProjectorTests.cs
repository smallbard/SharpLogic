using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.Tests;

[TestClass]
public class ResultProjectorTests
{
    [TestMethod]
    public void ValueTupleProjection()
    {
        var stackFrame = new StackFrame(null);
        var v1 = new QueryVariable("Item1");
        v1.Instantiate(5, stackFrame);
        var v2 = new QueryVariable("Item2");
        v2.Instantiate("test", stackFrame);
        stackFrame.Registers[0].Value = v2;
        stackFrame.Registers[1].Value = v1;

        var projectorTuple2 = new ResultProjector<(int, string)>(stackFrame);

        var (i, s) = projectorTuple2.Result;

        Assert.AreEqual(5, i);
        Assert.AreEqual("test", s);

        var v3 = new QueryVariable("Item3");
        v3.Instantiate(2.0, stackFrame);
        stackFrame.Registers[2].Value = v3;

        var projectorTuple3 = new ResultProjector<(int, string, double)>(stackFrame);

        var (i2, s2, d) = projectorTuple3.Result;
        Assert.AreEqual(5, i2);
        Assert.AreEqual("test", s2);
        Assert.AreEqual(2.0, d);

        var v4 = new QueryVariable("Item4");
        v4.Instantiate('c', stackFrame);
        stackFrame.Registers[3].Value = v4;

        var projectorTuple4 = new ResultProjector<(int, string, double, char)>(stackFrame);

        var (i3, s3, d2, c) = projectorTuple4.Result;
        Assert.AreEqual(5, i3);
        Assert.AreEqual("test", s3);
        Assert.AreEqual(2.0, d2);
        Assert.AreEqual('c', c);

        var v5 = new QueryVariable("Item5");
        v5.Instantiate(1, stackFrame);
        stackFrame.Registers[4].Value = v5;

        var projectorTuple5 = new ResultProjector<(int, string, double, char, int)>(stackFrame);

        var (i4, s4, d3, c2,i5) = projectorTuple5.Result;
        Assert.AreEqual(5, i4);
        Assert.AreEqual("test", s4);
        Assert.AreEqual(2.0, d3);
        Assert.AreEqual('c', c2);
        Assert.AreEqual(1, i5);

        var v6 = new QueryVariable("Item6");
        v6.Instantiate(4, stackFrame);
        stackFrame.Registers[5].Value = v6;

        var projectorTuple6 = new ResultProjector<(int, string, double, char, int, int)>(stackFrame);

        var (i6, s5, d4, c3, i7, i8) = projectorTuple6.Result;
        Assert.AreEqual(5, i6);
        Assert.AreEqual("test", s5);
        Assert.AreEqual(2.0, d4);
        Assert.AreEqual('c', c3);
        Assert.AreEqual(1, i7);
        Assert.AreEqual(4, i8);

        var v7 = new QueryVariable("Item7");
        v7.Instantiate(3, stackFrame);
        stackFrame.Registers[6].Value = v7;

        var projectorTuple7 = new ResultProjector<(int, string, double, char, int, int, int)>(stackFrame);

        var (i9, s6, d5, c4, i10, i11, i12) = projectorTuple7.Result;
        Assert.AreEqual(5, i9);
        Assert.AreEqual("test", s6);
        Assert.AreEqual(2.0, d5);
        Assert.AreEqual('c', c4);
        Assert.AreEqual(1, i10);
        Assert.AreEqual(4, i11);
        Assert.AreEqual(3, i12);
    }

    [TestMethod]
    public void StringProjection()
    {
        var stackFrame = new StackFrame(null);
        var v = new QueryVariable("v");
        v.Instantiate("test", stackFrame);
        stackFrame.Registers[0].Value = v;

        var projector = new ResultProjector<string>(stackFrame);

        Assert.AreEqual("test", projector.Result);

        v.Uninstantiate(stackFrame);
        v.Instantiate(5, stackFrame);

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
        var v = new QueryVariable("v");
        v.Instantiate(value, stackFrame);
        stackFrame.Registers[0].Value = v;

        var projector = typeof(ResultProjector<>).MakeGenericType(value.GetType()).GetConstructors().First().Invoke(new[] { stackFrame });

        Assert.AreEqual(value, projector.GetType().GetProperty("Result")!.GetValue(projector));
    }

    [TestMethod]
    public void DecimalProjection()
    {
        var stackFrame = new StackFrame(null);
        var v = new QueryVariable("v");
        v.Instantiate(3.5m, stackFrame);
        stackFrame.Registers[0].Value = v;

        var projector = new ResultProjector<decimal>(stackFrame);

        Assert.AreEqual(3.5m, projector.Result);
    }

    [TestMethod]
    public void ClassProjection()
    {
        var stackFrame = new StackFrame(null);
        var idVar = new QueryVariable(nameof(TestClass.Id));
        idVar.Instantiate(7, stackFrame);
        var nameVar = new QueryVariable(nameof(TestClass.Name));
        nameVar.Instantiate("myName", stackFrame);

        stackFrame.Registers[0].Value = idVar;
        stackFrame.Registers[1].Value = nameVar;

        var projector = new ResultProjector<TestClass>(stackFrame);

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
        idVar.Instantiate(7, stackFrame);

        var nameVar = new QueryVariable(nameof(TestStruct.Name));
        nameVar.Instantiate("myName", stackFrame);

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