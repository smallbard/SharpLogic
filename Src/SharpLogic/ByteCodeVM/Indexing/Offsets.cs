using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Indexing;

public class Offsets
{
    private List<int> _all = new List<int>();
    private Dictionary<int, List<int>> _firstEltIndex = new Dictionary<int, List<int>>();
    private List<int> _nonConstantFirstTerms = new List<int>();

    public void AddOffset(int offset, TermValue[] head)
    {
        _all.Add(offset);

        if (head.Length == 0) return;

        if (head[0] is Variable || head[0] is Term)
        {
            if (_nonConstantFirstTerms == null) _nonConstantFirstTerms = new List<int>();
            _nonConstantFirstTerms.Add(offset);
        }
        else
        {
            var value = head[0].Value;
            if (value == null) return;

            if (_firstEltIndex == null) _firstEltIndex = new Dictionary<int, List<int>>();
            if (!_firstEltIndex.TryGetValue(value.GetHashCode(), out var lst)) _firstEltIndex[value.GetHashCode()] = lst = new List<int>();

            lst.Add(offset);
        }
    }

    public IEnumerable<int> GetOffsets(Registers registers)
    {
        if (registers.Count > 0)
        {
            IEnumerable<int>? offsets = null;

            var firstRegister = registers[0];
            if (firstRegister.Type == RegisterValueType.Constant && firstRegister.Value != null)
            {
                if (_firstEltIndex.ContainsKey(firstRegister.Value.GetHashCode())) offsets = _firstEltIndex[firstRegister.Value.GetHashCode()];
            }
            else if (firstRegister.Value is QueryVariable v && v.Instantiated && v.Value != null)
            {
                if (_firstEltIndex.ContainsKey(v.Value.GetHashCode())) offsets = _firstEltIndex[v.Value.GetHashCode()];
            }

            if (offsets != null && _nonConstantFirstTerms.Count > 0) 
                return offsets.Union(_nonConstantFirstTerms).OrderBy(o => o);
            else if (offsets != null)
                return offsets;
        }

        return _all;
    }
}