using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class PEBuffer : IDisposable
{
	private int _buffPos;

	private int _buffLen;

	private byte[] _buff;

	private unsafe byte* _buffPtr;

	private GCHandle _pinningHandle;

	private Stream _stream;

	public unsafe byte* Buffer => _buffPtr;

	public int Length => _buffLen;

	public PEBuffer(Stream stream, int buffSize = 512)
	{
		_stream = stream;
		GetBuffer(buffSize);
	}

	public unsafe byte* Fetch(int filePos, int size)
	{
		if (size > _buff.Length)
		{
			GetBuffer(size);
		}
		if (_buffPos > filePos || filePos + size > _buffPos + _buffLen)
		{
			_buffPos = filePos;
			_stream.Seek(_buffPos, SeekOrigin.Begin);
			_buffLen = 0;
			while (_buffLen < _buff.Length)
			{
				int num = _stream.Read(_buff, _buffLen, size - _buffLen);
				if (num == 0)
				{
					break;
				}
				_buffLen += num;
			}
		}
		return _buffPtr + (filePos - _buffPos);
	}

	public void Dispose()
	{
		_pinningHandle.Free();
	}

	private unsafe void GetBuffer(int buffSize)
	{
		_buff = new byte[buffSize];
		_pinningHandle = GCHandle.Alloc(_buff, GCHandleType.Pinned);
		fixed (byte* buff = _buff)
		{
			_buffPtr = buff;
		}
		_buffLen = 0;
	}
}
