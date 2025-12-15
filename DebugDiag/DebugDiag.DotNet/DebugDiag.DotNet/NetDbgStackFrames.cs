using System.Collections.Generic;
using DebugDiag.DbgLib;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet;

/// <summary>
/// This object maintains a collection of <c>NetDbgStackFrame</c> objects.  An instance of this object is obtained from the <c>NetDbgThread.MixedStackFrames</c> property.
/// </summary>
public class NetDbgStackFrames : List<NetDbgStackFrame>
{
	private Dictionary<ulong, NetDbgStackFrame> _netStackFrames = new Dictionary<ulong, NetDbgStackFrame>();

	private NetDbgObj _debugger;

	/// <summary>
	/// Constructor of the collection
	/// </summary>
	/// <param name="legacyThread"></param>
	/// <param name="debugger">Reference of the NetDbgObj</param>
	/// <param name="includeManagedFrames">Should the collection include the Managed Frames, the default is true</param>
	/// <param name="includeNativeFrames">Should the collection include the Native Frames, the default is true</param>
	public NetDbgStackFrames(IDbgThread legacyThread, NetDbgObj debugger, bool includeNativeFrames = true, bool includeManagedFrames = true)
	{
		_debugger = debugger;
		bool flag = false;
		IStackFrames stackFrames = legacyThread.StackFrames;
		int num = (int)legacyThread.SystemID;
		if (includeManagedFrames && debugger.ClrRuntime != null)
		{
			foreach (ClrRuntime clrRuntime in debugger.ClrRuntimes)
			{
				foreach (ClrThread thread in clrRuntime.Threads)
				{
					if (thread.OSThreadId != num)
					{
						continue;
					}
					flag = true;
					foreach (ClrStackFrame item in thread.StackTrace)
					{
						if (!string.IsNullOrEmpty(item.DisplayString))
						{
							AddInternal(new NetDbgStackFrame(item, debugger));
						}
					}
					flag = true;
					break;
				}
			}
		}
		if (includeNativeFrames)
		{
			foreach (IDbgStackFrame item2 in stackFrames)
			{
				AddInternal(item2);
			}
		}
		if (!(includeManagedFrames && includeNativeFrames) || !flag)
		{
			return;
		}
		Sort(CompareBySP);
		int num2 = 0;
		using Enumerator enumerator5 = GetEnumerator();
		while (enumerator5.MoveNext())
		{
			enumerator5.Current.FrameNumber = num2++;
		}
	}

	private void AddInternal(NetDbgStackFrame netStackFrame)
	{
		ulong key = (ulong)netStackFrame.StackAddress;
		if (!_netStackFrames.ContainsKey(key))
		{
			Add(netStackFrame);
			_netStackFrames.Add(key, netStackFrame);
		}
	}

	private void AddInternal(IDbgStackFrame stackFrame)
	{
		ulong key = (ulong)stackFrame.StackAddress;
		if (_netStackFrames.ContainsKey(key))
		{
			_netStackFrames[key].UpdateWithNativeFrameInfo(stackFrame);
			return;
		}
		NetDbgStackFrame netStackFrame = new NetDbgStackFrame(stackFrame, _debugger);
		AddInternal(netStackFrame);
	}

	private static int CompareBySP(NetDbgStackFrame x, NetDbgStackFrame y)
	{
		if (x.StackAddress < y.StackAddress)
		{
			return -1;
		}
		if (x.StackAddress > y.StackAddress)
		{
			return 1;
		}
		return 0;
	}
}
