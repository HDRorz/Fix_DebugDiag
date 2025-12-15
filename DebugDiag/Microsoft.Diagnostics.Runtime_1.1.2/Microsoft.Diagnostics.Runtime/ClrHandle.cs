using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

public class ClrHandle
{
	public ulong Address { get; set; }

	public ulong Object { get; set; }

	public ClrType Type { get; set; }

	public virtual bool IsStrong
	{
		get
		{
			switch (HandleType)
			{
			case HandleType.RefCount:
				return RefCount != 0;
			case HandleType.WeakShort:
			case HandleType.WeakLong:
			case HandleType.Dependent:
				return false;
			default:
				return true;
			}
		}
	}

	public virtual bool IsPinned
	{
		get
		{
			if (HandleType != HandleType.AsyncPinned)
			{
				return HandleType == HandleType.Pinned;
			}
			return true;
		}
	}

	public HandleType HandleType { get; set; }

	public uint RefCount { get; set; }

	public ulong DependentTarget { get; set; }

	public ClrType DependentType { get; set; }

	public ClrAppDomain AppDomain { get; set; }

	public override string ToString()
	{
		return HandleType.ToString() + " " + ((Type != null) ? Type.Name : "");
	}

	internal ClrHandle()
	{
	}

	internal ClrHandle(V45Runtime clr, ClrHeap heap, HandleData handleData)
	{
		Address = handleData.Handle;
		clr.ReadPointer(Address, out var value);
		Object = value;
		Type = heap.GetObjectType(value);
		uint num = 0u;
		if (handleData.Type == 5)
		{
			if (handleData.IsPegged != 0)
			{
				num = handleData.JupiterRefCount;
			}
			if (num < handleData.RefCount)
			{
				num = handleData.RefCount;
			}
			if (Type != null)
			{
				if (Type.IsCCW(value))
				{
					CcwData cCWData = Type.GetCCWData(value);
					if (cCWData != null && num < cCWData.RefCount)
					{
						num = (uint)cCWData.RefCount;
					}
				}
				else if (Type.IsRCW(value))
				{
					RcwData rCWData = Type.GetRCWData(value);
					if (rCWData != null && num < rCWData.RefCount)
					{
						num = (uint)rCWData.RefCount;
					}
				}
			}
			RefCount = num;
		}
		HandleType = (HandleType)handleData.Type;
		AppDomain = clr.GetAppDomainByAddress(handleData.AppDomain);
		if (HandleType == HandleType.Dependent)
		{
			DependentTarget = handleData.Secondary;
			DependentType = heap.GetObjectType(handleData.Secondary);
		}
	}

	internal ClrHandle GetInteriorHandle()
	{
		if (HandleType != HandleType.AsyncPinned)
		{
			return null;
		}
		if (Type == null)
		{
			return null;
		}
		ClrInstanceField fieldByName = Type.GetFieldByName("m_userObject");
		if (fieldByName == null)
		{
			return null;
		}
		if (fieldByName.GetValue(Object) is long num)
		{
			ulong num2 = (ulong)num;
			if (num != 0L)
			{
				ClrType objectType = Type.Heap.GetObjectType(num2);
				if (objectType == null)
				{
					return null;
				}
				return new ClrHandle
				{
					Object = num2,
					Type = objectType,
					Address = Address,
					AppDomain = AppDomain,
					HandleType = HandleType
				};
			}
		}
		return null;
	}
}
