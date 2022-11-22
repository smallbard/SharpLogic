using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Indexing;

public class ClausesIndex
{
    private readonly Dictionary<string, Dictionary<int, Offsets>> _functorOffsets;

    public ClausesIndex()
    {
        _functorOffsets = new Dictionary<string, Dictionary<int, Offsets>>();
    }

    public void AddOffset(int offset, Term t)
    {
        AddOffset(offset, t.Functor, t.Args);
    }

    public void AddOffset(int offset, Rule r)
    {
        AddOffset(offset, r.Functor, r.Head);
    }

    public IEnumerable<int> GetOffsets(string functor, Registers registers) =>
        !_functorOffsets.TryGetValue(functor, out var arityOffsets) || !arityOffsets.TryGetValue(registers.Count, out var offsets)
            ? Enumerable.Empty<int>() : offsets.GetOffsets(registers);

    private void AddOffset(int offset, string functor, TermValue[] head)
    {
        if (!_functorOffsets.TryGetValue(functor, out var offsetsByArity)) _functorOffsets[functor] = offsetsByArity = new Dictionary<int, Offsets>();
        if (!offsetsByArity.TryGetValue(head.Length, out var offsets)) offsetsByArity[head.Length] = offsets = new Offsets();

        offsets.AddOffset(offset, head);
    }
}