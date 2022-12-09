using System.Collections.Concurrent;
using System.Collections.Immutable;
using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Indexing;

public class Offsets
{
    private static readonly object _lockFirstEltIndex = new object();

    private ImmutableList<InstructionPointer> _all = ImmutableList<InstructionPointer>.Empty;
    private ConcurrentDictionary<int, ImmutableList<InstructionPointer>> _firstEltIndex = new ConcurrentDictionary<int, ImmutableList<InstructionPointer>>();
    private ImmutableList<InstructionPointer> _nonConstantFirstTerms = ImmutableList<InstructionPointer>.Empty;

    public void AddOffset(InstructionPointer offset, TermValue[] head, IndexingMode mode)
    {
        if (mode == IndexingMode.Append)
            InterlockedAppend(ref _all, offset);
        else
            InterlockedInsert(ref _all, offset);

        if (head.Length == 0) return;

        if (head[0] is Variable || head[0] is Term)
        {
            if (mode == IndexingMode.Append)
                InterlockedAppend(ref _nonConstantFirstTerms, offset);
            else
                InterlockedInsert(ref _nonConstantFirstTerms, offset);
        }
        else
        {
            var value = head[0].Value;
            if (value == null) return;

            if (!_firstEltIndex.TryAdd(value.GetHashCode(), ImmutableList.Create<InstructionPointer>(offset)))
                lock (_lockFirstEltIndex)
                {
                    if (mode == IndexingMode.Append)
                        _firstEltIndex[value.GetHashCode()] = _firstEltIndex[value.GetHashCode()].Add(offset);
                    else
                        _firstEltIndex[value.GetHashCode()] = _firstEltIndex[value.GetHashCode()].Insert(0, offset);
                }
        }
    }

    public IEnumerable<InstructionPointer> GetOffsets(Registers registers)
    {
        if (registers.Count > 0)
        {
            IEnumerable<InstructionPointer>? offsets = null;

            var firstRegister = registers[0];
            if (firstRegister.Type == RegisterValueType.Constant && firstRegister.Value != null)
            {
                var vhc = GetValueHashCode(firstRegister.Value);
                if (_firstEltIndex.ContainsKey(vhc)) 
                    offsets = _firstEltIndex[vhc];
                else
                    offsets = Enumerable.Empty<InstructionPointer>();
            }
            else if (firstRegister.Value is QueryVariable v && v.Instantiated && v.Value != null)
            {
                var vhc = GetValueHashCode(v.Value);
                if (_firstEltIndex.ContainsKey(vhc)) 
                    offsets = _firstEltIndex[vhc];
                else
                    offsets = Enumerable.Empty<InstructionPointer>();
            }

            if (offsets != null && _nonConstantFirstTerms.Count > 0) 
                return offsets.Union(_nonConstantFirstTerms).OrderBy(o => _all.IndexOf(o));
            else if (offsets != null)
                return offsets;
        }

        return _all;
    }

    private int GetValueHashCode(object value)
    {
        if (value is ValueTuple<ReadOnlyMemory<byte>, Type> cst)
            return ValueConstants.GetRealValueConstant(cst).GetHashCode();

        return value.GetHashCode();
    }

    private void InterlockedInsert<T>(ref ImmutableList<T> lst, T ip)
    {
        var l = lst;
        while(l != Interlocked.CompareExchange(ref lst, l.Insert(0, ip), l)) l = lst;
    }

    private void InterlockedAppend<T>(ref ImmutableList<T> lst, T ip)
    {
        var l = lst;
        while (l != Interlocked.CompareExchange(ref lst, l.Add(ip), l)) l = lst;
    }
}