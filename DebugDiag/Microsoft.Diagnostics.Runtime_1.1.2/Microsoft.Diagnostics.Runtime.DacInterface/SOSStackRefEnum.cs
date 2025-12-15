using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class SOSStackRefEnum : CallableCOMWrapper
{
	private delegate int Next(IntPtr self, int count, [Out][MarshalAs(UnmanagedType.LPArray)] StackRefData[] stackRefs, out int pNeeded);

	private static Guid IID_ISOSStackRefEnum = new Guid("8FA642BD-9F10-4799-9AA3-512AE78C77EE");

	private readonly Next _next;

	public unsafe SOSStackRefEnum(DacLibrary library, IntPtr pUnk)
		: base(library.OwningLibrary, ref IID_ISOSStackRefEnum, pUnk)
	{
		ISOSStackRefEnumVTable* vtable = (ISOSStackRefEnumVTable*)base._vtable;
		CallableCOMWrapper.InitDelegate(ref _next, vtable->Next);
	}

	public int ReadStackReferences(StackRefData[] stackRefs)
	{
		if (stackRefs == null)
		{
			throw new ArgumentNullException("stackRefs");
		}
		if (_next(base.Self, stackRefs.Length, stackRefs, out var pNeeded) < 0)
		{
			return 0;
		}
		return pNeeded;
	}
}
