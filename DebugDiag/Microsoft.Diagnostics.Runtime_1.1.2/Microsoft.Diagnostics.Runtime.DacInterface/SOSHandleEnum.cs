using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class SOSHandleEnum : CallableCOMWrapper
{
	private delegate int Next(IntPtr self, int count, [Out][MarshalAs(UnmanagedType.LPArray)] HandleData[] stackRefs, out int pNeeded);

	private static Guid IID_ISOSHandleEnum = new Guid("3E269830-4A2B-4301-8EE2-D6805B29B2FA");

	private readonly Next _next;

	public unsafe SOSHandleEnum(DacLibrary library, IntPtr pUnk)
		: base(library.OwningLibrary, ref IID_ISOSHandleEnum, pUnk)
	{
		ISOSHandleEnumVTable* vtable = (ISOSHandleEnumVTable*)base._vtable;
		CallableCOMWrapper.InitDelegate(ref _next, vtable->Next);
	}

	public int ReadHandles(HandleData[] handles)
	{
		if (handles == null)
		{
			throw new ArgumentNullException("handles");
		}
		if (_next(base.Self, handles.Length, handles, out var pNeeded) < 0)
		{
			return 0;
		}
		return pNeeded;
	}
}
