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
        var code = ReadOnlyMemory<byte>.Empty;

        clausesIndex.AddOffsets(new List<(int, Term)>()
        {
            (1, new Term("fct", new[] { new TermValue(5) })),
            (2, new Term("fct", new[] { new TermValue(6) })),
            (3, new Term("fct", new[] { new TermValue(7) }))
        }, code, IndexingMode.Append);

        var registers = new Registers();
        registers[0].Value = 6;

        CollectionAssert.AreEquivalent(new[] { new InstructionPointer{ P = 2, Code = code } }, clausesIndex.GetOffsets("fct", registers).ToList());
    }

    [TestMethod]
    public void FirstInstantiatedVariableArgument()
    {
        var clausesIndex = new ClausesIndex();
        var code = ReadOnlyMemory<byte>.Empty;

        clausesIndex.AddOffsets(new List<(int, Term)>()
        {
            (1, new Term("fct", new[] { new TermValue(5) })),
            (2, new Term("fct", new[] { new TermValue(6) })),
            (3, new Term("fct", new[] { new TermValue(7) }))
        }, code, IndexingMode.Append);

        var registers = new Registers();
        var v = new QueryVariable("v");
        v.Instantiate(7, new StackFrame(null));
        registers[0].Value = v;

        CollectionAssert.AreEquivalent(new[] { new InstructionPointer{ P = 3, Code = code } }, clausesIndex.GetOffsets("fct", registers).ToList());
    }

    [TestMethod]
    public void FistVariableArgument()
    {
        var clausesIndex = new ClausesIndex();
        var code = ReadOnlyMemory<byte>.Empty;

        clausesIndex.AddOffsets(new List<(int, Term)>()
        {
            (1, new Term("fct", new[] { new TermValue(5) })),
            (2, new Term("fct", new[] { new TermValue(6) })),
            (3, new Term("fct", new[] { new TermValue(7) }))
        }, code, IndexingMode.Append);

        var registers = new Registers();
        registers[0].Value = new QueryVariable("v");

        CollectionAssert.AreEquivalent(new[] { new InstructionPointer{ P = 1, Code = code }, new InstructionPointer{ P = 2, Code = code }, new InstructionPointer{ P = 3, Code = code } }, clausesIndex.GetOffsets("fct", registers).ToList());
    }
}