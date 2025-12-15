using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct TypeHandle : IEquatable<TypeHandle>
{
	private class HeapTypeEqualityComparer : IEqualityComparer<TypeHandle>
	{
		public bool Equals(TypeHandle x, TypeHandle y)
		{
			if (x.MethodTable == y.MethodTable)
			{
				return x.ComponentMethodTable == y.ComponentMethodTable;
			}
			return false;
		}

		public int GetHashCode(TypeHandle obj)
		{
			return (int)obj.MethodTable + (int)obj.ComponentMethodTable >> 3;
		}
	}

	public readonly ulong MethodTable;

	public readonly ulong ComponentMethodTable;

	public static IEqualityComparer<TypeHandle> EqualityComparer = new HeapTypeEqualityComparer();

	public TypeHandle(ulong mt)
	{
		MethodTable = mt;
		ComponentMethodTable = 0uL;
	}

	public TypeHandle(ulong mt, ulong cmt)
	{
		MethodTable = mt;
		ComponentMethodTable = cmt;
	}

	public override int GetHashCode()
	{
		return (int)MethodTable + (int)ComponentMethodTable >> 3;
	}

	bool IEquatable<TypeHandle>.Equals(TypeHandle other)
	{
		if (MethodTable == other.MethodTable)
		{
			return ComponentMethodTable == other.ComponentMethodTable;
		}
		return false;
	}
}
