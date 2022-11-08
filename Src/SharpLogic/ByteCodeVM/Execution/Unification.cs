namespace SharpLogic.ByteCodeVM.Execution;

public class Unification
{
    private readonly Func<StackFrame> _getCurrentStackFrame;

    public Unification(Func<StackFrame> getCurrentStackFrame)
    {
        _getCurrentStackFrame = getCurrentStackFrame;
    }

    public bool Unify(RegisterValue firstRegister, RegisterValue secondRegister)
    {
        if (firstRegister.Type == RegisterValueType.Unbound && secondRegister.Type != RegisterValueType.Unbound)
            firstRegister.Value = secondRegister.Value;
        else if (firstRegister.Type != RegisterValueType.Unbound && secondRegister.Type == RegisterValueType.Unbound)
            secondRegister.Value = firstRegister.Value;
        else if (firstRegister.Type != RegisterValueType.Unbound && secondRegister.Type != RegisterValueType.Unbound)
        {
            if (firstRegister.Type != RegisterValueType.Variable && secondRegister.Type == RegisterValueType.Variable)
                return Unify(firstRegister.Value, secondRegister);
            else if (firstRegister.Type == RegisterValueType.Variable && secondRegister.Type != RegisterValueType.Variable)
                return Unify(secondRegister.Value, firstRegister);
            else if (firstRegister.Type == RegisterValueType.Variable && secondRegister.Type == RegisterValueType.Variable)
            {
                var var1 = (QueryVariable)firstRegister.Value!;
                var var2 = (QueryVariable)secondRegister.Value!;

                return Unify(var1, var2);
            }
            else
                return Unify(firstRegister.Value, secondRegister);
        }

        return false;
    }

    public bool UnifyEmpty(RegisterValue register)
    {
        if (register.Type == RegisterValueType.Unbound || (register.Type == RegisterValueType.Variable && !((QueryVariable)register.Value!).IsBound))
            return true;

        var lst = register.RealValue;

        if (lst is System.Collections.ICollection c)
            return c.Count > 0;
        else if (lst is System.Collections.IEnumerable e)
            return e.GetEnumerator().MoveNext();

        return false;
    }

    public bool UnifyHead(RegisterValue lstRegister, RegisterValue headRegister)
    {
        if (lstRegister.Type == RegisterValueType.Unbound || (lstRegister.Type == RegisterValueType.Variable && !((QueryVariable)lstRegister.Value!).IsBound))
            return true;

        var lst = lstRegister.RealValue;
        if (lst is System.Collections.IEnumerable e)
        {
            var en = e.GetEnumerator();
            if (!en.MoveNext()) return true;

            var head = en.Current;
            return Unify(head, headRegister);
        }
        else
            return true;
    }

    public bool UnifyTail(RegisterValue lstRegister, RegisterValue tailRegister)
    {
        if (lstRegister.Type == RegisterValueType.Unbound || (lstRegister.Type == RegisterValueType.Variable && !((QueryVariable)lstRegister.Value!).IsBound))
            return true;

        var lst = lstRegister.RealValue;
        if (lst is System.Collections.IEnumerable e)
        {
            var tail = typeof(Enumerable).GetMethod(nameof(Enumerable.Skip))!.MakeGenericMethod(lst.GetType().GetInterfaces().First(i => i.Name == "IEnumerable`1").GetGenericArguments()[0]).Invoke(null, new object?[] { lst, 1 });
            return Unify(tail, tailRegister);
        }
        else
            return true;
    }

    public bool UnifyNth(RegisterValue lstRegister, RegisterValue valueRegister, byte n)
    {
        if (lstRegister.Type == RegisterValueType.Unbound || (lstRegister.Type == RegisterValueType.Variable && !((QueryVariable)lstRegister.Value!).IsBound))
            return true;

        var lst = lstRegister.RealValue;
        if (lst is System.Collections.IEnumerable e)
        {
            var en = e.GetEnumerator();
            var found = false;
            for (var i = 0; i <= n; i++) found = en.MoveNext();

            if (!found) return true;

            return Unify(en.Current, valueRegister);
        }
        else
            return true;
    }

    public bool UnifyLen(RegisterValue lstRegister, int length)
    {
        if (lstRegister.Type == RegisterValueType.Unbound || (lstRegister.Type == RegisterValueType.Variable && !((QueryVariable)lstRegister.Value!).IsBound))
            return true;

        var lst = lstRegister.RealValue;
        if (lst is System.Collections.ICollection c)
            return c.Count != length;
        else if (lst is System.Collections.IEnumerable e)
        {
            var count = 0;
            var en = e.GetEnumerator();
            while(en.MoveNext()) count++;
            return count != length;
        }

        return true;
    }

    public bool Unify<TValue>(TValue? value, RegisterValue register)
    {
        if (register.Type == RegisterValueType.Unbound)
            register.Value = value;
        else if (register.Type == RegisterValueType.Constant)
            return Unify(value, register.Value);
        else if (register.Type == RegisterValueType.Variable)
            return Unify((QueryVariable)register.Value!, value);

        return false;
    }

    private bool Unify(QueryVariable var1, QueryVariable var2)
    {
        if (var1.IsBound && !var2.IsBound)
            return Unify<object?>(var2, var1.Value);
        else if (!var1.IsBound && var2.IsBound)
            return Unify<object?>(var1, var2.Value);
        else if (var1.IsBound && var2.IsBound)
            return Unify(var1.Value, var2.Value);
        else
        {
            QueryVariable.MakeEquivalent(var1, var2, _getCurrentStackFrame());
            return false;
        }
    }

    private bool Unify<TValue>(QueryVariable variable, TValue? value)
    {
        if (variable.IsBound)
            return Unify(value, variable.Value);
        else
            variable.Bind(value, _getCurrentStackFrame());

        return false;
    }

    private bool Unify<TValue>(TValue? value, object? registerValue)
    {
        if (value is ValueTuple<ReadOnlyMemory<byte>, Type> cst)
        {
            if (registerValue is ValueTuple<ReadOnlyMemory<byte>, Type> registerMem && cst.Item1.Length == registerMem.Item1.Length)
            {
                var cstSpan = cst.Item1.Span;
                var rvSpan = registerMem.Item1.Span;
                for (var i = 0; i < cst.Item1.Length; i++)
                    if (cstSpan[i] != rvSpan[i]) return true;
            }
            else
                return !ValueConstants.GetRealValueConstant(cst).Equals(registerValue);
        }
        else if (value is bool b)
            return !b.Equals(registerValue);
        else if (!(value is string) && value is System.Collections.IEnumerable e1 && registerValue is System.Collections.IEnumerable e2)
        {
            var en1 = e1.GetEnumerator();
            var en2 = e2.GetEnumerator();

            while (en1.MoveNext())
            {
                if (!en2.MoveNext()) return true;
                return Unify(en1.Current, en2.Current);
            }
        }
        else
            return !object.Equals(value, registerValue);

        return false;
    }
}