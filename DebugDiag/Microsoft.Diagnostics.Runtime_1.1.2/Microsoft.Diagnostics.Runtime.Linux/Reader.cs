using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class Reader : IDisposable
{
	public const int MaxHeldBuffer = 4096;

	public const int InitialBufferSize = 64;

	private byte[] _buffer;

	private GCHandle _handle;

	private bool _disposed;

	public IAddressSpace DataSource { get; }

	public Reader(IAddressSpace source)
	{
		DataSource = source;
		_buffer = new byte[512];
		_handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
	}

	public T? TryRead<T>(long position) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		EnsureSize(num);
		if (DataSource.Read(position, _buffer, 0, num) != num)
		{
			return null;
		}
		return (T)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(T));
	}

	public T Read<T>(long position) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		EnsureSize(num);
		if (DataSource.Read(position, _buffer, 0, num) != num)
		{
			throw new IOException();
		}
		return (T)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(T));
	}

	public T Read<T>(ref long position) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		EnsureSize(num);
		if (DataSource.Read(position, _buffer, 0, num) != num)
		{
			throw new IOException();
		}
		T result = (T)Marshal.PtrToStructure(_handle.AddrOfPinnedObject(), typeof(T));
		position += num;
		return result;
	}

	public int ReadBytes(byte[] buffer, long offset, int size)
	{
		return DataSource.Read(offset, buffer, 0, size);
	}

	public byte[] ReadBytes(long offset, int size)
	{
		byte[] array = new byte[size];
		if (DataSource.Read(offset, array, 0, size) != size)
		{
			throw new IOException();
		}
		return array;
	}

	private void EnsureSize(int size)
	{
		if (_buffer.Length < size)
		{
			if (size > 4096)
			{
				throw new InvalidOperationException();
			}
			_handle.Free();
			_buffer = new byte[size];
			_handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
		}
	}

	~Reader()
	{
		Dispose(disposing: false);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_disposed = true;
			_handle.Free();
		}
	}

	public string ReadNullTerminatedAscii(long position, int len)
	{
		byte[] array = _buffer;
		if (len > _buffer.Length)
		{
			array = new byte[len];
		}
		int num = DataSource.Read(position, array, 0, len);
		if (num == 0)
		{
			return "";
		}
		if (array[num - 1] == 0)
		{
			num--;
		}
		return Encoding.ASCII.GetString(array, 0, num);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	internal unsafe string ReadNullTerminatedAscii(long position)
	{
		byte[] array = new byte[1];
		char[] array2 = new char[1];
		StringBuilder stringBuilder = new StringBuilder();
		while (DataSource.Read(position, array, 0, array.Length) >= array.Length && array[0] != 0)
		{
			fixed (byte* bytes = array)
			{
				fixed (char* chars = array2)
				{
					Encoding.ASCII.GetChars(bytes, array.Length, chars, array2.Length);
				}
			}
			stringBuilder.Append(array2[0]);
			position++;
		}
		return stringBuilder.ToString();
	}
}
