using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DebugDiag.DotNet;

internal class LibraryModule : IDisposable
{
	private static class Win32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadLibrary(string lpFileName);
	}

	private readonly IntPtr _handle;

	private readonly string _filePath;

	public string FilePath => _filePath;

	public static LibraryModule LoadModule(string filePath)
	{
		LibraryModule libraryModule = new LibraryModule(Win32.LoadLibrary(filePath), filePath);
		if (libraryModule._handle == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), $"Cannot load library (0x{Marshal.GetLastWin32Error():x}): {filePath}");
		}
		return libraryModule;
	}

	private LibraryModule(IntPtr handle, string filePath)
	{
		_filePath = filePath;
		_handle = handle;
	}

	~LibraryModule()
	{
		if (_handle != IntPtr.Zero)
		{
			Win32.FreeLibrary(_handle);
		}
	}

	public void Dispose()
	{
		if (_handle != IntPtr.Zero)
		{
			Win32.FreeLibrary(_handle);
		}
		GC.SuppressFinalize(this);
	}

	public IntPtr GetProcAddress(string name)
	{
		IntPtr procAddress = Win32.GetProcAddress(_handle, "DllGetClassObject");
		if (procAddress == IntPtr.Zero)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			string message = $"Cannot find proc {name} in {_filePath}";
			throw new Win32Exception(lastWin32Error, message);
		}
		return procAddress;
	}
}
