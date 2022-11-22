using SharpLogic.ByteCodeVM.Execution;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic.Tests;

[TestClass]
public class ClausesIndexTests
{
    [TestMethod]
    public void FirstConstantArgument()
    {
        var clausesIndex = new ClausesIndex();

        clausesIndex.AddOffset(1, new Term("fct", new[] { new TermValue(5) }));
        clausesIndex.AddOffset(2, new Term("fct", new[] { new TermValue(6) }));
        clausesIndex.AddOffset(3, new Term("fct", new[] { new TermValue(7) }));

        var registers = new Registers();
        registers[0].Value = 6;

        CollectionAssert.AreEquivalent(new[] { 2 }, clausesIndex.GetOffsets("fct", registers).ToList());
    }

    [TestMethod]
    public void FirstInstantiatedVariableArgument()
    {
        var clausesIndex = new ClausesIndex();

        clausesIndex.AddOffset(1, new Term("fct", new[] { new TermValue(5) }));
        clausesIndex.AddOffset(2, new Term("fct", new[] { new TermValue(6) }));
        clausesIndex.AddOffset(3, new Term("fct", new[] { new TermValue(7) }));

        var registers = new Registers();
        var v = new QueryVariable("v");
        v.Instantiate(7, new StackFrame(null));
        registers[0].Value = v;

        CollectionAssert.AreEquivalent(new[] { 3 }, clausesIndex.GetOffsets("fct", registers).ToList());
    }

    [TestMethod]
    public void FistVariableArgument()
    {
        var clausesIndex = new ClausesIndex();

        clausesIndex.AddOffset(1, new Term("fct", new[] { new TermValue(5) }));
        clausesIndex.AddOffset(2, new Term("fct", new[] { new TermValue(6) }));
        clausesIndex.AddOffset(3, new Term("fct", new[] { new TermValue(7) }));

        var registers = new Registers();
        registers[0].Value = new QueryVariable("v");

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, clausesIndex.GetOffsets("fct", registers).ToList());
    }
}