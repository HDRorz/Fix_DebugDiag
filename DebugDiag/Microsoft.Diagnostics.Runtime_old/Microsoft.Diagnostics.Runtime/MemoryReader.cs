using System;

namespace Microsoft.Diagnostics.Runtime;

internal class MemoryReader
{
	protected ulong _currPageStart;

	protected int _currPageSize;

	protected byte[] _data;

	private byte[] _ptr;

	private byte[] _dword;

	protected IDataReader _dataReader;

	protected int _cacheSize;

	public MemoryReader(IDataReader dataReader, int cacheSize)
	{
		_data = new byte[cacheSize];
		_dataReader = dataReader;
		uint pointerSize = _dataReader.GetPointerSize();
		if (pointerSize != 4 && pointerSize != 8)
		{
			throw new InvalidOperationException("DataReader reported an invalid pointer size.");
		}
		_ptr = new byte[pointerSize];
		_dword = new byte[4];
		_cacheSize = cacheSize;
	}

	public bool ReadDword(ulong addr, out uint value)
	{
		uint num = 4u;
		if ((addr < _currPageStart || addr >= _currPageStart + (uint)_currPageSize) && !MoveToPage(addr))
		{
			return MisalignedRead(addr, out value);
		}
		ulong num2 = addr - _currPageStart;
		if (num2 + num > (uint)_currPageSize)
		{
			return MisalignedRead(addr, out value);
		}
		value = BitConverter.ToUInt32(_data, (int)num2);
		return true;
	}

	public bool ReadDword(ulong addr, out int value)
	{
		uint value2 = 0u;
		bool result = ReadDword(addr, out value2);
		value = (int)value2;
		return result;
	}

	internal unsafe bool TryReadPtr(ulong addr, out ulong value)
	{
		if (_currPageStart <= addr && addr - _currPageStart < (uint)_currPageSize)
		{
			ulong num = addr - _currPageStart;
			fixed (byte* ptr = &_data[num])
			{
				if (_ptr.Length == 4)
				{
					value = *(uint*)ptr;
				}
				else
				{
					value = *(ulong*)ptr;
				}
			}
			return true;
		}
		return MisalignedRead(addr, out value);
	}

	internal unsafe bool TryReadDword(ulong addr, out uint value)
	{
		if (_currPageStart <= addr && addr - _currPageStart < (uint)_currPageSize)
		{
			ulong num = addr - _currPageStart;
			value = BitConverter.ToUInt32(_data, (int)num);
			fixed (byte* ptr = &_data[num])
			{
				value = *(uint*)ptr;
			}
			return true;
		}
		return MisalignedRead(addr, out value);
	}

	internal unsafe bool TryReadDword(ulong addr, out int value)
	{
		if (_currPageStart <= addr && addr - _currPageStart < (uint)_currPageSize)
		{
			ulong num = addr - _currPageStart;
			fixed (byte* ptr = &_data[num])
			{
				value = *(int*)ptr;
			}
			return true;
		}
		return MisalignedRead(addr, out value);
	}

	public unsafe bool ReadPtr(ulong addr, out ulong value)
	{
		if ((addr < _currPageStart || addr - _currPageStart > (uint)_currPageSize) && !MoveToPage(addr))
		{
			return MisalignedRead(addr, out value);
		}
		ulong num = addr - _currPageStart;
		if (num + (uint)_ptr.Length > (uint)_currPageSize)
		{
			if (!MoveToPage(addr))
			{
				return MisalignedRead(addr, out value);
			}
			num = 0uL;
		}
		fixed (byte* ptr = &_data[num])
		{
			if (_ptr.Length == 4)
			{
				value = *(uint*)ptr;
			}
			else
			{
				value = *(ulong*)ptr;
			}
		}
		return true;
	}

	public virtual void EnsureRangeInCache(ulong addr)
	{
		if (!Contains(addr))
		{
			MoveToPage(addr);
		}
	}

	public bool Contains(ulong addr)
	{
		if (_currPageStart <= addr)
		{
			return addr - _currPageStart < (uint)_currPageSize;
		}
		return false;
	}

	private unsafe bool MisalignedRead(ulong addr, out ulong value)
	{
		int bytesRead = 0;
		bool result = _dataReader.ReadMemory(addr, _ptr, _ptr.Length, out bytesRead);
		fixed (byte* ptr = _ptr)
		{
			if (_ptr.Length == 4)
			{
				value = *(uint*)ptr;
			}
			else
			{
				value = *(ulong*)ptr;
			}
		}
		return result;
	}

	private bool MisalignedRead(ulong addr, out uint value)
	{
		int bytesRead = 0;
		bool result = _dataReader.ReadMemory(addr, _dword, _dword.Length, out bytesRead);
		value = BitConverter.ToUInt32(_dword, 0);
		return result;
	}

	private bool MisalignedRead(ulong addr, out int value)
	{
		int bytesRead = 0;
		bool result = _dataReader.ReadMemory(addr, _dword, _dword.Length, out bytesRead);
		value = BitConverter.ToInt32(_dword, 0);
		return result;
	}

	protected virtual bool MoveToPage(ulong addr)
	{
		return ReadMemory(addr);
	}

	protected virtual bool ReadMemory(ulong addr)
	{
		_currPageStart = addr;
		bool num = _dataReader.ReadMemory(_currPageStart, _data, _cacheSize, out _currPageSize);
		if (!num)
		{
			_currPageStart = 0uL;
			_currPageSize = 0;
		}
		return num;
	}
}
