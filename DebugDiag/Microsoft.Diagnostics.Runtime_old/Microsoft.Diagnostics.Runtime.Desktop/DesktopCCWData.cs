using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopCCWData : CcwData
{
	private ulong _addr;

	private ICCWData _ccw;

	private DesktopGCHeap _heap;

	private List<ComInterfaceData> _interfaces;

	public override ulong IUnknown => _ccw.IUnknown;

	public override ulong Object => _ccw.Object;

	public override ulong Handle => _ccw.Handle;

	public override int RefCount => _ccw.RefCount + _ccw.JupiterRefCount;

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
			COMInterfacePointerData[] cCWInterfaces = _heap.DesktopRuntime.GetCCWInterfaces(_addr, _ccw.InterfaceCount);
			for (int i = 0; i < cCWInterfaces.Length; i++)
			{
				ClrType type = null;
				if (cCWInterfaces[i].MethodTable != 0L)
				{
					type = _heap.GetGCHeapType(cCWInterfaces[i].MethodTable, 0uL);
				}
				_interfaces.Add(new DesktopInterfaceData(type, cCWInterfaces[i].InterfacePtr));
			}
			return _interfaces;
		}
	}

	internal DesktopCCWData(DesktopGCHeap heap, ulong ccw, ICCWData data)
	{
		_addr = ccw;
		_ccw = data;
		_heap = heap;
	}
}
