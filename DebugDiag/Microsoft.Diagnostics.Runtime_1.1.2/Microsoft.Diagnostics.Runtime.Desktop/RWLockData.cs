using System;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct RWLockData : IRWLockData
{
	public readonly IntPtr Next;

	public readonly IntPtr Prev;

	public readonly int ULockID;

	public readonly int LLockID;

	public readonly short ReaderLevel;

	ulong IRWLockData.Next => (ulong)Next.ToInt64();

	int IRWLockData.ULockID => ULockID;

	int IRWLockData.LLockID => LLockID;

	int IRWLockData.Level => ReaderLevel;
}
