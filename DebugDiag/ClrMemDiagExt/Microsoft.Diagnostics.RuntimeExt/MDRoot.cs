using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDRoot : IMDRoot
{
	private ClrRoot m_root;

	public MDRoot(ClrRoot root)
	{
		m_root = root;
	}

	public void GetRootInfo(out ulong pAddress, out ulong pObjRef, out MDRootType pType)
	{
		pAddress = m_root.Address;
		pObjRef = m_root.Object;
		pType = (MDRootType)m_root.Kind;
	}

	public void GetType(out IMDType ppType)
	{
		ppType = null;
	}

	public void GetName(out string ppName)
	{
		ppName = m_root.Name;
	}

	public void GetAppDomain(out IMDAppDomain ppDomain)
	{
		if (m_root.AppDomain != null)
		{
			ppDomain = new MDAppDomain(m_root.AppDomain);
		}
		else
		{
			ppDomain = null;
		}
	}
}
