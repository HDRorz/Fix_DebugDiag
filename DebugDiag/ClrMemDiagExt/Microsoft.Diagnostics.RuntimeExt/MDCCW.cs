using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDCCW : IMDCCW
{
	private CcwData m_ccw;

	public MDCCW(CcwData ccw)
	{
		m_ccw = ccw;
	}

	public void GetIUnknown(out ulong pIUnk)
	{
		pIUnk = m_ccw.IUnknown;
	}

	public void GetObject(out ulong pObject)
	{
		pObject = m_ccw.Object;
	}

	public void GetHandle(out ulong pHandle)
	{
		pHandle = m_ccw.Handle;
	}

	public void GetRefCount(out int pRefCnt)
	{
		pRefCnt = m_ccw.RefCount;
	}

	public void EnumerateInterfaces(out IMDCOMInterfaceEnum ppEnum)
	{
		ppEnum = new MDCOMInterfaceEnum(m_ccw.Interfaces);
	}
}
