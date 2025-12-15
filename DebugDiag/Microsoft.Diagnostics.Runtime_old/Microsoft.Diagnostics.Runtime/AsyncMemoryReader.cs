namespace Microsoft.Diagnostics.Runtime;

internal class AsyncMemoryReader : MemoryReader
{
	protected AsyncMemoryReadResult _result;

	public AsyncMemoryReader(IDataReader dataReader, int cacheSize)
		: base(dataReader, cacheSize)
	{
	}

	public override void EnsureRangeInCache(ulong addr)
	{
		if (!Contains(addr))
		{
			if (!_dataReader.CanReadAsync)
			{
				ReadMemory(addr);
			}
			else if (!Contains(addr, _result))
			{
				ReadMemory(addr);
				ReadAsync(addr + (uint)_cacheSize);
			}
		}
	}

	protected override bool MoveToPage(ulong addr)
	{
		if (Contains(addr, _result))
		{
			_result.Complete.WaitOne();
			_data = _result.Result;
			_currPageSize = _result.BytesRead;
			_currPageStart = _result.Address;
			if (_result.BytesRequested == _result.BytesRead)
			{
				ReadAsync(_result.Address + (uint)_result.BytesRead);
			}
			else
			{
				_result = null;
			}
			return true;
		}
		bool result = ReadMemory(addr);
		if (_cacheSize == _currPageSize)
		{
			ReadAsync(addr + (uint)_cacheSize);
		}
		return result;
	}

	private void ReadAsync(ulong addr)
	{
		_result = _dataReader.ReadMemoryAsync(addr, _cacheSize * 10);
	}

	protected override bool ReadMemory(ulong addr)
	{
		return base.ReadMemory(addr);
	}

	private bool Contains(ulong addr, AsyncMemoryReadResult result)
	{
		if (result != null)
		{
			return Contains(addr, result.Address, result.BytesRequested);
		}
		return false;
	}

	private bool Contains(ulong addr, ulong start, int len)
	{
		if (start <= addr)
		{
			return addr < start + (uint)len;
		}
		return false;
	}
}
