using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DebugDiag.DbgEng;
using Microsoft.Mex.Framework;

namespace DebugDiag.DotNet.WinDE;

public static class WinDEExtensionMethods
{
	public static NetDbgObj Debugger;

	private static uint? _lastEventTid = null;

	private static ulong? _ntKiBugCheckThread = null;

	private static Dictionary<int, bool> _threadIsHandlingException = new Dictionary<int, bool>();

	public unsafe static bool IsHandlingException(this ThreadInfo ti)
	{
		if (!_lastEventTid.HasValue)
		{
			uint ProcessId = 0u;
			uint ThreadId = 0u;
			DEBUG_EVENT Type;
			int lastEventInformation = ((IDebugControl)Debugger.RawDebugger).GetLastEventInformation(out Type, out ProcessId, out ThreadId, IntPtr.Zero, 0u, null, null, 0, null);
			if (lastEventInformation < 0)
			{
				throw new COMException("", lastEventInformation);
			}
			_lastEventTid = ThreadId;
		}
		if (ti.EngineThreadID == _lastEventTid)
		{
			return true;
		}
		if (!_ntKiBugCheckThread.HasValue)
		{
			((IDebugSystemObjects)Debugger.RawDebugger).GetCurrentThreadDataOffset(out var Offset);
			_ntKiBugCheckThread = Offset;
		}
		if (_ntKiBugCheckThread == ti.EThreadAddress)
		{
			return true;
		}
		return false;
	}
}
