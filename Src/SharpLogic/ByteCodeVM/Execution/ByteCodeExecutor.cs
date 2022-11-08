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

using SharpLogic.ByteCodeVM.Compilation;

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
        2, //UnifyReg
        1, //UnifyEmpty
        2, //UnifyHead
        3, //UnifyNth
        2, //UnifyTail
        5, //UnifyLen

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
        5, //NewVar
        5, //OfType
        6, //MbAccess
    };

    private readonly OpCodeExecuteDelegate[] _opCodeExecutes;
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) _factAndRule;
    private readonly ReadOnlyMemory<byte> _queryCode;
    private readonly List<StackFrame> _environment;
    private readonly Unification _unification;

    private StackFrame _currentStackFrame;
    private bool _firstExecution = true;
    private bool _failed;

    public ByteCodeExecutor(ValueConstants valueConstants, ManagedConstants managedConstants, (ByteCodeContainer Code,  GetOffsetsDelegate GetOffsets) factAndRule, ReadOnlyMemory<byte> queryCode)
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
            UnifyReg,
            UnifyEmpty,
            UnifyHead,
            UnifyNth,
            UnifyTail,
            UnifyLen,

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
        _unification = new Unification(() => _currentStackFrame);
    }

    public TResult Current => new ResultProjector<TResult>(_currentStackFrame).Result;

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        _failed = false;

        if (_firstExecution)
        {
            var factAndRuleCode = _factAndRule.Code.Code.Span;
            Run(factAndRuleCode.Length, factAndRuleCode, _queryCode.Span);
            _firstExecution = false;
        }
        else if (TryBacktrack(out int p))
            Run(p, _factAndRule.Code.Code.Span, _queryCode.Span);
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
        _failed = _unification.Unify(_valueConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCst(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify(_managedConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnValCstLgIdx(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify(_valueConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCstLgIdx(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify(_managedConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnTrue(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify(true, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnFalse(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify(false, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnNull(ReadOnlySpan<byte> arguments, ref int p)
    {
        _failed = _unification.Unify<object>(null, _currentStackFrame!.Registers[arguments[0]]);
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
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) > 0);
    }

    private void LessThan(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) < 0);
    }

    private void GreaterThanOrEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) >= 0);
    }

    private void LessThanOrEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) <= 0);
    }

    private void Equal(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, (v1, v2) => object.Equals(v1, v2));
    }

    private void NotEqual(ReadOnlySpan<byte> arguments, ref int p)
    {
        CompareTo(arguments, ref p, (v1, v2) => !object.Equals(v1, v2));
    }

    private void CompareTo(ReadOnlySpan<byte> arguments, ref int p, Func<object?, object?, bool> compare)
    {
        var firstOperand = _currentStackFrame!.Registers[arguments[0]];
        var secondOperand = _currentStackFrame!.Registers[arguments[1]];

        if (firstOperand.Type == RegisterValueType.Unbound || secondOperand.Type == RegisterValueType.Unbound)
            _failed = true;
        else
            _failed = !compare(firstOperand.RealValue, secondOperand.RealValue);

        _unification.Unify(_failed, _currentStackFrame!.Registers[arguments[2]]);

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
            var v1 = firstOperand.RealValue;
            if (v1 != null)
            {
                var v2 = secondOperand.RealValue as System.IComparable;
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

        if (!_failed) _failed = _unification.Unify(result, _currentStackFrame.Registers[arguments[2]]);
        
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

        _failed = _unification.Unify(firstRegister, secondRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyEmpty(ReadOnlySpan<byte> arguments, ref int p)
    {
        var register = _currentStackFrame!.Registers[arguments[0]];
        _failed = _unification.UnifyEmpty(register);

        p += 1 + arguments.Length;
    }
    
    private void UnifyHead(ReadOnlySpan<byte> arguments, ref int p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var headRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyHead(lstRegister, headRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyNth(ReadOnlySpan<byte> arguments, ref int p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var valueRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyNth(lstRegister, valueRegister, arguments[2]);

        p += 1 + arguments.Length;
    }

    private void UnifyTail(ReadOnlySpan<byte> arguments, ref int p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var tailRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyTail(lstRegister, tailRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyLen(ReadOnlySpan<byte> arguments, ref int p)
    {
        var length = BitConverter.ToInt32(arguments);
        var lstRegister = _currentStackFrame!.Registers[arguments[4]];

        _failed = _unification.UnifyLen(lstRegister, length);

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

        _failed = register.RealValue?.GetType().GetHashCode() != typeHashCode;

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

        var obj = objRegister.RealValue;
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

    private delegate void OpCodeExecuteDelegate(ReadOnlySpan<byte> arguments, ref int p);
}