using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ReaderStream : Stream
{
	private readonly Reader _reader;

	private readonly long _baseAddress;

	private long _position;

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length => 2048L;

	public override long Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
		}
	}

	public ReaderStream(long baseAddress, Reader reader)
	{
		_reader = reader;
		_baseAddress = baseAddress;
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (offset != 0)
		{
			throw new NotImplementedException();
		}
		int num = _reader.ReadBytes(buffer, _baseAddress + _position, count);
		_position += num;
		return num;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		switch (origin)
		{
		case SeekOrigin.Begin:
			_position = offset;
			break;
		case SeekOrigin.Current:
			_position += offset;
			break;
		default:
			throw new InvalidOperationException();
		}
		return _position;
	}

	public override void SetLength(long value)
	{
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException();
	}
}
