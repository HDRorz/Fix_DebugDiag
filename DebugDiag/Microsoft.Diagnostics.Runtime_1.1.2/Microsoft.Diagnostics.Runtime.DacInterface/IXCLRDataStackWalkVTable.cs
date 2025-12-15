using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct IXCLRDataStackWalkVTable
{
	public readonly IntPtr GetContext;

	private readonly IntPtr GetContext2;

	public readonly IntPtr Next;

	private readonly IntPtr GetStackSizeSkipped;

	private readonly IntPtr GetFrameType;

	public readonly IntPtr GetFrame;

	public readonly IntPtr Request;

	private readonly IntPtr SetContext2;
}
