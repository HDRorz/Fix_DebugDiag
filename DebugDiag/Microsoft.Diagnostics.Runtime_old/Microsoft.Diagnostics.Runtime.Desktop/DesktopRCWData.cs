using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopRCWData : RcwData
{
	private IRCWData _rcw;

	private DesktopGCHeap _heap;

	private uint _osThreadID;

	private List<ComInterfaceData> _interfaces;

	private ulong _addr;

	public override ulong IUnknown => _rcw.UnknownPointer;

	public override ulong VTablePointer => _rcw.VTablePtr;

	public override int RefCount => _rcw.RefCount;

	public override ulong Object => _rcw.ManagedObject;

	public override bool Disconnected => _rcw.IsDisconnected;

	public override ulong WinRTObject => _rcw.JupiterObject;

	public override uint CreatorThread
	{
		get
		{
			if (_osThreadID == uint.MaxValue)
			{
				IThreadData thread = _heap.DesktopRuntime.GetThread(_rcw.CreatorThread);
				if (thread == null || thread.OSThreadID == uint.MaxValue)
				{
					_osThreadID = 0u;
				}
				else
				{
					_osThreadID = thread.OSThreadID;
				}
			}
			return _osThreadID;
		}
	}

	public override IList<ComInterfaceData> Interfaces
	{
		get
		{
			if (_interfaces != null)
			{
				return _interfaces;
			}
			_heap.LoadAllTypes();
			_interfaces = new List<ComInterfaceData>();
			COMInterfacePointerData[] rCWInterfaces = _heap.DesktopRuntime.GetRCWInterfaces(_addr, _rcw.InterfaceCount);
			for (int i = 0; i < rCWInterfaces.Length; i++)
			{
				ClrType type = null;
				if (rCWInterfaces[i].MethodTable != 0L)
				{
					type = _heap.GetGCHeapType(rCWInterfaces[i].MethodTable, 0uL);
				}
				_interfaces.Add(new DesktopInterfaceData(type, rCWInterfaces[i].InterfacePtr));
			}
			return _interfaces;
		}
	}

	internal DesktopRCWData(DesktopGCHeap heap, ulong rcw, IRCWData data)
	{
		_addr = rcw;
		_rcw = data;
		_heap = heap;
		_osThreadID = uint.MaxValue;
	}
}
