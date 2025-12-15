using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct ArrayRankHandle : IEquatable<ArrayRankHandle>
{
	private readonly ClrElementType _type;

	private readonly int _ranks;

	public ArrayRankHandle(ClrElementType eltype, int ranks)
	{
		_type = eltype;
		_ranks = ranks;
	}

	public bool Equals(ArrayRankHandle other)
	{
		if (_type == other._type)
		{
			return _ranks == other._ranks;
		}
		return false;
	}
}
