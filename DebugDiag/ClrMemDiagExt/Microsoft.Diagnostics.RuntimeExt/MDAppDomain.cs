using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDAppDomain : IMDAppDomain
{
	private ClrAppDomain m_appDomain;

	public MDAppDomain(ClrAppDomain ad)
	{
		m_appDomain = ad;
	}

	public void GetName(out string pName)
	{
		pName = m_appDomain.Name;
	}

	public void GetID(out int pID)
	{
		pID = m_appDomain.Id;
	}

	public void GetAddress(out ulong pAddress)
	{
		pAddress = m_appDomain.Address;
	}
}
