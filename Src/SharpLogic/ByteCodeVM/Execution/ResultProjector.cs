using System.Runtime.CompilerServices;

namespace SharpLogic.ByteCodeVM.Execution;

public class ResultProjector<TResult>
{
    private readonly StackFrame _rootEnvironment;

    public ResultProjector(StackFrame currentEnvironment)
    {
        _rootEnvironment = currentEnvironment;
        while(_rootEnvironment.PreviousStackFrame != null) _rootEnvironment = _rootEnvironment.PreviousStackFrame;
    }

    public TResult Result
    {
        get
        {
            // Todo : lot of boxing to remove

            if (typeof(ITuple).IsAssignableFrom(typeof(TResult)))
            {
                var result = Activator.CreateInstance(typeof(TResult)); // boxing needed to set field values with reflection
                var fields = typeof(TResult).GetFields();

                var variables = _rootEnvironment.Registers.GetVariables().ToDictionary(v => v.Name, v => v);

                for (var i = 0; i < fields.Length; i++)
                {
                    if (!variables.TryGetValue(fields[i].Name, out var variable)) continue;
                    if (variable?.Value is ValueTuple<ReadOnlyMemory<byte>, Type> tup)
                        fields[i].SetValue(result, ValueConstants.GetRealValueConstant(tup));
                    else
                        fields[i].SetValue(result, variable?.Value);
                }

                return (TResult)result!;
            }
            else if (typeof(TResult) == typeof(string))
                return (TResult)(object)_rootEnvironment.Registers.GetVariables().FirstOrDefault()?.Value?.ToString()!;
            else if (typeof(TResult).IsPrimitive || typeof(TResult) == typeof(decimal))
            {
                var variable = _rootEnvironment.Registers.GetVariables().FirstOrDefault();
                if (variable?.Value is ValueTuple<ReadOnlyMemory<byte>, Type> tup)
                    return (TResult)ValueConstants.GetRealValueConstant(tup);
                else if (variable?.Value == null)
                    throw new SharpLogicException($"Variable {variable?.Name} can't be null.");
                else
                    return (TResult)((object)variable?.Value!);
            }
            else
            {
                var result = Activator.CreateInstance(typeof(TResult)); // boxing needed to set property/field values with reflection for struct

                foreach (var variable in _rootEnvironment.Registers.GetVariables())
                {
                    var property = typeof(TResult).GetProperty(variable.Name);
                    if (property != null)
                    {
                        if (variable?.Value is ValueTuple<ReadOnlyMemory<byte>, Type> tupP)
                            property.SetValue(result, ValueConstants.GetRealValueConstant(tupP));
                        else
                            property.SetValue(result, variable?.Value);
                        continue;
                    }

                    var field = typeof(TResult).GetField(variable.Name);
                    if (field == null) continue;

                     if (variable?.Value is ValueTuple<ReadOnlyMemory<byte>, Type> tupF)
                        field.SetValue(result, ValueConstants.GetRealValueConstant(tupF));
                    else
                        field.SetValue(result, variable?.Value);
                }

                return (TResult)result!;
            }
        }
    }
}