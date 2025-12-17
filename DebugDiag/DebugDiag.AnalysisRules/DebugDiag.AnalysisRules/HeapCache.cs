using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.AnalysisRules;

public class HeapCache
{
	internal Dictionary<string, CacheInfo> cache;

	protected ClrRuntime runtime;

	protected ClrHeap heap;

	public ClrHeap Heap => heap;

	public bool IsValidHeap { get; internal set; }

	public HeapCache(ClrRuntime Runtime)
	{
		runtime = Runtime;
		cache = null;
		if (runtime == null)
		{
			IsValidHeap = true;
			return;
		}
		heap = runtime.GetHeap();
		if (!heap.CanWalkHeap)
		{
			IsValidHeap = true;
		}
		else
		{
			IsValidHeap = false;
		}
	}

	protected void EnsureCache()
	{
		if (cache != null)
		{
			return;
		}
		cache = new Dictionary<string, CacheInfo>();
		foreach (ulong item in heap.EnumerateObjects())
		{
			string name = heap.GetObjectType(item).Name;
			if (!cache.TryGetValue(name, out var value))
			{
				value = new CacheInfo();
				cache[name] = value;
			}
			ulong size = heap.GetObjectType(item).GetSize(item);
			value.Size += size;
			value.SizeSquared += size * size;
			if (size > value.MaxSize)
			{
				value.MaxSize = size;
			}
			if (size < value.MinSize)
			{
				value.MinSize = size;
			}
			value.Cache.Add(item);
		}
	}

	public static bool WildcardCompare(string Text, string Pattern)
	{
		if (string.IsNullOrEmpty(Pattern))
		{
			return true;
		}
		string[] array = Pattern.Split('*');
		if (array.Length != 0)
		{
			if (array[0].GetSafeLength() > 0 && !Text.StartsWith(array[0], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (array[array.Length - 1].GetSafeLength() > 0 && !Text.EndsWith(array[array.Length - 1], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			for (int i = 1; i < array.Length - 1; i++)
			{
				if (Text.IndexOf(array[0], StringComparison.OrdinalIgnoreCase) == -1)
				{
					return false;
				}
			}
			return true;
		}
		return string.Compare(Text, Pattern, ignoreCase: true) == 0;
	}

	public IEnumerable<ulong> EnumerateObjectsOfType(string TypePattern = "")
	{
		EnsureCache();
		foreach (string key in cache.Keys)
		{
			if (!WildcardCompare(key, TypePattern))
			{
				continue;
			}
			foreach (ulong item in cache[key].Cache)
			{
				yield return item;
			}
		}
	}

	public IEnumerable<dynamic> EnumerateDynamicObjectsOfType(string TypePattern = "")
	{
		EnsureCache();
		foreach (string key in cache.Keys)
		{
			if (!WildcardCompare(key, TypePattern))
			{
				continue;
			}
			foreach (ulong item in cache[key].Cache)
			{
				yield return ClrMemDiagExtensions.GetDynamicObject(heap, item);
			}
		}
	}

	public dynamic GetDinamicFromAddress(ulong Address)
	{
		return ClrMemDiagExtensions.GetDynamicObject(heap, Address);
	}

	public IEnumerable<HeapStatItem> GetExceptions()
	{
		foreach (HeapStatItem item in EnumerateTypesStats())
		{
			if (item.IsDerivedOf("System.Exception"))
			{
				yield return item;
			}
		}
	}

	public IEnumerable<string> EnumerateInterfaces(ulong Address)
	{
		ClrType objectType = heap.GetObjectType(Address);
		foreach (ClrInterface @interface in objectType.Interfaces)
		{
			yield return @interface.Name;
		}
	}

	public IEnumerable<string> InheritanceChain(ulong Address)
	{
		ClrType tp = heap.GetObjectType(Address);
		do
		{
			tp = tp.BaseType;
			if (tp != null)
			{
				yield return tp.Name;
			}
		}
		while (tp != null && tp.Name != "System.Object");
	}

	public bool IsImplementationOf(ulong Address, string TypePattern)
	{
		if (string.IsNullOrEmpty(TypePattern))
		{
			throw new ArgumentException("TypePattern cannot be empty or null");
		}
		foreach (string item in EnumerateInterfaces(Address))
		{
			if (WildcardCompare(item, TypePattern))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDerivedOf(ulong Address, string TypePattern)
	{
		if (string.IsNullOrEmpty(TypePattern))
		{
			throw new ArgumentException("TypePattern cannot be empty or null");
		}
		foreach (string item in InheritanceChain(Address))
		{
			if (WildcardCompare(item, TypePattern))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDerivedOrImplementationOf(ulong Address, string TypePattern)
	{
		if (!IsImplementationOf(Address, TypePattern))
		{
			return IsDerivedOf(Address, TypePattern);
		}
		return true;
	}

	public IEnumerable<HeapStatItem> EnumerateTypesStats()
	{
		EnsureCache();
		foreach (string key in cache.Keys)
		{
			yield return new HeapStatItem(key, this);
		}
	}

	public object GetFieldValue(ulong Address, string FieldName, ClrType TheType = null)
	{
		string[] array = FieldName.Split('.');
		ulong num = Address;
		ClrType val = TheType;
		if (val == null)
		{
			val = heap.GetObjectType(Address);
		}
		ClrInstanceField fieldByName;
		for (int i = 0; i < array.Length - 1; i++)
		{
			fieldByName = val.GetFieldByName(array[i]);
			if (!((ClrField)fieldByName).IsObjectReference())
			{
				return null;
			}
			num = (ulong)fieldByName.GetFieldValue(num);
			if (num == 0L)
			{
				return null;
			}
			val = heap.GetObjectType(num);
		}
		fieldByName = val.GetFieldByName(array[array.Length - 1]);
		object obj = null;
		obj = fieldByName.GetFieldValue(num);
		if (((ClrField)fieldByName).Type.IsEnum)
		{
			obj = ((ClrField)fieldByName).Type.GetEnumName(obj);
		}
		return obj;
	}

	public Dictionary<string, object> GetFields(ulong Address, string Fields)
	{
		string[] array = Fields.Split(' ', ',');
		ClrType objectType = heap.GetObjectType(Address);
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				dictionary[text] = GetFieldValue(Address, text, objectType);
			}
		}
		return dictionary;
	}

	public IEnumerable<ThreadObj> EnumerateStackObj(string TypePattern = "")
	{
		foreach (ClrThread thread in runtime.Threads)
		{
			foreach (ClrRoot item in thread.EnumerateStackObjects())
			{
				string name = item.Type.Name;
				if (WildcardCompare(name, TypePattern))
				{
					ThreadObj threadObj = new ThreadObj();
					threadObj.Address = item.Address;
					threadObj.IsPossibleFalsePositive = item.IsPossibleFalsePositive;
					threadObj.OSThreadId = thread.OSThreadId;
					threadObj.Address = thread.Address;
					threadObj.TypeName = name;
					threadObj.IsAlive = thread.IsAlive;
					yield return threadObj;
				}
			}
		}
	}
}
