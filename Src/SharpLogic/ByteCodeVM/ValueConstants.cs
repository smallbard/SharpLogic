using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SharpLogic.ByteCodeVM;                     

public class ValueConstants
{
    private const int cstValueConstantBufferSize = 500;
    private readonly static Type[] _supportedTypes = new[] { typeof(int), typeof(short), typeof(long), typeof(double), typeof(float), typeof(char), typeof(decimal) };
    private readonly ValueConstants? _baseConstants;
    private byte[] _valueConstants;
    private int _valueConstantsLength;

    public ValueConstants(ValueConstants? baseConstants)
    {
        _valueConstants = new byte[cstValueConstantBufferSize];
        _valueConstantsLength = 0;
        _baseConstants = baseConstants;
    }

    public bool TryAddBoxedConstant(object? cst, out int index)
    {
        index = -1;
        if (cst == null) return false;

        if (cst is int i)
            index = AddValueConstant(i, sizeof(int), BitConverter.IsLittleEndian ? BinaryPrimitives.WriteInt32LittleEndian : BinaryPrimitives.WriteInt32BigEndian);
        else if (cst is short s)
            index = AddValueConstant(s, sizeof(short), BitConverter.IsLittleEndian ? BinaryPrimitives.WriteInt16LittleEndian : BinaryPrimitives.WriteInt16BigEndian);
        else if (cst is long l)
            index = AddValueConstant(l, sizeof(long), BitConverter.IsLittleEndian ? BinaryPrimitives.WriteInt64LittleEndian : BinaryPrimitives.WriteInt64BigEndian);
        else if (cst is double d)
            index = AddValueConstant(d, sizeof(double), BitConverter.IsLittleEndian ? BinaryPrimitives.WriteDoubleLittleEndian : BinaryPrimitives.WriteDoubleBigEndian);
        else if (cst is float f)
            index = AddValueConstant(f, sizeof(float), BitConverter.IsLittleEndian ? BinaryPrimitives.WriteSingleLittleEndian : BinaryPrimitives.WriteSingleBigEndian);
        else if (cst is char c)
            index = AddValueConstant(c, sizeof(char), (dest, c) =>
            {
                var bytes = BitConverter.GetBytes(c);
                for (var i = 0; i < bytes.Length; i++) dest[i] = bytes[i];
            });
        else if (cst is decimal dec)
            index = AddValueConstant(dec, sizeof(decimal), (dest, d) =>
            {
                Span<int> decInts = stackalloc int[4];
                decimal.GetBits(d, decInts);
                var decBytes = MemoryMarshal.Cast<int, byte>(decInts);
                for (var i = 0; i < decBytes.Length; i++) dest[i] = decBytes[i];
            });
        else
            return false;

        return true;
    }

    public (ReadOnlyMemory<byte>, Type) GetConstant(int index)
    {
        if (_baseConstants != null)
        {
            if (index < _baseConstants._valueConstantsLength)
                return _baseConstants.GetConstant(index);

            index = index - _baseConstants._valueConstantsLength;
        }

        switch (_valueConstants[index])
        {
            
            case 0: //int
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(int)), typeof(int));
            case 1: //short
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(short)), typeof(short));
            case 2: //long
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(long)), typeof(long));
            case 3: //double
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(double)), typeof(double));
            case 4: //float
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(float)), typeof(float));
            case 5: //char
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(char)), typeof(char));
            case 6: //decimal
                return (new ReadOnlyMemory<byte>(_valueConstants, index + 1, sizeof(decimal)), typeof(decimal));
            default:
                throw new SharpLogicException($"Invalid type index {_valueConstants[index]}");
        }
    }

    public object GetRealValueConstant(int index)
    {
        return GetRealValueConstant(GetConstant(index));
    }

    public static object GetRealValueConstant(in ValueTuple<ReadOnlyMemory<byte>, Type> tup)
    {
        if (tup.Item2 == typeof(int))
            return BitConverter.ToInt32(tup.Item1.Span);
        else if (tup.Item2 == typeof(short))
            return BitConverter.ToInt16(tup.Item1.Span);
        else if (tup.Item2 == typeof(long))
            return BitConverter.ToInt64(tup.Item1.Span);
        else if (tup.Item2 == typeof(double))
            return BitConverter.ToDouble(tup.Item1.Span);
        else if (tup.Item2 == typeof(float))
            return BitConverter.ToSingle(tup.Item1.Span);
        else if (tup.Item2 == typeof(char))
            return BitConverter.ToChar(tup.Item1.Span);
        else if (tup.Item2 == typeof(decimal))
            return new decimal(MemoryMarshal.Cast<byte, int>(tup.Item1.Span));
        else
            throw new SharpLogicException($"Constant type {tup.Item2.FullName} unsupported.");
    }

    private int AddValueConstant<TCst>(TCst cst, byte sizeofTCst, WriteCstDelegateDelegate<TCst> writeCst) where TCst : struct
    {
        if (_valueConstantsLength + sizeofTCst + 1 >= _valueConstants.Length)
        {
            var newArray = new byte[_valueConstants.Length + cstValueConstantBufferSize];
            Array.Copy(_valueConstants, newArray, _valueConstants.Length);
            _valueConstants = newArray;
        } 

        _valueConstants[_valueConstantsLength] = (byte)Array.IndexOf<Type>(_supportedTypes, typeof(TCst));

        var destination = new Span<byte>(_valueConstants, _valueConstantsLength + 1, sizeofTCst);
        writeCst(destination, cst);
        
        var cstAddress = _valueConstantsLength;
        _valueConstantsLength += sizeofTCst + 1;
        return cstAddress + (_baseConstants == null ? 0 : _baseConstants._valueConstantsLength);
    }

    private delegate void WriteCstDelegateDelegate<TCst>(Span<byte> destination, TCst cst);
}