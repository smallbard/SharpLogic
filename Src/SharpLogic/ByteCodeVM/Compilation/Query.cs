using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography;

using SharpLogic.ByteCodeVM.Execution;

namespace SharpLogic.ByteCodeVM.Compilation;

public class Query<T> : IEnumerable<T>
{
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) _factAndRule;
    private readonly ByteCodeContainer _queryCode;
    private readonly Dictionary<string, byte> _variables;

    public Query(ValueConstants valueConstants, ManagedConstants managedConstants, (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) factAndRule, ByteCodeContainer queryCode)
    {
        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _factAndRule = factAndRule;
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
        return new ByteCodeExecutor<T>(_valueConstants, _managedConstants, new ExecutionCodeContainer(_factAndRule, _queryCode.Code));
    }
}