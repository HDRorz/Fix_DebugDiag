using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct DumpPointer
{
	private struct StructUInt64
	{
		public readonly ulong Value;
	}

	private readonly IntPtr _pointer;

	private readonly uint _size;

	public static DumpPointer DangerousMakeDumpPointer(IntPtr rawPointer, uint size)
	{
		return new DumpPointer(rawPointer, size);
	}

	private DumpPointer(IntPtr rawPointer, uint size)
	{
		_pointer = rawPointer;
		_size = size;
	}

	public DumpPointer Shrink(uint size)
	{
		EnsureSizeRemaining(size);
		return new DumpPointer(_pointer, _size - size);
	}

	public DumpPointer Adjust(uint delta)
	{
		EnsureSizeRemaining(delta);
		return new DumpPointer(new IntPtr(_pointer.ToInt64() + delta), _size - delta);
	}

	public DumpPointer Adjust(ulong delta64)
	{
		uint num = (uint)delta64;
		EnsureSizeRemaining(num);
		ulong value = (ulong)(_pointer.ToInt64() + num);
		return new DumpPointer(new IntPtr((long)value), _size - num);
	}

	public void Copy(IntPtr dest, uint destinationBufferSizeInBytes, uint numberBytesToCopy)
	{
		EnsureSizeRemaining(numberBytesToCopy);
		if (numberBytesToCopy > destinationBufferSizeInBytes)
		{
			throw new ArgumentException("Buffer too small");
		}
		RawCopy(_pointer, dest, numberBytesToCopy);
	}

	private static void RawCopy(IntPtr src, IntPtr dest, uint numBytes)
	{
		DumpReader.RtlMoveMemory(dest, src, new IntPtr(numBytes));
	}

	internal unsafe ulong GetUlong()
	{
		return *(ulong*)_pointer.ToPointer();
	}

	internal unsafe uint GetDword()
	{
		return *(uint*)_pointer.ToPointer();
	}

	public void Copy(byte[] dest, int offset, int numberBytesToCopy)
	{
		EnsureSizeRemaining((uint)numberBytesToCopy);
		Marshal.Copy(_pointer, dest, offset, numberBytesToCopy);
	}

	public void Copy(IntPtr destinationBuffer, uint sizeBytes)
	{
		EnsureSizeRemaining(sizeBytes);
		RawCopy(_pointer, destinationBuffer, sizeBytes);
	}

	public int ReadInt32()
	{
		EnsureSizeRemaining(4u);
		return Marshal.ReadInt32(_pointer);
	}

	public long ReadInt64()
	{
		EnsureSizeRemaining(8u);
		return Marshal.ReadInt64(_pointer);
	}

	public uint ReadUInt32()
	{
		EnsureSizeRemaining(4u);
		return (uint)Marshal.ReadInt32(_pointer);
	}

	public ulong ReadUInt64()
	{
		EnsureSizeRemaining(8u);
		return (ulong)Marshal.ReadInt64(_pointer);
	}

	public string ReadAsUnicodeString(int lengthChars)
	{
		int requestedSize = lengthChars * 2;
		EnsureSizeRemaining((uint)requestedSize);
		return Marshal.PtrToStringUni(_pointer, lengthChars);
	}

	public T PtrToStructure<T>(uint offset)
	{
		return Adjust(offset).PtrToStructure<T>();
	}

	public T PtrToStructureAdjustOffset<T>(ref uint offset)
	{
		T val = Adjust(offset).PtrToStructure<T>();
		offset += (uint)Marshal.SizeOf((object)val);
		return val;
	}

	public T PtrToStructure<T>()
	{
		uint requestedSize = (uint)Marshal.SizeOf(typeof(T));
		EnsureSizeRemaining(requestedSize);
		return (T)Marshal.PtrToStructure(_pointer, typeof(T));
	}

	private void EnsureSizeRemaining(uint requestedSize)
	{
		if (requestedSize > _size)
		{
			throw new ClrDiagnosticsException("The given crash dump is in an incorrect format.", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
	}
}
