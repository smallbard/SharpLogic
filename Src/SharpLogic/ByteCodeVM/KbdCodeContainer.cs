using System.Collections;
using System.Collections.Immutable;
using SharpLogic.ByteCodeVM.Indexing;

namespace SharpLogic.ByteCodeVM;

public class KbdCodeContainer : IEnumerable<(ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex)>
{
    private ImmutableList<(ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex)> _codeContainers;

    public KbdCodeContainer((ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex) factsAndRules)
    {
        _codeContainers = ImmutableList.Create(factsAndRules);
    }

    public IEnumerator<(ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex)> GetEnumerator()
    {
        return _codeContainers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void InsertFirst((ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex) code)
    {
        var lst = _codeContainers;
        while(lst != Interlocked.CompareExchange(ref _codeContainers, lst.Insert(0, code), lst)) lst = _codeContainers;
    }

    public void InsertLast((ReadOnlyMemory<byte> Code, ClausesIndex ClausesIndex) code)
    {
        var lst = _codeContainers;
        while(lst != Interlocked.CompareExchange(ref _codeContainers, lst.Add(code), lst)) lst = _codeContainers;
    }
}