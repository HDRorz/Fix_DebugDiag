using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDRCW : IMDRCW
{
	private RcwData m_rcw;

	public MDRCW(RcwData rcw)
	{
		m_rcw = rcw;
	}

	public void GetIUnknown(out ulong pIUnk)
	{
		pIUnk = m_rcw.IUnknown;
	}

	public void GetObject(out ulong pObject)
	{
		pObject = m_rcw.Object;
	}

	public void GetRefCount(out int pRefCnt)
	{
		pRefCnt = m_rcw.RefCount;
	}

	public void GetVTable(out ulong pHandle)
	{
		pHandle = m_rcw.VTablePointer;
	}

	public void IsDisconnected(out int pDisconnected)
	{
		pDisconnected = (m_rcw.Disconnected ? 1 : 0);
	}

	public void EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
	{
		ppEnum = new MDCOMInterfaceEnum(m_rcw.Interfaces);
	}
}
