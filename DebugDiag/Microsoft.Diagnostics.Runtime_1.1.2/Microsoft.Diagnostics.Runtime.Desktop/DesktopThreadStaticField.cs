using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThreadStaticField : ClrThreadStaticField
{
	private readonly IFieldData _field;

	private readonly BaseDesktopHeapType _type;

	public override uint Token { get; }

	public override ClrElementType ElementType => (ClrElementType)_field.CorElementType;

	public override string Name { get; }

	public override ClrType Type => _type;

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
		Name = name;
		Token = field.FieldToken;
		_type = (BaseDesktopHeapType)heap.GetTypeByMethodTable(field.TypeMethodTable, 0uL);
	}

	public override object GetValue(ClrAppDomain appDomain, ClrThread thread, bool convertStrings = true)
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
