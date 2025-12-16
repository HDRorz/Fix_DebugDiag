using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDThreadStaticField : IMDThreadStaticField
{
	private ClrThreadStaticField m_field;

	public MDThreadStaticField(ClrThreadStaticField field)
	{
		m_field = field;
	}

	public void GetName(out string pName)
	{
		pName = m_field.Name;
	}

	public void GetType(out IMDType ppType)
	{
		ppType = MDType.Construct(m_field.Type);
	}

	public void GetElementType(out int pCET)
	{
		pCET = (int)m_field.ElementType;
	}

	public void GetSize(out int pSize)
	{
		pSize = m_field.Size;
	}

	public void GetFieldValue(IMDAppDomain appDomain, IMDThread thread, out IMDValue ppValue)
	{
		object fieldValue = m_field.GetValue((ClrAppDomain)appDomain, (ClrThread)thread);
		ppValue = new MDValue(fieldValue, m_field.ElementType);
	}

	public void GetFieldAddress(IMDAppDomain appDomain, IMDThread thread, out ulong pAddress)
	{
		pAddress = m_field.GetAddress((ClrAppDomain)appDomain, (ClrThread)thread);
	}
}
