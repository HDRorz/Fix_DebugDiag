using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDField : IMDField
{
	private ClrInstanceField m_field;

	public MDField(ClrInstanceField field)
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

	public void GetOffset(out int pOffset)
	{
		pOffset = m_field.Offset;
	}

	public void GetFieldValue(ulong objRef, int interior, out IMDValue ppValue)
	{
		object fieldValue = m_field.GetFieldValue(objRef, interior != 0);
		ppValue = new MDValue(fieldValue, m_field.ElementType);
	}

	public void GetFieldAddress(ulong objRef, int interior, out ulong pAddress)
	{
		pAddress = m_field.GetFieldAddress(objRef, interior != 0);
	}
}
