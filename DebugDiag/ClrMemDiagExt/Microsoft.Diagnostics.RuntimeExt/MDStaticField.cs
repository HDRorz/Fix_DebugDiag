using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDStaticField : IMDStaticField
{
	private ClrStaticField m_field;

	public MDStaticField(ClrStaticField field)
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

	public void GetFieldValue(IMDAppDomain appDomain, out IMDValue ppValue)
	{
		object fieldValue = m_field.GetFieldValue((ClrAppDomain)appDomain);
		ppValue = new MDValue(fieldValue, m_field.ElementType);
	}

	public void GetFieldAddress(IMDAppDomain appDomain, out ulong pAddress)
	{
		ulong fieldAddress = m_field.GetFieldAddress((ClrAppDomain)appDomain);
		pAddress = fieldAddress;
	}
}
