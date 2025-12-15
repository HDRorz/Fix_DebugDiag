using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

public struct WDBGEXTS_CLR_DATA_INTERFACE
{
	public unsafe Guid* Iid;

	private unsafe void* _iface;

	public unsafe object Interface
	{
		get
		{
			if (_iface == null)
			{
				return null;
			}
			return Marshal.GetObjectForIUnknown((IntPtr)_iface);
		}
	}

	public unsafe WDBGEXTS_CLR_DATA_INTERFACE(Guid* iid)
	{
		Iid = iid;
		_iface = null;
	}
}
