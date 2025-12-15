using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public class ClrNullValue : DynamicObject
{
	private static ClrType s_free;

	public ClrNullValue(ClrHeap heap)
	{
		using IEnumerator<ClrType> enumerator = heap.EnumerateTypes().GetEnumerator();
		if (enumerator.MoveNext())
		{
			s_free = enumerator.Current;
		}
	}

	public bool IsNull()
	{
		return true;
	}

	public ulong GetValue()
	{
		return 0uL;
	}

	public int GetLength()
	{
		return 0;
	}

	public ClrType GetHeapType()
	{
		return s_free;
	}

	public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
	{
		result = this;
		return true;
	}

	public override bool TryConvert(ConvertBinder binder, out object result)
	{
		return GetDefaultNullValue(binder.Type, out result);
	}

	public override IEnumerable<string> GetDynamicMemberNames()
	{
		return new string[0];
	}

	public override bool TryGetMember(GetMemberBinder binder, out object result)
	{
		result = this;
		return true;
	}

	public static bool GetDefaultNullValue(Type type, out object result)
	{
		result = null;
		if (!type.IsValueType)
		{
			return true;
		}
		if (type.IsPrimitive && type.IsPublic)
		{
			try
			{
				result = Activator.CreateInstance(type);
				return true;
			}
			catch
			{
			}
		}
		return false;
	}
}
