using System.Buffers.Binary;

namespace SharpLogic.ByteCodeVM.Definition;

public static class ByteCodeGen
{
    public static void UnifyConstant(ByteCodeContainer codeContainer, TermValue tValue, int rxIndex, Span<byte> byte5, ValueConstants valueConstants, ManagedConstants managedConstants)
    {
        var byte1 = byte5.Slice(0, 1);
        var byte2 = byte5.Slice(0, 2);

        if (tValue is Variable || tValue is Term) throw new SharpLogicException($"A value was expected, not a {tValue.GetType().Name}.");

        if (tValue.Value == null)
        {
            byte1[0] = (byte)rxIndex;
            codeContainer.AppendOpCode(OpCode.UnNull, byte1);
        }
        else if (tValue.Value is bool b)
        {
            byte1[0] = (byte)rxIndex;
            codeContainer.AppendOpCode(b ? OpCode.UnTrue : OpCode.UnFalse, byte1);
        }
        else if (valueConstants.TryAddBoxedConstant(tValue.Value, out var index))
        {
            if (index > 254)
            {
                WriteInt(byte5, index);
                byte5[4] = (byte)rxIndex;
                codeContainer.AppendOpCode(OpCode.UnValCstLgIdx, byte5);
            }
            else
            {
                byte2[0] = (byte)index;
                byte2[1] = (byte)rxIndex;
                codeContainer.AppendOpCode(OpCode.UnValCst, byte2);
            }
        }
        else
        {
            index = managedConstants.AddConstant(tValue.Value!);
            if (index > 254)
            {
                WriteInt(byte5, index);
                byte5[4] = (byte)rxIndex;
                codeContainer.AppendOpCode(OpCode.UnRefCstLgIdx, byte5);
            }
            else
            {
                byte2[0] = (byte)index;
                byte2[1] = (byte)rxIndex;
                codeContainer.AppendOpCode(OpCode.UnRefCst, byte2);
            }
        }
    }

    public static void PrepareVariableForGoalInRule(Span<byte> byte5, ByteCodeContainer codeContainer, TermValue[] head, Dictionary<string, byte> freeVariables, ManagedConstants managedConstants, Variable variable, byte aYIndex, ref byte freeRegister)
    {
        var byte2 = byte5.Slice(0, 2);
        var registerIndexInt = Array.IndexOf(head, variable);
        var registerIndex = (byte)registerIndexInt;
        if (registerIndexInt == -1 && !freeVariables.TryGetValue(variable.VariableName, out registerIndex)) throw new SharpLogicException($"Variable {variable.VariableName} shoulde have been already define.");

        byte2[0] = (byte)registerIndex;
        byte2[1] = (byte)aYIndex;
        codeContainer.AppendOpCode(OpCode.StackPxToAy, byte2);
    }

    public static void PrepareVariableForGoalInQuery(Span<byte> byte5, ByteCodeContainer codeContainer, Dictionary<string, byte> freeVariables, ManagedConstants managedConstants, Variable variable, byte aYIndex, ref byte freeRegister)
    {
        PrepareVariableForGoalInRule(byte5, codeContainer, Array.Empty<TermValue>(), freeVariables, managedConstants, variable, aYIndex, ref freeRegister);
    }

    public static void WriteInt(Span<byte> bytes, int value)
    {
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        else
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
    }
}