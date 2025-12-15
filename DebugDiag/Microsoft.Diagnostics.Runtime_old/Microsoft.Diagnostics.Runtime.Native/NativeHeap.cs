using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeHeap : HeapBase
{
	private ulong _lastObj;

	private ClrType _lastType;

	private Dictionary<ulong, int> _indices = new Dictionary<ulong, int>();

	private List<NativeType> _types = new List<NativeType>(1024);

	private NativeModule[] _modules;

	private NativeModule _mrtModule;

	private NativeType _free;

	internal NativeRuntime NativeRuntime { get; set; }

	internal TextWriter Log { get; set; }

	public override int TypeIndexLimit => _types.Count;

	internal NativeHeap(NativeRuntime runtime, NativeModule[] modules, TextWriter log)
		: base(runtime)
	{
		Log = log;
		NativeRuntime = runtime;
		_modules = modules;
		_mrtModule = FindMrtModule();
		CreateFreeType();
		InitSegments(runtime);
	}

	public override ClrRuntime GetRuntime()
	{
		return NativeRuntime;
	}

	public override ClrType GetTypeByIndex(int index)
	{
		return _types[index];
	}

	private NativeModule FindMrtModule()
	{
		NativeModule[] modules = _modules;
		foreach (NativeModule nativeModule in modules)
		{
			if (string.Compare(nativeModule.Name, "mrt100", StringComparison.CurrentCultureIgnoreCase) == 0 || string.Compare(nativeModule.Name, "mrt100_app", StringComparison.CurrentCultureIgnoreCase) == 0)
			{
				return nativeModule;
			}
		}
		return null;
	}

	private void CreateFreeType()
	{
		ulong freeType = NativeRuntime.GetFreeType();
		IMethodTableData methodTableData = NativeRuntime.GetMethodTableData(freeType);
		_free = new NativeType(this, _types.Count, _mrtModule, "Free", freeType, methodTableData);
		_indices[freeType] = _types.Count;
		_types.Add(_free);
	}

	public override ClrType GetObjectType(ulong objRef)
	{
		if (_lastObj == objRef)
		{
			return _lastType;
		}
		MemoryReader memoryReader = base.MemoryReader;
		if (!memoryReader.Contains(objRef))
		{
			memoryReader = NativeRuntime.MemoryReader;
		}
		if (!memoryReader.ReadPtr(objRef, out var value))
		{
			return null;
		}
		if (((int)value & 3) != 0)
		{
			value &= 0xFFFFFFFFFFFFFFFCuL;
		}
		ClrType clrType = null;
		clrType = ((!_indices.TryGetValue(value, out var value2)) ? ConstructObjectType(value) : _types[value2]);
		_lastObj = objRef;
		_lastType = clrType;
		return clrType;
	}

	private ClrType ConstructObjectType(ulong eeType)
	{
		IMethodTableData methodTableData = NativeRuntime.GetMethodTableData(eeType);
		if (methodTableData == null)
		{
			return null;
		}
		ulong elementTypeHandle = methodTableData.ElementTypeHandle;
		bool flag = elementTypeHandle != 0;
		ulong num = (flag ? elementTypeHandle : methodTableData.EEClass);
		if (!flag && num != 0L)
		{
			if (!flag && _indices.TryGetValue(num, out var value))
			{
				_indices[eeType] = value;
				return _types[value];
			}
			ulong num2 = eeType;
			eeType = num;
			num = num2;
		}
		string text = NativeRuntime.ResolveSymbol(eeType);
		if (string.IsNullOrEmpty(text))
		{
			text = NativeRuntime.ResolveSymbol(num);
			if (text == null)
			{
				text = $"unknown type {eeType:x}";
			}
		}
		int num3 = text.Length;
		if (text.EndsWith("::`vftable'"))
		{
			num3 -= 11;
		}
		int num4 = text.IndexOf('!') + 1;
		text = text.Substring(num4, num3 - num4);
		if (flag)
		{
			text += "[]";
		}
		NativeModule nativeModule = FindContainingModule(eeType);
		if (nativeModule == null && num != 0L)
		{
			nativeModule = FindContainingModule(num);
		}
		if (nativeModule == null)
		{
			nativeModule = _mrtModule;
		}
		NativeType nativeType = new NativeType(this, _types.Count, nativeModule, text, eeType, methodTableData);
		_indices[eeType] = _types.Count;
		if (!flag)
		{
			_indices[num] = _types.Count;
		}
		_types.Add(nativeType);
		return nativeType;
	}

	private NativeModule FindContainingModule(ulong eeType)
	{
		int num = 0;
		int num2 = _modules.Length;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			int num4 = _modules[num3].ComparePointer(eeType);
			if (num4 < 0)
			{
				num2 = num3 - 1;
				continue;
			}
			if (num4 > 0)
			{
				num = num3 + 1;
				continue;
			}
			return _modules[num3];
		}
		return null;
	}

	public override IEnumerable<ClrRoot> EnumerateRoots()
	{
		return EnumerateRoots(enumerateStatics: true);
	}

	public override IEnumerable<ClrRoot> EnumerateRoots(bool enumerateStatics)
	{
		foreach (ClrThread thread in NativeRuntime.Threads)
		{
			foreach (ClrRoot item in NativeRuntime.EnumerateStackRoots(thread))
			{
				yield return item;
			}
		}
		foreach (ClrRoot item2 in NativeRuntime.EnumerateStaticRoots(enumerateStatics))
		{
			yield return item2;
		}
		foreach (ClrRoot item3 in NativeRuntime.EnumerateHandleRoots())
		{
			yield return item3;
		}
		ClrAppDomain domain = NativeRuntime.AppDomains[0];
		foreach (ulong item4 in NativeRuntime.EnumerateFinalizerQueue())
		{
			ClrType objectType = GetObjectType(item4);
			if (objectType != null)
			{
				yield return new NativeFinalizerRoot(item4, objectType, domain, "finalizer root");
			}
		}
	}

	public override int ReadMemory(ulong address, byte[] buffer, int offset, int count)
	{
		if (offset != 0)
		{
			throw new NotImplementedException("Non-zero offsets not supported (yet)");
		}
		int bytesRead = 0;
		if (!NativeRuntime.ReadMemory(address, buffer, count, out bytesRead))
		{
			return 0;
		}
		return bytesRead;
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		return null;
	}

	public override IEnumerable<ulong> EnumerateFinalizableObjects()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<BlockingObject> EnumerateBlockingObjects()
	{
		throw new NotImplementedException();
	}

	public override ClrException GetExceptionObject(ulong objRef)
	{
		throw new NotImplementedException();
	}

	protected override int GetRuntimeRevision()
	{
		return 0;
	}
}
