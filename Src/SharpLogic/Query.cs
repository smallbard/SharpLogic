using System.Collections;

using SharpLogic.ByteCodeVM;
using SharpLogic.ByteCodeVM.Compilation;
using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic;

public class Query<T> : IEnumerable<T>
{
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly KbdCodeContainer _kbdCodeContainer;
    private readonly ByteCodeContainer _queryCode;
    private readonly Dictionary<string, byte> _variables;

    public Query(ValueConstants valueConstants, ManagedConstants managedConstants, KbdCodeContainer codeContainers, ByteCodeContainer queryCode)
    {
        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _kbdCodeContainer = codeContainers;
        _queryCode = queryCode;
        _variables = new Dictionary<string, byte>();
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (_queryCode.CodeLength == 0) return Enumerable.Empty<T>().GetEnumerator();
        return CompileAndExecute();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private ByteCodeExecutor<T> CompileAndExecute()
    {
        return new ByteCodeExecutor<T>(_valueConstants, _managedConstants, new ExecutionCodeContainer(_kbdCodeContainer, _queryCode.Code, _managedConstants, _valueConstants));
    }
}