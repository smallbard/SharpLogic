using SharpLogic.ByteCodeVM;

namespace SharpLogic.Tests;

[TestClass]
public class ValueConstantsTests
{
    [DataTestMethod]  
    [DataRow(int.MaxValue)]
    [DataRow(short.MaxValue)]
    [DataRow(long.MaxValue)]
    [DataRow(float.MaxValue)]
    [DataRow(char.MaxValue)]
    public void AddAndReadConstant(object cst)
    {
        var valueConstants = new ValueConstants(null);
        Assert.IsTrue(valueConstants.TryAddBoxedConstant(cst, out var index));
        Assert.AreEqual(cst, valueConstants.GetRealValueConstant(index));
    }

    [TestMethod]
    public void AddAndReadDecimalConstant()
    {
        var valueConstants = new ValueConstants(null);
        var dec = 5.0m;
        Assert.IsTrue(valueConstants.TryAddBoxedConstant(dec, out var index));
        Assert.AreEqual(dec, valueConstants.GetRealValueConstant(index));
    }

    [TestMethod]
    public void RejectNullAndNonPrimitiveTypes()
    {
        var valueConstants = new ValueConstants(null);

        Assert.IsFalse(valueConstants.TryAddBoxedConstant(null, out _));
        Assert.IsFalse(valueConstants.TryAddBoxedConstant("test", out _));
        Assert.IsFalse(valueConstants.TryAddBoxedConstant(new object(), out _));
    }

    [TestMethod]
    public void QueryValueConstants()
    {
        var valueConstantsKb = new ValueConstants(null);
        valueConstantsKb.TryAddBoxedConstant(6, out var intIndex);

        var valueConstantsQuery = new ValueConstants(valueConstantsKb);
        valueConstantsQuery.TryAddBoxedConstant(2.0, out var doubleIndex);

        Assert.AreEqual(6, valueConstantsQuery.GetRealValueConstant(intIndex));
        Assert.AreEqual(2.0, valueConstantsQuery.GetRealValueConstant(doubleIndex));
    }

}