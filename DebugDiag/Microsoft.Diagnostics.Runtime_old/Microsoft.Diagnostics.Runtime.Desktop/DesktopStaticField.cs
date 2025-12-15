using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopStaticField : ClrStaticField
{
	private IFieldData _field;

	private string _name;

	private BaseDesktopHeapType _type;

	private BaseDesktopHeapType _containingType;

	private FieldAttributes _attributes;

	private object _defaultValue;

	private DesktopGCHeap _heap;

	public override bool HasDefaultValue => _defaultValue != null;

	public override bool IsPublic => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;

	public override bool IsPrivate => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;

	public override bool IsInternal => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;

	public override bool IsProtected => (_attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;

	public override ClrElementType ElementType => (ClrElementType)_field.CorElementType;

	public override string Name => _name;

	public override ClrType Type
	{
		get
		{
			if (_type == null)
			{
				_type = (BaseDesktopHeapType)TryBuildType(_heap);
			}
			return _type;
		}
	}

	public override int Offset => (int)_field.Offset;

	public override bool HasSimpleValue => _containingType != null;

	public override int Size
	{
		get
		{
			if (_type == null)
			{
				_type = (BaseDesktopHeapType)TryBuildType(_heap);
			}
			return DesktopInstanceField.GetSize(_type, ElementType);
		}
	}

	public DesktopStaticField(DesktopGCHeap heap, IFieldData field, BaseDesktopHeapType containingType, string name, FieldAttributes attributes, object defaultValue, IntPtr sig, int sigLen)
	{
		_field = field;
		_name = name;
		_attributes = attributes;
		_type = (BaseDesktopHeapType)heap.GetGCHeapType(field.TypeMethodTable, 0uL);
		_defaultValue = defaultValue;
		_heap = heap;
		if (_type != null && ElementType != ClrElementType.Class)
		{
			_type.ElementType = ElementType;
		}
		_containingType = containingType;
		if (_type != null)
		{
			return;
		}
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
					int data2 = 0;
					if (num && sigParser.GetData(out data2))
					{
						_type = heap.GetArrayType((ClrElementType)etype, data2, null);
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
			_type = (BaseDesktopHeapType)TryBuildType(_heap);
		}
		if (_type == null)
		{
			_type = (BaseDesktopHeapType)heap.GetBasicType(ElementType);
		}
	}

	public override object GetDefaultValue()
	{
		return _defaultValue;
	}

	private ClrType TryBuildType(ClrHeap heap)
	{
		IList<ClrAppDomain> appDomains = heap.GetRuntime().AppDomains;
		ClrType[] array = new ClrType[appDomains.Count];
		ClrElementType elementType = ElementType;
		if (ClrRuntime.IsPrimitive(elementType) || elementType == ClrElementType.String)
		{
			return ((DesktopGCHeap)heap).GetBasicType(elementType);
		}
		int num = 0;
		foreach (ClrAppDomain item in appDomains)
		{
			object value = GetValue(item);
			if (value != null && value is ulong && (ulong)value != 0L)
			{
				array[num++] = heap.GetObjectType((ulong)value);
			}
		}
		int num2 = int.MaxValue;
		ClrType clrType = null;
		for (int i = 0; i < num; i++)
		{
			ClrType clrType2 = array[i];
			if (clrType2 != clrType && clrType2 != null)
			{
				int depth = GetDepth(clrType2);
				if (depth < num2)
				{
					clrType = clrType2;
					num2 = depth;
				}
			}
		}
		return clrType;
	}

	private int GetDepth(ClrType curr)
	{
		int num = 0;
		while (curr != null)
		{
			curr = curr.BaseType;
			num++;
		}
		return num;
	}

	public override object GetValue(ClrAppDomain appDomain)
	{
		if (!HasSimpleValue)
		{
			return null;
		}
		ulong num = GetAddress(appDomain);
		if (ElementType == ClrElementType.String)
		{
			object valueAtAddress = _containingType.DesktopHeap.GetValueAtAddress(ClrElementType.Object, num);
			if (valueAtAddress == null || !(valueAtAddress is ulong))
			{
				return null;
			}
			num = (ulong)valueAtAddress;
		}
		ClrElementType clrElementType = ElementType;
		if (clrElementType == ClrElementType.Struct)
		{
			clrElementType = ClrElementType.Object;
		}
		if (clrElementType == ClrElementType.Object && num == 0L)
		{
			return 0uL;
		}
		return _containingType.DesktopHeap.GetValueAtAddress(clrElementType, num);
	}

	public override ulong GetAddress(ClrAppDomain appDomain)
	{
		if (_containingType == null)
		{
			return 0uL;
		}
		bool shared = _containingType.Shared;
		IDomainLocalModuleData domainLocalModuleData = null;
		if (shared)
		{
			ulong moduleId = _containingType.DesktopModule.ModuleId;
			domainLocalModuleData = _containingType.DesktopHeap.DesktopRuntime.GetDomainLocalModule(appDomain.Address, moduleId);
			if (!IsInitialized(domainLocalModuleData))
			{
				return 0uL;
			}
		}
		else
		{
			ulong moduleAddress = _containingType.GetModuleAddress(appDomain);
			if (moduleAddress != 0L)
			{
				domainLocalModuleData = _containingType.DesktopHeap.DesktopRuntime.GetDomainLocalModule(moduleAddress);
			}
		}
		if (domainLocalModuleData == null)
		{
			return 0uL;
		}
		if (ClrRuntime.IsPrimitive(ElementType))
		{
			return domainLocalModuleData.NonGCStaticDataStart + _field.Offset;
		}
		return domainLocalModuleData.GCStaticDataStart + _field.Offset;
	}

	public override bool IsInitialized(ClrAppDomain appDomain)
	{
		if (_containingType == null)
		{
			return false;
		}
		if (!_containingType.Shared)
		{
			return true;
		}
		ulong moduleId = _containingType.DesktopModule.ModuleId;
		IDomainLocalModuleData domainLocalModule = _containingType.DesktopHeap.DesktopRuntime.GetDomainLocalModule(appDomain.Address, moduleId);
		if (domainLocalModule == null)
		{
			return false;
		}
		return IsInitialized(domainLocalModule);
	}

	private bool IsInitialized(IDomainLocalModuleData data)
	{
		if (data == null || _containingType == null)
		{
			return false;
		}
		byte value = 0;
		ulong addr = data.ClassData + (uint)((int)_containingType.MetadataToken & -33554433) - 1;
		if (!_heap.DesktopRuntime.ReadByte(addr, out value))
		{
			return false;
		}
		return (value & 1) != 0;
	}
}
