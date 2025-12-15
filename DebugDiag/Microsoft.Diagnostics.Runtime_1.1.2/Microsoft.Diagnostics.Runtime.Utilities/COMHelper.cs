using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public abstract class COMHelper
{
	protected delegate int AddRefDelegate(IntPtr self);

	protected delegate int ReleaseDelegate(IntPtr self);

	protected delegate int QueryInterfaceDelegate(IntPtr self, ref Guid guid, out IntPtr ptr);

	protected const int S_OK = 0;

	protected const int E_INVALIDARG = -2147024809;

	protected const int E_FAIL = -2147467259;

	protected const int E_NOTIMPL = -2147467263;

	protected const int E_NOINTERFACE = -2147467262;

	protected readonly Guid IUnknownGuid = new Guid("00000000-0000-0000-C000-000000000046");

	public unsafe static int Release(IntPtr pUnk)
	{
		if (pUnk == IntPtr.Zero)
		{
			return 0;
		}
		return ((ReleaseDelegate)Marshal.GetDelegateForFunctionPointer((*(IUnknownVTable**)(void*)pUnk)->Release, typeof(ReleaseDelegate)))(pUnk);
	}
}
