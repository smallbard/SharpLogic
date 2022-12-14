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
    private readonly OpCodeExecuteDelegate[] _opCodeExecutes;
    private readonly ValueConstants _valueConstants;
    private readonly ManagedConstants _managedConstants;
    private readonly ExecutionCodeContainer _code;
    private readonly List<StackFrame> _environment;
    private readonly Unification _unification;

    private StackFrame _currentStackFrame;
    private bool _firstExecution = true;
    private bool _failed;

    public ByteCodeExecutor(ValueConstants valueConstants, ManagedConstants managedConstants, ExecutionCodeContainer code)
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

            Asserta,
            Assertz,
    };

        _valueConstants = valueConstants;
        _managedConstants = managedConstants;
        _code = code;
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
            Run(_code.StartPoint);
            _firstExecution = false;
        }
        else if (TryBacktrack(out InstructionPointer p))
            Run(p);
        else
            return false;

        return !_failed;
    }

    public void Reset()
    {
        _currentStackFrame = new StackFrame(null);
        _firstExecution = true;
        _environment.Clear();
    }

    public void Dispose() { }

    private void Run(InstructionPointer p)
    {
        while (!InstructionPointer.Invalid.Equals(p))
        {
            var opCode = _code.GetInstruction(p, out var arguments);

            _opCodeExecutes[(byte)opCode](arguments, ref p);

            if (_failed && !TryBacktrack(out p)) break;
        }
    }

    private bool TryBacktrack(out InstructionPointer p)
    {
        for (var i = _environment.Count - 1; i >= 0; i--)
        {
            var e = _environment[i];
            if (e.Choices == null)
            {
                e.UninstantiateVariables();
                _environment.RemoveAt(i);
            }
            else
                break;
        }

        if (_environment.Count > 0)
        {
            _currentStackFrame = _environment[_environment.Count - 1];
            _currentStackFrame.UninstantiateVariables();
            var offsets = _currentStackFrame.Choices;
            p = offsets!.Current;
            if (!offsets.MoveNext()) _currentStackFrame.Choices = null;

            _failed = false;

            return true;
        }

        p = InstructionPointer.Invalid;
        return false;
    }

    private void UnValCst(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(_valueConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCst(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(_managedConstants.GetConstant(arguments[0]), _currentStackFrame!.Registers[arguments[1]]);
        p+= 1 + arguments.Length;
    }

    private void UnValCstLgIdx(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(_valueConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnRefCstLgIdx(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(_managedConstants.GetConstant(BitConverter.ToInt32(arguments)), _currentStackFrame!.Registers[arguments[5]]);
        p+= 1 + arguments.Length;
    }

    private void UnTrue(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(true, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnFalse(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify(false, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void UnNull(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = _unification.Unify<object>(null, _currentStackFrame!.Registers[arguments[0]]);
        p+= 1 + arguments.Length;
    }

    private void StackPxToAy(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var parRegister = _currentStackFrame!.PreviousStackFrame!.Registers[arguments[0]];
        var argRegister = _currentStackFrame!.Registers[arguments[1]];
        argRegister.Value = parRegister.Value;
        p += 1 + arguments.Length;
    }

    private void NewEnvironment(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _currentStackFrame = new StackFrame(_currentStackFrame);
        _environment.Add(_currentStackFrame);

        p += 1 + arguments.Length;
    }

    private void Goal(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var functor = (string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments));
        var offsets = _code.GetOffsets(functor, _currentStackFrame!.Registers).GetEnumerator();

        if (!offsets.MoveNext())
        {
            _failed = true;
            return;
        }

        _currentStackFrame!.CP = p + 1 + arguments.Length;
        p = offsets.Current;

        if (offsets.MoveNext()) _currentStackFrame!.Choices = offsets;
    }

    private void Proceed(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        if (_currentStackFrame.PreviousStackFrame == null)
            p = InstructionPointer.Invalid;
        else
        {
            var e = _currentStackFrame;
            _currentStackFrame = _currentStackFrame.PreviousStackFrame;
            p = e.CP;
        }
    }

    private void Fail(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _failed = true;
        p+= 1 + arguments.Length;
    }

    private void GreaterThan(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) > 0);
    }

    private void LessThan(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) < 0);
    }

    private void GreaterThanOrEqual(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) >= 0);
    }

    private void LessThanOrEqual(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1 , v2) => v1 != null && v1 is IComparable c1 && v2 != null && c1.CompareTo(v2) <= 0);
    }

    private void Equal(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1, v2) => object.Equals(v1, v2));
    }

    private void NotEqual(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        CompareTo(arguments, ref p, (v1, v2) => !object.Equals(v1, v2));
    }

    private void CompareTo(ReadOnlySpan<byte> arguments, ref InstructionPointer p, Func<object?, object?, bool> compare)
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

    private void Add(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        ArithmeticOp(arguments, ref p, "op_Addition");
    }

    private void Substract(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        ArithmeticOp(arguments, ref p, "op_Subtraction");
    }

    private void Multiply(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    { 
        ArithmeticOp(arguments, ref p, "op_Multiply");
    }

    private void Divide(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        ArithmeticOp(arguments, ref p, "op_Division");
    }

    private void Modulus(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        ArithmeticOp(arguments, ref p, "op_Modulus");
    }

    private void ArithmeticOp(ReadOnlySpan<byte> arguments, ref InstructionPointer p, string operatorName)
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

    private void Cut(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var e = _currentStackFrame;
        while (e != null && e.Choices == null) e = e.PreviousStackFrame;

        if (e != null) e.Choices = null;

        p += 1 + arguments.Length;
    }

    private void UnifyReg(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var firstRegister = _currentStackFrame.Registers[arguments[0]];
        var secondRegister = _currentStackFrame.Registers[arguments[1]];

        _failed = _unification.Unify(firstRegister, secondRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyEmpty(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var register = _currentStackFrame!.Registers[arguments[0]];
        _failed = _unification.UnifyEmpty(register);

        p += 1 + arguments.Length;
    }
    
    private void UnifyHead(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var headRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyHead(lstRegister, headRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyNth(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var valueRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyNth(lstRegister, valueRegister, arguments[2]);

        p += 1 + arguments.Length;
    }

    private void UnifyTail(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var lstRegister = _currentStackFrame!.Registers[arguments[0]];
        var tailRegister = _currentStackFrame!.Registers[arguments[1]];

        _failed = _unification.UnifyTail(lstRegister, tailRegister);

        p += 1 + arguments.Length;
    }

    private void UnifyLen(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var length = BitConverter.ToInt32(arguments);
        var lstRegister = _currentStackFrame!.Registers[arguments[4]];

        _failed = _unification.UnifyLen(lstRegister, length);

        p += 1 + arguments.Length;
    }

    private void NewVar(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var register = _currentStackFrame.Registers[arguments[4]];
        register.Value = new QueryVariable((string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments)));

        p += 1 + arguments.Length;
    }

    private void OfType(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var typeHashCode = BitConverter.ToInt32(arguments);
        var register = _currentStackFrame.Registers[arguments[4]];

        if (register.Type == RegisterValueType.Unbound)
            _failed = true;
        else if (register.Type == RegisterValueType.Variable && !((QueryVariable)register.Value!).Instantiated)
            _failed = true;

        _failed = register.RealValue?.GetType().GetHashCode() != typeHashCode;

        p += 1 + arguments.Length;
    }

    private void MbAccess(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        var memberName = (string)_managedConstants.GetConstant(BitConverter.ToInt32(arguments));
        var objRegister = _currentStackFrame.Registers[arguments[4]];
        var valueRegister  = _currentStackFrame.Registers[arguments[5]];

        if (objRegister.Type == RegisterValueType.Unbound || (objRegister.Type == RegisterValueType.Variable && !((QueryVariable)objRegister.Value!).Instantiated))
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

    private void Asserta(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _code.Asserta(CloneAndInstantiate((Term)_managedConstants.GetConstant(BitConverter.ToInt32(arguments))));
        p += 1 + arguments.Length;
    }

    private void Assertz(ReadOnlySpan<byte> arguments, ref InstructionPointer p)
    {
        _code.Assertz(CloneAndInstantiate((Term)_managedConstants.GetConstant(BitConverter.ToInt32(arguments))));
        p += 1 + arguments.Length;
    }

    private Term CloneAndInstantiate(Term t) => t switch
    {
        Rule r => new Rule(r.Functor, r.Head.Select(h => CloneAndInstantiate(h)).ToArray(), r.Args.Select(a => CloneAndInstantiate(a)).ToArray()),
        ListPredicate lp => new ListPredicate(lp.Functor, lp.Args.Select(a => CloneAndInstantiate(a)).ToArray()),
        Predicate p => new Predicate(p.Functor, p.Args.Select(a => CloneAndInstantiate(a)).ToArray()),
        _ => new Term(t.Functor, t.Args.Select(a => CloneAndInstantiate(a)).ToArray())
    };

    private TermValue CloneAndInstantiate(TermValue tv) 
    {
        if (tv is Variable v)
        {
            var instanciatedVar = _currentStackFrame.Registers.GetVariables().FirstOrDefault(qv => qv.Instantiated && qv.Name == v.VariableName);
            if (instanciatedVar != null) return new TermValue(instanciatedVar.Value);
            return new Variable(v.VariableName);
        }

        return new TermValue(tv.Value);
    }

    private delegate void OpCodeExecuteDelegate(ReadOnlySpan<byte> arguments, ref InstructionPointer p);
}