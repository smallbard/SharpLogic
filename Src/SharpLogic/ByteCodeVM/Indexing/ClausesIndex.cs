using System.Collections.Concurrent;
using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Indexing;

public class ClausesIndex
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, Offsets>> _functorOffsets;

    public ClausesIndex()
    {
        _functorOffsets = new ConcurrentDictionary<string, ConcurrentDictionary<int, Offsets>>();
    }

    public void AddOffsets(IEnumerable<(int Offset, Term Term)> offsets, ReadOnlyMemory<byte> code, IndexingMode mode)
    {
        foreach(var o in offsets)
            if (o.Term is Rule r)
                AddOffset(o.Offset, r.Functor, r.Head, code, mode);
            else
                AddOffset(o.Offset, o.Term.Functor, o.Term.Args, code, mode);
    }

    public IEnumerable<InstructionPointer> GetOffsets(string functor, Registers registers) =>
        !_functorOffsets.TryGetValue(functor, out var arityOffsets) || !arityOffsets.TryGetValue(registers.Count, out var offsets)
            ? Enumerable.Empty<InstructionPointer>() : offsets.GetOffsets(registers);

    private void AddOffset(int offset, string functor, TermValue[] head, ReadOnlyMemory<byte> code, IndexingMode mode)
    {
        //Console.WriteLine($"======= {functor}/{head.Length} {offset} =======");

        if (!_functorOffsets.TryGetValue(functor, out var offsetsByArity)) _functorOffsets[functor] = offsetsByArity = new ConcurrentDictionary<int, Offsets>();
        if (!offsetsByArity.TryGetValue(head.Length, out var offsets)) offsetsByArity[head.Length] = offsets = new Offsets();

        offsets.AddOffset(new InstructionPointer { P = offset, Code = code }, head, mode);
    }
}

public enum IndexingMode
{
    Append,
    Insert
}