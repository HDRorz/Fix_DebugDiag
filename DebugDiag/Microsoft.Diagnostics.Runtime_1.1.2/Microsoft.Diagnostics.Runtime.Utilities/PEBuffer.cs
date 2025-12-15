using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class PEBuffer : IDisposable
{
	private int _buffPos;

	private byte[] _buff;

	private GCHandle _pinningHandle;

	private readonly Stream _stream;

	public unsafe byte* Buffer { get; private set; }

	public int Length { get; private set; }

	public PEBuffer(Stream stream, int buffSize = 512)
	{
		_stream = stream;
		GetBuffer(buffSize);
	}

	~PEBuffer()
	{
		if (_pinningHandle.IsAllocated)
		{
			_pinningHandle.Free();
		}
	}

	public unsafe byte* Fetch(int filePos, int size)
	{
		if (size > _buff.Length)
		{
			GetBuffer(size);
		}
		if (_buffPos > filePos || filePos + size > _buffPos + Length)
		{
			_buffPos = filePos;
			_stream.Seek(_buffPos, SeekOrigin.Begin);
			Length = 0;
			while (Length < _buff.Length)
			{
				int num = _stream.Read(_buff, Length, size - Length);
				if (num == 0)
				{
					break;
				}
				Length += num;
			}
		}
		return Buffer + (filePos - _buffPos);
	}

	public void Dispose()
	{
		if (_pinningHandle.IsAllocated)
		{
			_pinningHandle.Free();
		}
		GC.SuppressFinalize(this);
	}

	private unsafe void GetBuffer(int buffSize)
	{
		if (_pinningHandle.IsAllocated)
		{
			_pinningHandle.Free();
		}
		_buff = new byte[buffSize];
		_pinningHandle = GCHandle.Alloc(_buff, GCHandleType.Pinned);
		fixed (byte* buff = _buff)
		{
			Buffer = buff;
		}
		Length = 0;
	}
}
