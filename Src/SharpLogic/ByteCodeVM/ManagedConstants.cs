namespace SharpLogic.ByteCodeVM;

public class ManagedConstants
{
    private const int cstReferenceConstantBufferSize = 50;
    private List<object> _managedConstants;
    private readonly Dictionary<int, Dictionary<int, int>> _alreadyAdded;
    private readonly ManagedConstants? _baseConstants;

    public ManagedConstants(ManagedConstants? baseConstants)
    {
        _managedConstants = new List<object>(cstReferenceConstantBufferSize);
        _alreadyAdded = new Dictionary<int, Dictionary<int, int>>();
        _baseConstants = baseConstants;
    }

    public int AddConstant(object cst)
    {
        if (cst == null) throw new ArgumentNullException(nameof(cst));

        if (!_alreadyAdded.TryGetValue(cst.GetType().GetHashCode(), out var offsets)) _alreadyAdded[cst.GetType().GetHashCode()] = offsets = new Dictionary<int, int>();
        if (offsets.TryGetValue(cst.GetHashCode(), out int offset) && offset < _managedConstants.Count) return offset;
        
        _managedConstants.Add(cst);
        offsets[cst.GetHashCode()] = _managedConstants.Count -1;

        return _managedConstants.Count -1 + (_baseConstants == null ? 0 : _baseConstants._managedConstants.Count);
    }

    public object GetConstant(int index)
    {
        if (_baseConstants != null)
        {
            if (index < _baseConstants._managedConstants.Count)
                return _baseConstants.GetConstant(index);

            index = index - _baseConstants._managedConstants.Count;
        }

        return _managedConstants[index]!;
    }
}