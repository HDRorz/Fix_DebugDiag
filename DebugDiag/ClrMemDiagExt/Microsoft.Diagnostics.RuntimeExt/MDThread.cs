using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDThread : IMDThread
{
	private ClrThread m_thread;

	public MDThread(ClrThread thread)
	{
		m_thread = thread;
	}

	public void GetAddress(out ulong pAddress)
	{
		pAddress = m_thread.Address;
	}

	public void IsFinalizer(out int pIsFinalizer)
	{
		pIsFinalizer = (m_thread.IsFinalizer ? 1 : 0);
	}

	public void IsAlive(out int pIsAlive)
	{
		pIsAlive = (m_thread.IsAlive ? 1 : 0);
	}

	public void GetOSThreadId(out int pOSThreadId)
	{
		pOSThreadId = (int)m_thread.OSThreadId;
	}

	public void GetAppDomainAddress(out ulong pAppDomain)
	{
		pAppDomain = m_thread.AppDomain;
	}

	public void GetLockCount(out int pLockCount)
	{
		pLockCount = (int)m_thread.LockCount;
	}

	public void GetCurrentException(out IMDException ppException)
	{
		if (m_thread.CurrentException != null)
		{
			ppException = new MDException(m_thread.CurrentException);
		}
		else
		{
			ppException = null;
		}
	}

	public void GetTebAddress(out ulong pTeb)
	{
		pTeb = m_thread.Teb;
	}

	public void GetStackLimits(out ulong pBase, out ulong pLimit)
	{
		pBase = m_thread.StackBase;
		pLimit = m_thread.StackLimit;
	}

	public void EnumerateStackTrace(out IMDStackTraceEnum ppEnum)
	{
		ppEnum = new MDStackTraceEnum(m_thread.StackTrace);
	}
}
