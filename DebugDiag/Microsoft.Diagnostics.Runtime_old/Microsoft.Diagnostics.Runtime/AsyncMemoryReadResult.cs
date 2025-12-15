using System.Threading;

namespace Microsoft.Diagnostics.Runtime;

public class AsyncMemoryReadResult
{
	protected volatile int _read;

	protected volatile byte[] _result;

	public virtual EventWaitHandle Complete { get; set; }

	public virtual ulong Address { get; set; }

	public virtual int BytesRequested { get; set; }

	public virtual int BytesRead
	{
		get
		{
			return _read;
		}
		set
		{
			_read = value;
		}
	}

	public virtual byte[] Result
	{
		get
		{
			return _result;
		}
		set
		{
			_result = value;
		}
	}

	public AsyncMemoryReadResult()
	{
	}

	public AsyncMemoryReadResult(ulong addr, int requested)
	{
		Address = addr;
		BytesRequested = requested;
		Complete = new ManualResetEvent(initialState: false);
	}

	public override string ToString()
	{
		return $"[{Address:x}, {Address + (uint)BytesRequested:x}]";
	}
}
