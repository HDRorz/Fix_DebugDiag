using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDException : IMDException
{
	private ClrException m_ex;

	public MDException(ClrException ex)
	{
		m_ex = ex;
	}

	void IMDException.GetGCHeapType(out IMDType ppType)
	{
		ppType = new MDType(m_ex.Type);
	}

	void IMDException.GetMessage(out string pMessage)
	{
		pMessage = m_ex.Message;
	}

	void IMDException.GetObjectAddress(out ulong pAddress)
	{
		pAddress = m_ex.Address;
	}

	void IMDException.GetInnerException(out IMDException ppException)
	{
		if (m_ex.Inner != null)
		{
			ppException = new MDException(m_ex.Inner);
		}
		else
		{
			ppException = null;
		}
	}

	void IMDException.GetHRESULT(out int pHResult)
	{
		pHResult = m_ex.HResult;
	}

	void IMDException.EnumerateStackFrames(out IMDStackTraceEnum ppEnum)
	{
		ppEnum = new MDStackTraceEnum(m_ex.StackTrace);
	}
}
