using System;

namespace Microsoft.Diagnostics.Runtime;

public struct M128A
{
	public ulong Low;

	public ulong High;

	public void Clear()
	{
		Low = 0uL;
		High = 0uL;
	}

	public static bool operator ==(M128A lhs, M128A rhs)
	{
		if (lhs.Low == rhs.Low)
		{
			return lhs.High == rhs.High;
		}
		return false;
	}

	public static bool operator !=(M128A lhs, M128A rhs)
	{
		if (lhs.Low == rhs.Low)
		{
			return lhs.High != rhs.High;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (obj.GetType() != typeof(M128A))
		{
			return false;
		}
		return this == (M128A)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
