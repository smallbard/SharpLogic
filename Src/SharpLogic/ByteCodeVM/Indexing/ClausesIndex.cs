using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Indexing;

public class ClausesIndex
{
    private readonly Dictionary<string, Dictionary<int, List<int>>> _functorOffsets;

    public ClausesIndex()
    {
        _functorOffsets = new Dictionary<string, Dictionary<int, List<int>>>();
    }

    public void AddOffset(int offset, Term t)
    {
        AddOffset(offset, t.Functor, t.Args.Length);
    }

    public void AddOffset(int offset, Rule r)
    {
        AddOffset(offset, r.Functor, r.Head.Length);
    }

    public IEnumerable<int> GetOffsets(string functor, Registers registers) =>
        !_functorOffsets.TryGetValue(functor, out var arityOffsets) || !arityOffsets.TryGetValue(registers.Count, out var offsets)
            ? Enumerable.Empty<int>() : offsets;

    private void AddOffset(int offset, string functor, int arity)
    {
        if (!_functorOffsets.TryGetValue(functor, out var offsetsByArity)) _functorOffsets[functor] = offsetsByArity = new Dictionary<int, List<int>>();
        if (!offsetsByArity.TryGetValue(arity, out var offsets)) offsetsByArity[arity] = offsets = new List<int>();

        offsets.Add(offset);
    }
}