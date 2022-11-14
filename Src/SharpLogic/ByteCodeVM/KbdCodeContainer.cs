using System.Collections;
using System.Collections.Immutable;
using SharpLogic.ByteCodeVM.Compilation;

namespace SharpLogic.ByteCodeVM;

public class KbdCodeContainer : IEnumerable<(ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets)>
{
    private ImmutableList<(ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets)> _codeContainers;

    public KbdCodeContainer((ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets) factsAndRules)
    {
        _codeContainers = ImmutableList.Create(factsAndRules);
    }

    public IEnumerator<(ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets)> GetEnumerator()
    {
        return _codeContainers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void InsertFirst((ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets) code)
    {
        var lst = _codeContainers;
        while(lst != Interlocked.CompareExchange(ref _codeContainers, lst.Insert(0, code), lst)) lst = _codeContainers;
    }

    public void InsertLast((ReadOnlyMemory<byte> Code, GetOffsetsDelegate GetOffsets) code)
    {
        var lst = _codeContainers;
        while(lst != Interlocked.CompareExchange(ref _codeContainers, lst.Add(code), lst)) lst = _codeContainers;
    }
}