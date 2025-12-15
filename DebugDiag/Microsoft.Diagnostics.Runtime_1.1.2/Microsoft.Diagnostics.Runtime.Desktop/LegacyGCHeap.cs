using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class LegacyGCHeap : DesktopGCHeap
{
	private ClrObject _lastObject;

	private readonly Dictionary<TypeHandle, int> _indices = new Dictionary<TypeHandle, int>(TypeHandle.EqualityComparer);

	public override bool HasComponentMethodTables => true;

	public LegacyGCHeap(DesktopRuntimeBase runtime)
		: base(runtime)
	{
	}

	public override ClrType GetTypeByMethodTable(ulong mt, ulong cmt)
	{
		return GetTypeByMethodTable(mt, cmt, 0uL);
	}

	internal override ClrType GetTypeByMethodTable(ulong mt, ulong cmt, ulong obj)
	{
		if (mt == 0L)
		{
			return null;
		}
		ClrType clrType = null;
		if (mt == base.DesktopRuntime.ArrayMethodTable)
		{
			if (cmt != 0L)
			{
				clrType = GetTypeByMethodTable(cmt, 0uL);
				if (clrType != null)
				{
					cmt = clrType.MethodTable;
				}
				else if (obj != 0L)
				{
					clrType = TryGetComponentType(obj, cmt);
					if (clrType != null)
					{
						cmt = clrType.MethodTable;
					}
				}
			}
			else
			{
				clrType = base.ObjectType;
				cmt = base.ObjectType.MethodTable;
			}
		}
		else
		{
			cmt = 0uL;
		}
		TypeHandle hnd = new TypeHandle(mt, cmt);
		ClrType clrType2 = null;
		if (_indices.TryGetValue(hnd, out var value))
		{
			clrType2 = _types[value];
		}
		else if (mt == base.DesktopRuntime.ArrayMethodTable && cmt == 0L)
		{
			uint metadataToken = base.DesktopRuntime.GetMetadataToken(mt);
			if (metadataToken == uint.MaxValue)
			{
				return null;
			}
			ModuleEntry key = new ModuleEntry(base.ArrayType.Module, metadataToken);
			clrType2 = base.ArrayType;
			value = _types.Count;
			_indices[hnd] = value;
			_typeEntry[key] = value;
			_types.Add(clrType2);
		}
		else
		{
			ulong moduleForMT = base.DesktopRuntime.GetModuleForMT(hnd.MethodTable);
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
			ModuleEntry key2 = new ModuleEntry(module, token2);
			if (clrType2 == null)
			{
				IMethodTableData methodTableData = base.DesktopRuntime.GetMethodTableData(mt);
				if (methodTableData == null)
				{
					return null;
				}
				clrType2 = new DesktopHeapType(() => GetTypeName(hnd, module, token), module, token, mt, methodTableData, this)
				{
					ComponentType = clrType
				};
				value = _types.Count;
				((DesktopHeapType)clrType2).SetIndex(value);
				_indices[hnd] = value;
				_typeEntry[key2] = value;
				_types.Add(clrType2);
			}
		}
		if (obj != 0L && clrType2.ComponentType == null && clrType2.IsArray)
		{
			clrType2.ComponentType = TryGetComponentType(obj, cmt);
		}
		return clrType2;
	}

	public override ClrType GetObjectType(ulong objRef)
	{
		ulong value = 0uL;
		if (_lastObject.Address == objRef)
		{
			return _lastObject.Type;
		}
		if (IsHeapCached)
		{
			return base.GetObjectType(objRef);
		}
		MemoryReader memoryReader = base.MemoryReader;
		ulong value2;
		if (memoryReader.Contains(objRef))
		{
			if (!memoryReader.ReadPtr(objRef, out value2))
			{
				return null;
			}
		}
		else if (base.DesktopRuntime.MemoryReader.Contains(objRef))
		{
			memoryReader = base.DesktopRuntime.MemoryReader;
			if (!memoryReader.ReadPtr(objRef, out value2))
			{
				return null;
			}
		}
		else
		{
			memoryReader = null;
			value2 = base.DesktopRuntime.DataReader.ReadPointerUnsafe(objRef);
		}
		if (((int)value2 & 3) != 0)
		{
			value2 &= 0xFFFFFFFFFFFFFFFCuL;
		}
		if (value2 == base.DesktopRuntime.ArrayMethodTable)
		{
			uint num = (uint)(PointerSize * 2);
			if (memoryReader == null)
			{
				value = base.DesktopRuntime.DataReader.ReadPointerUnsafe(objRef + num);
			}
			else if (!memoryReader.ReadPtr(objRef + num, out value))
			{
				return null;
			}
		}
		else
		{
			value = 0uL;
		}
		ClrType typeByMethodTable = GetTypeByMethodTable(value2, value, objRef);
		_lastObject = ClrObject.Create(objRef, typeByMethodTable);
		return typeByMethodTable;
	}
}
