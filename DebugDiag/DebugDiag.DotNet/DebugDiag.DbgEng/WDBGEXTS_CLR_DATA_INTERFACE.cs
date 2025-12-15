using System;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

public struct WDBGEXTS_CLR_DATA_INTERFACE
{
	public unsafe Guid* Iid;

	private unsafe void* Iface;

	public unsafe object Interface
	{
		get
		{
			if (Iface == null)
			{
				return null;
			}
			return Marshal.GetObjectForIUnknown((IntPtr)Iface);
		}
	}

	public unsafe WDBGEXTS_CLR_DATA_INTERFACE(Guid* iid)
	{
		Iid = iid;
		Iface = null;
	}
}
