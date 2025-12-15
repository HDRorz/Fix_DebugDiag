using System;
using System.Reflection;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopInstanceField : ClrInstanceField
{
	private readonly DesktopGCHeap _heap;

	private readonly Lazy<BaseDesktopHeapType> _type;

	private readonly IFieldData _field;

	private readonly FieldAttributes _attributes;

	private ClrElementType _elementType;

	public override uint Token { get; }

	public override bool IsPublic => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

	public override bool IsPrivate => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

	public override bool IsInternal => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

	public override bool IsProtected => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

	public override bool IsObjectReference => ((ClrElementType)_field.CorElementType).IsObjectReference();

	public override bool IsValueClass => ((ClrElementType)_field.CorElementType).IsValueClass();

	public override bool IsPrimitive => ((ClrElementType)_field.CorElementType).IsPrimitive();

	public override ClrElementType ElementType
	{
		get
		{
			if (_elementType != 0)
			{
				return _elementType;
			}
			ClrType value = _type.Value;
			if (value == null)
			{
				_elementType = (ClrElementType)_field.CorElementType;
			}
			else if (value.IsEnum)
			{
				_elementType = value.GetEnumElementType();
			}
			else
			{
				_elementType = value.ElementType;
			}
			return _elementType;
		}
	}

	public override string Name { get; }

	public override ClrType Type => _type.Value;

	public override int Offset => (int)_field.Offset;

	public override bool HasSimpleValue
	{
		get
		{
			if (_type != null)
			{
				return !ElementType.IsValueClass();
			}
			return false;
		}
	}

	public override int Size => GetSize(_type.Value, ElementType);

	public DesktopInstanceField(DesktopGCHeap heap, IFieldData data, string name, FieldAttributes attributes, IntPtr sig, int sigLen)
	{
		DesktopInstanceField desktopInstanceField = this;
		Name = name;
		_field = data;
		_attributes = attributes;
		Token = data.FieldToken;
		_heap = heap;
		_type = new Lazy<BaseDesktopHeapType>(() => GetType(desktopInstanceField._heap, data, sig, sigLen, (ClrElementType)desktopInstanceField._field.CorElementType));
	}

	private static BaseDesktopHeapType GetType(DesktopGCHeap heap, IFieldData data, IntPtr sig, int sigLen, ClrElementType elementType)
	{
		BaseDesktopHeapType baseDesktopHeapType = null;
		ulong typeMethodTable = data.TypeMethodTable;
		if (typeMethodTable != 0L)
		{
			baseDesktopHeapType = (BaseDesktopHeapType)heap.GetTypeByMethodTable(typeMethodTable, 0uL);
		}
		if (baseDesktopHeapType == null)
		{
			if (sig != IntPtr.Zero && sigLen > 0)
			{
				SigParser sigParser = new SigParser(sig, sigLen);
				int etype = 0;
				bool flag = sigParser.GetCallingConvInfo(out var _) && sigParser.SkipCustomModifiers() && sigParser.GetElemType(out etype);
				if (etype == 21)
				{
					flag = flag && sigParser.GetElemType(out etype);
				}
				if (flag)
				{
					switch ((ClrElementType)etype)
					{
					case ClrElementType.Array:
					{
						flag = sigParser.PeekElemType(out etype) && sigParser.SkipExactlyOne();
						int data3 = 0;
						if (flag && sigParser.GetData(out data3))
						{
							baseDesktopHeapType = heap.GetArrayType((ClrElementType)etype, data3, null);
						}
						break;
					}
					case ClrElementType.SZArray:
					{
						flag = sigParser.PeekElemType(out etype);
						ClrElementType clrElementType = (ClrElementType)etype;
						baseDesktopHeapType = ((!clrElementType.IsObjectReference()) ? heap.GetArrayType(clrElementType, -1, null) : ((BaseDesktopHeapType)heap.GetBasicType(ClrElementType.SZArray)));
						break;
					}
					case ClrElementType.Pointer:
					{
						flag = sigParser.GetElemType(out etype);
						ClrElementType clrElementType = (ClrElementType)etype;
						sigParser.GetToken(out var token2);
						BaseDesktopHeapType baseDesktopHeapType2 = (BaseDesktopHeapType)heap.GetGCHeapTypeFromModuleAndToken(data.Module, Convert.ToUInt32(token2));
						if (baseDesktopHeapType2 == null)
						{
							baseDesktopHeapType2 = (BaseDesktopHeapType)heap.GetBasicType(clrElementType);
						}
						baseDesktopHeapType = heap.CreatePointerType(baseDesktopHeapType2, clrElementType, null);
						break;
					}
					case ClrElementType.Class:
					case ClrElementType.Object:
						baseDesktopHeapType = (BaseDesktopHeapType)heap.ObjectType;
						break;
					default:
					{
						int token = 0;
						if (etype == 17 || etype == 18)
						{
							flag = flag && sigParser.GetToken(out token);
						}
						if (token != 0)
						{
							baseDesktopHeapType = (BaseDesktopHeapType)heap.GetGCHeapTypeFromModuleAndToken(data.Module, (uint)token);
						}
						if (baseDesktopHeapType == null && (baseDesktopHeapType = (BaseDesktopHeapType)heap.GetBasicType((ClrElementType)etype)) == null)
						{
							baseDesktopHeapType = heap.ErrorType;
						}
						break;
					}
					}
				}
			}
			if (baseDesktopHeapType == null)
			{
				baseDesktopHeapType = (BaseDesktopHeapType)heap.GetBasicType(elementType);
			}
		}
		else if (elementType != ClrElementType.Class)
		{
			baseDesktopHeapType.ElementType = elementType;
		}
		if (baseDesktopHeapType.IsArray && baseDesktopHeapType.ComponentType == null && sig != IntPtr.Zero && sigLen > 0)
		{
			SigParser sigParser2 = new SigParser(sig, sigLen);
			int etype2 = 0;
			bool flag2 = sigParser2.GetCallingConvInfo(out var _) && sigParser2.SkipCustomModifiers() && sigParser2.GetElemType(out etype2) && sigParser2.GetElemType(out etype2);
			if (etype2 == 21)
			{
				flag2 = flag2 && sigParser2.GetElemType(out etype2);
			}
			int token3 = 0;
			if (etype2 == 17 || etype2 == 18)
			{
				flag2 = flag2 && sigParser2.GetToken(out token3);
			}
			if (token3 != 0)
			{
				baseDesktopHeapType.ComponentType = heap.GetGCHeapTypeFromModuleAndToken(data.Module, (uint)token3);
			}
			else if (baseDesktopHeapType.ComponentType == null)
			{
				ClrType clrType = (baseDesktopHeapType.ComponentType = heap.GetBasicType((ClrElementType)etype2));
				if (clrType == null)
				{
					baseDesktopHeapType.ComponentType = heap.ErrorType;
				}
			}
		}
		return baseDesktopHeapType;
	}

	public override object GetValue(ulong objRef, bool interior = false, bool convertStrings = true)
	{
		if (!HasSimpleValue)
		{
			return null;
		}
		ulong num = GetAddress(objRef, interior);
		if (ElementType == ClrElementType.String)
		{
			object valueAtAddress = _heap.GetValueAtAddress(ClrElementType.Object, num);
			if (valueAtAddress == null || !(valueAtAddress is ulong))
			{
				if (!convertStrings)
				{
					return 0uL;
				}
				return null;
			}
			num = (ulong)valueAtAddress;
			if (!convertStrings)
			{
				return num;
			}
		}
		return _heap.GetValueAtAddress(ElementType, num);
	}

	public override ulong GetAddress(ulong objRef, bool interior = false)
	{
		if (interior)
		{
			return objRef + (ulong)Offset;
		}
		if (_type == null)
		{
			return objRef + (ulong)(Offset + IntPtr.Size);
		}
		return objRef + (ulong)(Offset + _heap.PointerSize);
	}

	internal static int GetSize(BaseDesktopHeapType type, ClrElementType cet)
	{
		switch (cet)
		{
		case ClrElementType.Struct:
		{
			if (type == null)
			{
				return 1;
			}
			ClrField clrField = null;
			foreach (ClrInstanceField field in type.Fields)
			{
				if (clrField == null)
				{
					clrField = field;
				}
				else if (field.Offset > clrField.Offset)
				{
					clrField = field;
				}
				else if (field.Offset == clrField.Offset && field.Size > clrField.Size)
				{
					clrField = field;
				}
			}
			if (clrField == null)
			{
				return 0;
			}
			return clrField.Offset + clrField.Size;
		}
		case ClrElementType.Boolean:
		case ClrElementType.Int8:
		case ClrElementType.UInt8:
			return 1;
		case ClrElementType.Int32:
		case ClrElementType.UInt32:
		case ClrElementType.Float:
			return 4;
		case ClrElementType.Int64:
		case ClrElementType.UInt64:
		case ClrElementType.Double:
			return 8;
		case ClrElementType.String:
		case ClrElementType.Pointer:
		case ClrElementType.Class:
		case ClrElementType.Array:
		case ClrElementType.NativeInt:
		case ClrElementType.NativeUInt:
		case ClrElementType.FunctionPointer:
		case ClrElementType.Object:
		case ClrElementType.SZArray:
			return type?.DesktopHeap.PointerSize ?? IntPtr.Size;
		case ClrElementType.Char:
		case ClrElementType.Int16:
		case ClrElementType.UInt16:
			return 2;
		default:
			throw new Exception("Unexpected element type.");
		}
	}
}
