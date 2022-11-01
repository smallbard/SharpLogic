using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpLogic.ByteCodeVM.Execution;

public class ResultProjector<TResult>
{
    private readonly StackFrame _rootStackFrame;
    private readonly Dictionary<string, QueryVariable> _variables;
    private Func<TResult>? _project;

    public ResultProjector(StackFrame currentStackFrame)
    {
        _rootStackFrame = currentStackFrame;
        while(_rootStackFrame.PreviousStackFrame != null) _rootStackFrame = _rootStackFrame.PreviousStackFrame;
        _variables = _rootStackFrame.Registers.GetVariables().ToDictionary(v => v.Name, v => v);

        if (typeof(ITuple).IsAssignableFrom(typeof(TResult)))
        {
            var genericArguments = typeof(TResult).GetGenericArguments();
            var projectMethod = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == "ProjectValueTuple" && m.GetGenericArguments().Length == genericArguments.Length);
            if (projectMethod == null) throw new SharpLogicException($"Type {typeof(TResult)} not supported.");

            _project = projectMethod.MakeGenericMethod(genericArguments).CreateDelegate<Func<TResult>>(this);
        }
        else if (typeof(TResult) == typeof(string))
        {
            var v = _variables.Values.FirstOrDefault();
            if (v == null) throw new SharpLogicException("No variable found.");

            var valueAccess = Expression.MakeMemberAccess(Expression.Constant(v), typeof(QueryVariable).GetProperty(nameof(QueryVariable.Value))!);
            _project = Expression.Lambda<Func<TResult>>(Expression.Condition(
                Expression.NotEqual(valueAccess, Expression.Constant(null)), 
                Expression.Call(valueAccess, typeof(object).GetMethod(nameof(object.ToString))!), 
                Expression.Convert(Expression.Constant(null), typeof(string)))).Compile();
        }
        else if (typeof(TResult).IsPrimitive || typeof(TResult) == typeof(decimal))
        {
            var v = _variables.Values.FirstOrDefault();
            if (v == null) throw new SharpLogicException("No variable found.");

            var valueAccess = Expression.MakeMemberAccess(Expression.Constant(v), typeof(QueryVariable).GetProperty(nameof(QueryVariable.Value))!);

            _project = Expression.Lambda<Func<TResult>>(Expression.Condition(
               Expression.TypeIs(valueAccess, typeof(ValueTuple<ReadOnlyMemory<byte>, Type>)),
               Expression.Convert(Expression.Call(
                    typeof(ValueConstants).GetMethod(nameof(ValueConstants.GetRealValueConstant), BindingFlags.Static | BindingFlags.Public)!,
                    Expression.Convert(valueAccess, typeof(ValueTuple<ReadOnlyMemory<byte>, Type>))), typeof(TResult)),
               Expression.Convert(valueAccess, typeof(TResult)))).Compile();
        }
    }

    public TResult Result
    {
        get
        {
            if (_project != null) return _project();

            var result = Activator.CreateInstance(typeof(TResult)); // boxing needed to set property/field values with reflection for struct

            foreach (var variable in _rootStackFrame.Registers.GetVariables())
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

    private T ConvertVarValue<T>(object value)
    {
        if (value is ValueTuple<ReadOnlyMemory<byte>, Type> tup) return (T)ValueConstants.GetRealValueConstant(tup);
        return (T)value;
    }

    private ValueTuple<T1,T2> ProjectValueTuple<T1,T2>()
    {
        var result = new ValueTuple<T1, T2>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);

        return result;
    }

    private ValueTuple<T1,T2, T3> ProjectValueTuple<T1,T2, T3>()
    {
        var result = new ValueTuple<T1, T2, T3>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);
        if (_variables.TryGetValue("Item3", out v)) result.Item3 = ConvertVarValue<T3>(v.Value!);

        return result;
    }

    private ValueTuple<T1,T2, T3, T4> ProjectValueTuple<T1,T2, T3, T4>()
    {
        var result = new ValueTuple<T1, T2, T3, T4>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);
        if (_variables.TryGetValue("Item3", out v)) result.Item3 = ConvertVarValue<T3>(v.Value!);
        if (_variables.TryGetValue("Item4", out v)) result.Item4 = ConvertVarValue<T4>(v.Value!);

        return result;
    }

    private ValueTuple<T1,T2, T3, T4, T5> ProjectValueTuple<T1,T2, T3, T4, T5>()
    {
        var result = new ValueTuple<T1, T2, T3, T4, T5>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);
        if (_variables.TryGetValue("Item3", out v)) result.Item3 = ConvertVarValue<T3>(v.Value!);
        if (_variables.TryGetValue("Item4", out v)) result.Item4 = ConvertVarValue<T4>(v.Value!);
        if (_variables.TryGetValue("Item5", out v)) result.Item5 = ConvertVarValue<T5>(v.Value!);

        return result;
    }

    private ValueTuple<T1,T2, T3, T4, T5, T6> ProjectValueTuple<T1,T2, T3, T4, T5, T6>()
    {
        var result = new ValueTuple<T1, T2, T3, T4, T5, T6>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);
        if (_variables.TryGetValue("Item3", out v)) result.Item3 = ConvertVarValue<T3>(v.Value!);
        if (_variables.TryGetValue("Item4", out v)) result.Item4 = ConvertVarValue<T4>(v.Value!);
        if (_variables.TryGetValue("Item5", out v)) result.Item5 = ConvertVarValue<T5>(v.Value!);
        if (_variables.TryGetValue("Item6", out v)) result.Item6 = ConvertVarValue<T6>(v.Value!);

        return result;
    }

    private ValueTuple<T1,T2, T3, T4, T5, T6, T7> ProjectValueTuple<T1,T2, T3, T4, T5, T6, T7>()
    {
        var result = new ValueTuple<T1, T2, T3, T4, T5, T6, T7>();
        
        if (_variables.TryGetValue("Item1", out var v)) result.Item1 = ConvertVarValue<T1>(v.Value!);
        if (_variables.TryGetValue("Item2", out v)) result.Item2 = ConvertVarValue<T2>(v.Value!);
        if (_variables.TryGetValue("Item3", out v)) result.Item3 = ConvertVarValue<T3>(v.Value!);
        if (_variables.TryGetValue("Item4", out v)) result.Item4 = ConvertVarValue<T4>(v.Value!);
        if (_variables.TryGetValue("Item5", out v)) result.Item5 = ConvertVarValue<T5>(v.Value!);
        if (_variables.TryGetValue("Item6", out v)) result.Item6 = ConvertVarValue<T6>(v.Value!);
        if (_variables.TryGetValue("Item7", out v)) result.Item7 = ConvertVarValue<T7>(v.Value!);

        return result;
    }
}