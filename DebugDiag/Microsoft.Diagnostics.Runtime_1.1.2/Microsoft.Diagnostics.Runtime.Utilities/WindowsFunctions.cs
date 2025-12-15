using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class WindowsFunctions : PlatformFunctions
{
	internal static class NativeMethods
	{
		[Flags]
		public enum LoadLibraryFlags : uint
		{
			NoFlags = 0u,
			DontResolveDllReferences = 1u,
			LoadIgnoreCodeAuthzLevel = 0x10u,
			LoadLibraryAsDatafile = 2u,
			LoadLibraryAsDatafileExclusive = 0x40u,
			LoadLibraryAsImageResource = 0x20u,
			LoadWithAlteredSearchPath = 8u
		}

		private const string Kernel32LibraryName = "kernel32.dll";

		public const uint FILE_MAP_READ = 4u;

		private const int VS_FIXEDFILEINFO_size = 52;

		public static short IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14;

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeLibrary(IntPtr hModule);

		public static IntPtr LoadLibrary(string lpFileName)
		{
			return LoadLibraryEx(lpFileName, 0, LoadLibraryFlags.NoFlags);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadLibraryEx(string fileName, int hFile, LoadLibraryFlags dwFlags);

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process([In] IntPtr hProcess, out bool isWow64);

		[DllImport("version.dll")]
		public static extern bool GetFileVersionInfo(string sFileName, int handle, int size, byte[] infoBuffer);

		[DllImport("version.dll")]
		public static extern int GetFileVersionInfoSize(string sFileName, out int handle);

		[DllImport("version.dll")]
		public static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out IntPtr val, out int len);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
	}

	public override bool FreeLibrary(IntPtr module)
	{
		return NativeMethods.FreeLibrary(module);
	}

	internal override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
	{
		major = (minor = (revision = (patch = 0)));
		int handle;
		int len = NativeMethods.GetFileVersionInfoSize(dll, out handle);
		if (len <= 0)
		{
			return false;
		}
		byte[] array = new byte[len];
		if (!NativeMethods.GetFileVersionInfo(dll, handle, len, array))
		{
			return false;
		}
		if (!NativeMethods.VerQueryValue(array, "\\", out var val, out len))
		{
			return false;
		}
		byte[] array2 = new byte[len];
		Marshal.Copy(val, array2, 0, len);
		minor = (ushort)Marshal.ReadInt16(array2, 8);
		major = (ushort)Marshal.ReadInt16(array2, 10);
		patch = (ushort)Marshal.ReadInt16(array2, 12);
		revision = (ushort)Marshal.ReadInt16(array2, 14);
		return true;
	}

	public override IntPtr GetProcAddress(IntPtr module, string method)
	{
		return NativeMethods.GetProcAddress(module, method);
	}

	public override IntPtr LoadLibrary(string lpFileName)
	{
		return NativeMethods.LoadLibraryEx(lpFileName, 0, NativeMethods.LoadLibraryFlags.NoFlags);
	}

	public override bool TryGetWow64(IntPtr proc, out bool result)
	{
		if (Environment.OSVersion.Version.Major > 5 || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
		{
			return NativeMethods.IsWow64Process(proc, out result);
		}
		result = false;
		return false;
	}
}
