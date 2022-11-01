using System.Buffers.Binary;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SharpLogic.ByteCodeVM.Definition;

namespace SharpLogic.ByteCodeVM.Execution;

public class ByteCodeExecutor<TResult> : IEnumerator<TResult>
{
    internal static readonly int[] OpCodeArgumentSizes = new[]
    {
        2, //UnValCst
        2, //UnRefCst
        5, //UnValCstLgIdx
        5, //UnRefCstLgIdx
        1, //UnTrue
        1, //UnFalse
        1, //UnNull
        2, //StackPxToAy
        0, //NewEnvironment
        4, //Goal
        0, //Proceed
        0, //Fail
        0, //SwitchNot
        3, //GreaterThan
        3, //LessThan
        3, //GreaterThanOrEqual
        3, //LessThanOrEqual
        3, //Equal
        3, //NotEqual

        3, //Add
        3, //Substract
        3, //Multiply
        3, //Divide
        3, //Modulus

        0, //Cut
        2, //UnifyReg
        5, //NewVar
        5, //OfType
        6, //MbAccess
    };

    private readonly OpCodeExecuteDelegate[] _opCodeExecutes;
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly FactAndRule _factAndRule;
    private readonly ReadOnlyMemory<byte> _queryCode;
    private readonly List<StackFrame> _environment;

    private StackFrame _currentStackFrame;
    private bool _firstExecution = true;
    private bool _failed;

    public ByteCodeExecutor(ValueConstants valueConstants, ManagedConstants managedConstants, FactAndRule factAndRule, ReadOnlyMemory<byte> queryCode)
    {
        _opCodeExecutes = new OpCodeExecuteDelegate[]
        {
            UnValCst,
            UnRefCst,
            UnValCstLgIdx,
            UnRefCstLgIdx,
            UnTrue,
            UnFalse,
            UnNull,
            StackPxToAy,
            NewEnvironment,
            Goal,
            Proceed,
            Fail,
            SwitchNot,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual,
            Equal,
            NotEqual,
            Add,
            Substract,
            Multiply,
            Divide,
            Modulus,
            Cut,
            UnifyReg,
            NewVar,
            OfType,
            MbAccess,
    };

        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _factAndRule = factAndRule;
        _queryCode = queryCode;
        _environment = new List<StackFrame>();
        _currentStackFrame = new StackFrame(null);
    }

    public TResult Current => new ResultProjector<TResult>(_currentStackFrame).Result;

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        _failed = false;

        if (_firstExecution)
        {
            var factAndRuleCode = _factAndRule.Code.Span;
            Run(factAndRuleCode.Length, factAndRuleCode, _queryCode.Span);
            _firstExecution = false;
        }
        else if (TryBacktrack(out int p))
            Run(p, _factAndRule.Code.Span, _queryCode.Span);
        else
            return false;

        return !_failed;
    }

    public void Reset()
    {
        _environment.Clear();
    }

    public void Dispose() { }

    private void Run(int p, ReadOnlySpan<byte> factAndRuleCode, ReadOnlySpan<byte> queryCode)
    {
        var totalCodeLength = factAndRuleCode.Length + _queryCode.Length;
        while (p < totalCodeLength)
        {
            var code = factAndRuleCode;
            var realP = p;
            if (p >= factAndRuleCode.Length)
            {
                code = queryCode;
                realP = p - factAndRuleCode.Length;
            }

            var opCode = code[realP];
            var argumentsSize = OpCodeArgumentSizes[opCode];

            _opCodeExecutes[opCode](code.Slice(realP + 1, argumentsSize), ref p);

            if (_failed && !TryBacktrack(out p))
            {
                if (_currentStackFrame?.PreviousStackFrame != null && _currentStackFrame.PreviousStackFrame.InNegation)
                    Proceed(Span<byte>.Empty, ref p);
                else
                    break;
            } 
        }
    }

    private bool TryBacktrack(out int p)
    {
        for (var i = _environment.Count - 1; i >= 0; i--)
        {
            var e = _environment[i];
            if (e.Choices == null)
            {
                foreach (var variable in e.Registers.GetVariables()) variable.Unbind(e);
                _environment.RemoveAt(i);
            }
            else
                break;
        }

        if (_environment.Count > 0)
        {
            _currentStackFrame = _environment[_environment.Count - 1];
            foreach (var variable in _currentStackFrame.Registers.GetVariables()) variable.Unbind(_currentStackFrame);
            var offsets = _currentStackFrame.Choices;
            p = offsets!.Pop();
            if (offsets.Count == 0) _currentStackFrame.Choices = null; 

            _failed = false;

            return true;
        }

        p = int.MaxValue;
        return false;
    }

    private void UnValCst(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(_valueConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCst(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(_managedConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnValCstLgIdx(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(_valueConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCstLgIdx(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(_managedConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnTrue(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(true, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnFalse(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify(false, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnNull(ReadOnlySpan<byte> arguments, ref int p)
    {
        Unify<object>(null, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void StackPxToAy(ReadOnlySpan<byte> arguments, ref int p)
    {
        var parRegister = _currentStackFrame!.PreviousStackFrame!.Registers[arguments[0]];
        var argRegister = _currentStackFrame!.Registers[arguments[1]];
        argRegister.Value = parRegister.Value;
        p += 1 + arguments.Length;
    }

    private void NewEnvironment(ReadOnlySpan<byte> arguments, ref int p)
    {
        _currentStackFrame = new StackFrame(_currentStackFrame);
        _environment.Add(_currentStackFrame);

        p += 1 + arguments.Length;
    }

    private void Goal(ReadOnlySpan<byte> arguments, ref int p)
    {
        var functor = (string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments));
        var arity = _currentStackFrame!.Registers.Count;

        var offsets = _factAndRule.GetOffsets(functor, arity);
        var offsetsCount = offsets.Count();

        if (offsetsCount == 0)
        {
            _failed = true;
            return;
        }
        else if (offsetsCount > 1)
            _currentStackFrame!.Choices = new Stack<int>(offsets.Skip(1).Reverse());

        _currentStackFrame!.CP = p + 1 + arguments.Length;

        p = offsets.First();
    }

    private void Proceed(ReadOnlySpan<byte> arguments, ref int p)
    {
        if (_currentStackFrame.PreviousStackFrame == null)
            p = int.MaxValue;
        else
        {
            var e = _currentStackFrame;
            _currentStackFrame = _currentStackFrame.PreviousStackFrame;
            p = e.CP;
        }
    }

    private void Fail(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = true;
        p+= 1 + arguments.Length;
    }

    private void SwitchNot(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _currentStackFrame!.InNegation && !_failed;

        _currentStackFrame!.InNegation = !_currentStackFrame!.InNegation;
        p+= 1 + arguments.Length;
    }

    private void GreaterThan(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c > 0);
    }

    private void LessThan(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c < 0);
    }

    private void GreaterThanOrEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c >= 0);
    }

    private void LessThanOrEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c <= 0);
    }

    private void Equal(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c == 0);
    }

    private void NotEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, c => c != 0);
    }

    private void CompareTo(ReadOnlySpan<byte> arguments, ref int p, Func<int, bool> compare)
    {
        var firstOperand = _currentStackFrame!.Registers[arguments[0]];
        var secondOperand = _currentStackFrame!.Registers[arguments[1]];

        if (firstOperand.Type == RegisterValueType.Unbound || secondOperand.Type == RegisterValueType.Unbound)
            _failed = true;
        else
        {
            var v1 = GetRealValueInRegister(firstOperand) as System.IComparable;
            if (v1 != null)
            {
                var v2 = GetRealValueInRegister(secondOperand) as System.IComparable;
                _failed = v2 == null || !compare(v1.CompareTo(v2));
            }
            else
                _failed = true;
        }

        Unify(_failed, _currentStackFrame!.Registers[arguments[2]]);

        p += 1 + arguments.Length;
    }

    private void Add(ReadOnlySpan<byte> arguments, ref int p)
    {
        ArithmeticOp(arguments, ref p, "op_Addition");
    }

    private void Substract(ReadOnlySpan<byte> arguments, ref int p)
    {
        ArithmeticOp(arguments, ref p, "op_Subtraction");
    }

    private void Multiply(ReadOnlySpan<byte> arguments, ref int p)
    { 
        ArithmeticOp(arguments, ref p, "op_Multiply");
    }

    private void Divide(ReadOnlySpan<byte> arguments, ref int p)
    {
        ArithmeticOp(arguments, ref p, "op_Division");
    }

    private void Modulus(ReadOnlySpan<byte> arguments, ref int p)
    {
        ArithmeticOp(arguments, ref p, "op_Modulus");
    }

    private void ArithmeticOp(ReadOnlySpan<byte> arguments, ref int p, string operatorName)
    {
        var firstOperand = _currentStackFrame!.Registers[arguments[0]];
        var secondOperand = _currentStackFrame!.Registers[arguments[1]];
        object? result = null;

        if (firstOperand.Type == RegisterValueType.Unbound || secondOperand.Type == RegisterValueType.Unbound)
            _failed = true;
        else
        {
            var v1 = GetRealValueInRegister(firstOperand) as System.IComparable;
            if (v1 != null)
            {
                var v2 = GetRealValueInRegister(secondOperand) as System.IComparable;
                if (v1 is int i1 && v2 is int i2)
                {
                    if (operatorName == "op_Addition")
                        result = i1 + i2;
                    else if (operatorName == "op_Subtraction")
                        result = i1 - i2;
                    else if (operatorName == "op_Multiply")
                        result = i1 * i2;
                    else if (operatorName == "op_Division")
                        result = i1 / i2;
                    else if (operatorName == "op_Modulus")
                        result = i1 % i2;
                }
                else if (v2 == null)
                    _failed = true;
                else
                {
                    var operatorMethod = v1.GetType().GetMethod(operatorName, BindingFlags.Static | BindingFlags.Public);
                    result = v1.GetType().GetMethod(operatorName, BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new[] { v1, v2 });
                }
            }
            else
                _failed = true;
        }

        if (!_failed) Unify(result, _currentStackFrame.Registers[arguments[2]]);
        
        p += 1 + arguments.Length;
    }

    private void Cut(ReadOnlySpan<byte> arguments, ref int p)
    {
        var e = _currentStackFrame;
        while (e != null && e.Choices == null) e = e.PreviousStackFrame;

        if (e != null) e.Choices = null;

        p += 1 + arguments.Length;
    }

    private void UnifyReg(ReadOnlySpan<byte> arguments, ref int p)
    {
        var firstRegister = _currentStackFrame.Registers[arguments[0]];
        var secondRegister = _currentStackFrame.Registers[arguments[1]];

        if (firstRegister.Type == RegisterValueType.Unbound && secondRegister.Type != RegisterValueType.Unbound)
            firstRegister.Value = secondRegister.Type;
        else if (firstRegister.Type != RegisterValueType.Unbound && secondRegister.Type == RegisterValueType.Unbound)
            secondRegister.Value = firstRegister.Value;
        else if (firstRegister.Type != RegisterValueType.Unbound && secondRegister.Type != RegisterValueType.Unbound)
        {
            if (firstRegister.Type != RegisterValueType.Variable && secondRegister.Type == RegisterValueType.Variable)
                Unify(firstRegister.Value, secondRegister);
            else if (firstRegister.Type == RegisterValueType.Variable && secondRegister.Type != RegisterValueType.Variable)
                Unify(secondRegister.Value, firstRegister);
            else if (firstRegister.Type == RegisterValueType.Variable && secondRegister.Type == RegisterValueType.Variable)
            {
                var var1 = (QueryVariable)firstRegister.Value!;
                var var2 = (QueryVariable)secondRegister.Value!;

                if (var1.IsBound && !var2.IsBound)
                    Unify(var1.Value, secondRegister);
                else if (!var1.IsBound && var2.IsBound)
                    Unify(var2.Value, firstRegister);
                else if (var1.IsBound && var2.IsBound)
                    Unify(var1.Value, var2.Value);
                else
                    _failed = true;
            }
            else
                Unify(firstRegister.Value, secondRegister);
        }

        p += 1 + arguments.Length;
    }

    private void NewVar(ReadOnlySpan<byte> arguments, ref int p)
    {
        var register = _currentStackFrame.Registers[arguments[4]];
        register.Value = new QueryVariable((string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments)));

        p += 1 + arguments.Length;
    }

    private void OfType(ReadOnlySpan<byte> arguments, ref int p)
    {
        var typeHashCode = BitConverter.ToInt32(arguments);
        var register = _currentStackFrame.Registers[arguments[4]];

        if (register.Type == RegisterValueType.Unbound)
            _failed = true;
        else if (register.Type == RegisterValueType.Variable && !((QueryVariable)register.Value!).IsBound)
            _failed = true;

        _failed = GetRealValueInRegister(register)?.GetType().GetHashCode() != typeHashCode;

        p += 1 + arguments.Length;
    }

    private void MbAccess(ReadOnlySpan<byte> arguments, ref int p)
    {
        var memberName = (string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments));
        var objRegister = _currentStackFrame.Registers[arguments[4]];
        var valueRegister  = _currentStackFrame.Registers[arguments[5]];

        if (objRegister.Type == RegisterValueType.Unbound || (objRegister.Type == RegisterValueType.Variable && !((QueryVariable)objRegister.Value!).IsBound))
        {
            _failed = true;
            return;
        }

        var obj = GetRealValueInRegister(objRegister);
        if (obj == null)
        {
            _failed = true;
            return;
        }

        var property = obj.GetType().GetProperty(memberName);
        if (property != null)
        {
            valueRegister.Value = property.GetValue(obj);
            
            p += 1 + arguments.Length;
            return;
        }

        var field = obj.GetType().GetField(memberName);
        if (field == null)
        {
            _failed = true;
            return;
        }

        valueRegister.Value = field.GetValue(obj);

        p += 1 + arguments.Length;
    }

    private void Unify<TValue>(TValue? value, RegisterValue register)
    {
        if (register.Type == RegisterValueType.Unbound)
            register.Value = value;
        else if (register.Type == RegisterValueType.Constant)
            Unify(value, register.Value);
        else if (register.Type == RegisterValueType.Variable)
        {
            var variable = (QueryVariable)register.Value!;
            if (variable.IsBound)
                Unify(value, ((QueryVariable)register.Value!).Value);
            else
                variable.Bind(value, _currentStackFrame!);
        }
    }

    private void Unify<TValue>(TValue? value, object? registerValue)
    {
        if (value is ValueTuple<ReadOnlyMemory<byte>, Type> cst)
        {
            if (registerValue is ValueTuple<ReadOnlyMemory<byte>, Type> registerMem && cst.Item1.Length == registerMem.Item1.Length)
            {
                var cstSpan = cst.Item1.Span;
                var rvSpan = registerMem.Item1.Span;
                for (var i = 0; i < cst.Item1.Length; i++)
                    if (cstSpan[i] != rvSpan[i])
                    {
                        _failed = true;
                        break;
                    }
            }
            else if (ValueConstants.GetRealValueConstant(cst).Equals(registerValue))
                _failed = false;
            else
            {
                _failed = true;
            }
        }
        else if (value is bool b)
        {
            _failed = !b.Equals(registerValue);
        }
        else if (value is object obj)
        {
            _failed = !object.Equals(value, registerValue);
        }
    }

    private object? GetRealValueInRegister(RegisterValue register)
    {
        if (register.Type == RegisterValueType.Unbound) throw new SharpLogicException("Register must be bound.");

        if (register.Type == RegisterValueType.Variable && register.Value is QueryVariable v)
        {
            if (!v.IsBound) throw new SharpLogicException($"Variable must be bound : '{v.Name}'.");

            if (v.Value is ValueTuple<ReadOnlyMemory<byte>, Type> cstVar)
                return ValueConstants.GetRealValueConstant(cstVar);

            return v.Value;
        }

        if (register.Value is ValueTuple<ReadOnlyMemory<byte>, Type> cst)
            return ValueConstants.GetRealValueConstant(cst);

        return register.Value;
    }

    private delegate void OpCodeExecuteDelegate(ReadOnlySpan<byte> arguments, ref int p);
}