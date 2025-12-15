using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime;

internal class ReadVirtualStream : Stream
{
	private byte[] _tmp;

	private long _pos;

	private long _disp;

	private long _len;

	private IDataReader _dataReader;

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => true;

	public override long Length
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override long Position
	{
		get
		{
			return _pos;
		}
		set
		{
			_pos = value;
			if (_pos > _len)
			{
				_pos = _len;
			}
		}
	}

	public ReadVirtualStream(IDataReader dataReader, long displacement, long len)
	{
		_dataReader = dataReader;
		_disp = displacement;
		_len = len;
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (offset == 0)
		{
			if (_dataReader.ReadMemory((ulong)(_pos + _disp), buffer, count, out var bytesRead))
			{
				return bytesRead;
			}
			return 0;
		}
		if (_tmp == null || _tmp.Length < count)
		{
			_tmp = new byte[count];
		}
		if (!_dataReader.ReadMemory((ulong)(_pos + _disp), _tmp, count, out var bytesRead2))
		{
			return 0;
		}
		Buffer.BlockCopy(_tmp, 0, buffer, offset, bytesRead2);
		return bytesRead2;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		switch (origin)
		{
		case SeekOrigin.Begin:
			_pos = offset;
			break;
		case SeekOrigin.End:
			_pos = _len + offset;
			if (_pos > _len)
			{
				_pos = _len;
			}
			break;
		case SeekOrigin.Current:
			_pos += offset;
			if (_pos > _len)
			{
				_pos = _len;
			}
			break;
		}
		return _pos;
	}

	public override void SetLength(long value)
	{
		_len = value;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException();
	}
}
