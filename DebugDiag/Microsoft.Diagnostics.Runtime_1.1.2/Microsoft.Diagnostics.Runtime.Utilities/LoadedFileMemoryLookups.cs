using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class LoadedFileMemoryLookups
{
	private readonly Dictionary<string, SafeLoadLibraryHandle> _files;

	public LoadedFileMemoryLookups()
	{
		_files = new Dictionary<string, SafeLoadLibraryHandle>();
	}

	public unsafe void GetBytes(string fileName, ulong offset, IntPtr destination, uint bytesRequested, ref uint bytesWritten)
	{
		bytesWritten = 0u;
		IntPtr handle;
		if (!_files.ContainsKey(fileName))
		{
			handle = WindowsFunctions.NativeMethods.LoadLibraryEx(fileName, 0, WindowsFunctions.NativeMethods.LoadLibraryFlags.DontResolveDllReferences);
			_files[fileName] = new SafeLoadLibraryHandle(handle);
		}
		else
		{
			handle = _files[fileName].BaseAddress;
		}
		if (!handle.Equals(IntPtr.Zero))
		{
			handle = new IntPtr((byte*)handle.ToPointer() + offset);
			InternalGetBytes(handle, destination, bytesRequested, ref bytesWritten);
		}
	}

	private unsafe void InternalGetBytes(IntPtr src, IntPtr dest, uint bytesRequested, ref uint bytesWritten)
	{
		byte* ptr = (byte*)src.ToPointer();
		byte* ptr2 = (byte*)dest.ToPointer();
		for (bytesWritten = 0u; bytesWritten < bytesRequested; bytesWritten++)
		{
			ptr2[bytesWritten] = ptr[bytesWritten];
		}
	}
}
