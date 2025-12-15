using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThreadStaticField : ClrThreadStaticField
{
	private IFieldData _field;

	private string _name;

	private BaseDesktopHeapType _type;

	public override ClrElementType ElementType => (ClrElementType)_field.CorElementType;

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

	public override int Size => DesktopInstanceField.GetSize(_type, ElementType);

	public override bool IsPublic
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsPrivate
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsInternal
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsProtected
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public DesktopThreadStaticField(DesktopGCHeap heap, IFieldData field, string name)
	{
		_field = field;
		_name = name;
		_type = (BaseDesktopHeapType)heap.GetGCHeapType(field.TypeMethodTable, 0uL);
	}

	public override object GetValue(ClrAppDomain appDomain, ClrThread thread)
	{
		if (!HasSimpleValue)
		{
			return null;
		}
		ulong num = GetAddress(appDomain, thread);
		if (num == 0L)
		{
			return null;
		}
		if (ElementType == ClrElementType.String)
		{
			object valueAtAddress = _type.DesktopHeap.GetValueAtAddress(ClrElementType.Object, num);
			if (valueAtAddress == null || !(valueAtAddress is ulong))
			{
				return null;
			}
			num = (ulong)valueAtAddress;
		}
		return _type.DesktopHeap.GetValueAtAddress(ElementType, num);
	}

	public override ulong GetAddress(ClrAppDomain appDomain, ClrThread thread)
	{
		if (_type == null)
		{
			return 0uL;
		}
		DesktopRuntimeBase desktopRuntime = _type.DesktopHeap.DesktopRuntime;
		return desktopRuntime.GetThreadStaticPointer(moduleId: (uint)desktopRuntime.GetModuleData(_field.Module).ModuleId, thread: thread.Address, type: (ClrElementType)_field.CorElementType, offset: (uint)Offset, shared: _type.Shared);
	}
}
