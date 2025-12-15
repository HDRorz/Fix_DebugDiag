using System;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public struct COR_TYPEID : IEquatable<COR_TYPEID>
{
	public ulong token1;

	public ulong token2;

	public override int GetHashCode()
	{
		return (int)token1 + (int)token2;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is COR_TYPEID))
		{
			return false;
		}
		return Equals((COR_TYPEID)obj);
	}

	public bool Equals(COR_TYPEID other)
	{
		if (token1 == other.token1)
		{
			return token2 == other.token2;
		}
		return false;
	}
}
