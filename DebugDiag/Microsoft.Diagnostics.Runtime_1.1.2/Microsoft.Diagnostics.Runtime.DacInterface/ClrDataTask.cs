using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal class ClrDataTask : CallableCOMWrapper
{
	private delegate int CreateStackWalkDelegate(IntPtr self, uint flags, out IntPtr stackwalk);

	private static Guid IID_IXCLRDataTask = new Guid("A5B0BEEA-EC62-4618-8012-A24FFC23934C");

	private unsafe ClrDataTaskVTable* VTable => (ClrDataTaskVTable*)base._vtable;

	public ClrDataTask(DacLibrary library, IntPtr pUnk)
		: base(library.OwningLibrary, ref IID_IXCLRDataTask, pUnk)
	{
	}

	public unsafe ClrStackWalk CreateStackWalk(DacLibrary library, uint flags)
	{
		if (((CreateStackWalkDelegate)Marshal.GetDelegateForFunctionPointer(VTable->CreateStackWalk, typeof(CreateStackWalkDelegate)))(base.Self, flags, out var stackwalk) != 0)
		{
			return null;
		}
		return new ClrStackWalk(library, stackwalk);
	}
}
