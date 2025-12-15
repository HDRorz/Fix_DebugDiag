using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class V46GCHeap : DesktopGCHeap
{
	private ClrObject _lastObject;

	private readonly Dictionary<ulong, int> _indices = new Dictionary<ulong, int>();

	public V46GCHeap(DesktopRuntimeBase runtime)
		: base(runtime)
	{
	}

	public override ClrType GetObjectType(ulong objRef)
	{
		if (_lastObject.Address == objRef)
		{
			return _lastObject.Type;
		}
		if (IsHeapCached)
		{
			return base.GetObjectType(objRef);
		}
		MemoryReader memoryReader = base.MemoryReader;
		ulong value;
		if (memoryReader.Contains(objRef))
		{
			if (!memoryReader.ReadPtr(objRef, out value))
			{
				return null;
			}
		}
		else if (base.DesktopRuntime.MemoryReader.Contains(objRef))
		{
			memoryReader = base.DesktopRuntime.MemoryReader;
			if (!memoryReader.ReadPtr(objRef, out value))
			{
				return null;
			}
		}
		else
		{
			memoryReader = null;
			value = base.DesktopRuntime.DataReader.ReadPointerUnsafe(objRef);
		}
		if (((byte)value & 3) != 0)
		{
			value &= 0xFFFFFFFFFFFFFFFCuL;
		}
		ClrType typeByMethodTable = GetTypeByMethodTable(value, 0uL, objRef);
		_lastObject = ClrObject.Create(objRef, typeByMethodTable);
		return typeByMethodTable;
	}

	public override ClrType GetTypeByMethodTable(ulong mt, ulong cmt)
	{
		return GetTypeByMethodTable(mt, 0uL, 0uL);
	}

	internal override ClrType GetTypeByMethodTable(ulong mt, ulong _, ulong obj)
	{
		if (mt == 0L)
		{
			return null;
		}
		ClrType clrType = null;
		if (_indices.TryGetValue(mt, out var value))
		{
			clrType = _types[value];
		}
		else
		{
			ulong moduleForMT = base.DesktopRuntime.GetModuleForMT(mt);
			DesktopModule module = base.DesktopRuntime.GetModule(moduleForMT);
			uint token = base.DesktopRuntime.GetMetadataToken(mt);
			bool flag = mt == base.DesktopRuntime.FreeMethodTable;
			if (token == uint.MaxValue && !flag)
			{
				return null;
			}
			uint token2 = token;
			if (!flag && (module == null || module.IsDynamic))
			{
				token2 = (uint)mt;
			}
			ModuleEntry key = new ModuleEntry(module, token2);
			if (clrType == null)
			{
				IMethodTableData methodTableData = base.DesktopRuntime.GetMethodTableData(mt);
				if (methodTableData == null)
				{
					return null;
				}
				IMethodTableCollectibleData methodTableCollectibleData = base.DesktopRuntime.GetMethodTableCollectibleData(mt);
				clrType = new DesktopHeapType(() => GetTypeName(mt, module, token), module, token, mt, methodTableData, this, methodTableCollectibleData);
				value = _types.Count;
				((DesktopHeapType)clrType).SetIndex(value);
				_indices[mt] = value;
				if (!clrType.IsArray)
				{
					_typeEntry[key] = value;
				}
				_types.Add(clrType);
			}
		}
		if (obj != 0L && clrType.ComponentType == null && clrType.IsArray)
		{
			clrType.ComponentType = TryGetComponentType(obj, 0uL);
		}
		return clrType;
	}
}
