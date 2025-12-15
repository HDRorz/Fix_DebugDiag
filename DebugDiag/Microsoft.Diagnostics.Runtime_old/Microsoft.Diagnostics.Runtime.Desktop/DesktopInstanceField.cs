using System;
using System.Reflection;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopInstanceField : ClrInstanceField
{
	private string _name;

	private BaseDesktopHeapType _type;

	private IFieldData _field;

	private FieldAttributes _attributes;

	private ClrElementType _elementType;

	public override bool IsPublic => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

	public override bool IsPrivate => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

	public override bool IsInternal => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

	public override bool IsProtected => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

	public override ClrElementType ElementType
	{
		get
		{
			if (_elementType != 0)
			{
				return _elementType;
			}
			if (_type == null)
			{
				_elementType = (ClrElementType)_field.CorElementType;
			}
			else if (_type.IsEnum)
			{
				_elementType = _type.GetEnumElementType();
			}
			else
			{
				_elementType = _type.ElementType;
			}
			return _elementType;
		}
	}

	public override string Name => _name;

	public override ClrType Type => _type;

	public override int Offset => (int)_field.Offset;

	public override bool HasSimpleValue
	{
		get
		{
			if (_type != null)
			{
				return !ClrRuntime.IsValueClass(ElementType);
			}
			return false;
		}
	}

	public override int Size => GetSize(_type, ElementType);

	public DesktopInstanceField(DesktopGCHeap heap, IFieldData data, string name, FieldAttributes attributes, IntPtr sig, int sigLen)
	{
		_name = name;
		_field = data;
		_attributes = attributes;
		ulong typeMethodTable = data.TypeMethodTable;
		if (typeMethodTable != 0L)
		{
			_type = (BaseDesktopHeapType)heap.GetGCHeapType(typeMethodTable, 0uL);
		}
		if (_type == null)
		{
			if (sig != IntPtr.Zero && sigLen > 0)
			{
				SigParser sigParser = new SigParser(sig, sigLen);
				int etype = 0;
				if (sigParser.GetCallingConvInfo(out var _) && sigParser.SkipCustomModifiers() && sigParser.GetElemType(out etype))
				{
					switch ((ClrElementType)etype)
					{
					case ClrElementType.Array:
					{
						bool num = sigParser.PeekElemType(out etype) && sigParser.SkipExactlyOne();
						int data3 = 0;
						if (num && sigParser.GetData(out data3))
						{
							_type = heap.GetArrayType((ClrElementType)etype, data3, null);
						}
						break;
					}
					case ClrElementType.SZArray:
					{
						sigParser.PeekElemType(out etype);
						ClrElementType elType = (ClrElementType)etype;
						if (ClrRuntime.IsObjectReference(elType))
						{
							_type = (BaseDesktopHeapType)heap.GetBasicType(ClrElementType.SZArray);
						}
						else
						{
							_type = heap.GetArrayType(elType, -1, null);
						}
						break;
					}
					case ClrElementType.Pointer:
					{
						sigParser.PeekElemType(out etype);
						ClrElementType elType = (ClrElementType)etype;
						_type = (BaseDesktopHeapType)heap.GetBasicType(elType);
						break;
					}
					}
				}
			}
			if (_type == null)
			{
				_type = (BaseDesktopHeapType)heap.GetBasicType(ElementType);
			}
		}
		else if (ElementType != ClrElementType.Class)
		{
			_type.ElementType = ElementType;
		}
	}

	public override object GetValue(ulong objRef, bool interior = false)
	{
		if (!HasSimpleValue)
		{
			return null;
		}
		ulong addr = GetAddress(objRef, interior);
		if (ElementType == ClrElementType.String)
		{
			object valueAtAddress = _type.DesktopHeap.GetValueAtAddress(ClrElementType.Object, addr);
			if (valueAtAddress == null || !(valueAtAddress is ulong))
			{
				return null;
			}
			addr = (ulong)valueAtAddress;
		}
		return _type.DesktopHeap.GetValueAtAddress(ElementType, addr);
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
		return objRef + (ulong)(Offset + _type.DesktopHeap.PointerSize);
	}

	internal static int GetSize(BaseDesktopHeapType type, ClrElementType cet)
	{
		switch (cet)
		{
		case ClrElementType.Struct:
			return type?.BaseSize ?? 1;
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
