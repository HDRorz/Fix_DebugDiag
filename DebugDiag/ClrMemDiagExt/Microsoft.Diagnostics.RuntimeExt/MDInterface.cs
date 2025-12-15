using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDInterface : IMDInterface
{
	private ClrInterface m_heapint;

	public MDInterface(ClrInterface heapint)
	{
		m_heapint = heapint;
	}

	public void GetName(out string pName)
	{
		pName = m_heapint.Name;
	}

	public void GetBaseInterface(out IMDInterface ppBase)
	{
		if (m_heapint.BaseInterface != null)
		{
			ppBase = new MDInterface(m_heapint.BaseInterface);
		}
		else
		{
			ppBase = null;
		}
	}
}
