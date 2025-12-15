using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDHandle : IMDHandle
{
	private ClrHandle m_handle;

	public MDHandle(ClrHandle handle)
	{
		m_handle = handle;
	}

	public void GetHandleData(out ulong pAddr, out ulong pObjRef, out MDHandleTypes pType)
	{
		pAddr = m_handle.Address;
		pObjRef = m_handle.Object;
		pType = (MDHandleTypes)m_handle.HandleType;
	}

	public void IsStrong(out int pStrong)
	{
		pStrong = (m_handle.Strong ? 1 : 0);
	}

	public void GetRefCount(out int pRefCount)
	{
		pRefCount = (int)m_handle.RefCount;
	}

	public void GetDependentTarget(out ulong pTarget)
	{
		pTarget = m_handle.DependentTarget;
	}

	public void GetAppDomain(out IMDAppDomain ppDomain)
	{
		if (m_handle.AppDomain != null)
		{
			ppDomain = new MDAppDomain(m_handle.AppDomain);
		}
		else
		{
			ppDomain = null;
		}
	}
}
