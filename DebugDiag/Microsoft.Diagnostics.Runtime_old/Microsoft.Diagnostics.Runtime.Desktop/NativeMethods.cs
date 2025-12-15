using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class NativeMethods
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

	[Flags]
	public enum PageProtection : uint
	{
		NoAccess = 1u,
		Readonly = 2u,
		ReadWrite = 4u,
		WriteCopy = 8u,
		Execute = 0x10u,
		ExecuteRead = 0x20u,
		ExecuteReadWrite = 0x40u,
		ExecuteWriteCopy = 0x80u,
		Guard = 0x100u,
		NoCache = 0x200u,
		WriteCombine = 0x400u
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate int CreateDacInstance([In][ComAliasName("REFIID")] ref Guid riid, [In][MarshalAs(UnmanagedType.Interface)] IDacDataTarget data, [MarshalAs(UnmanagedType.IUnknown)] out object ppObj);

	private const string Kernel32LibraryName = "kernel32.dll";

	public const uint FILE_MAP_READ = 4u;

	private const int VS_FIXEDFILEINFO_size = 52;

	public static short IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14;

	public static bool LoadNative(string dllName)
	{
		return LoadLibrary(dllName) != IntPtr.Zero;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern SafeWin32Handle CreateFileMapping(SafeFileHandle hFile, IntPtr lpFileMappingAttributes, PageProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnmapViewOfFile(IntPtr baseAddress);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern SafeMapViewHandle MapViewOfFile(SafeWin32Handle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);

	[DllImport("kernel32.dll")]
	public static extern void RtlMoveMemory(IntPtr destination, IntPtr source, IntPtr numberBytes);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(IntPtr handle);

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool FreeLibrary(IntPtr hModule);

	public static IntPtr LoadLibrary(string lpFileName)
	{
		return LoadLibraryEx(lpFileName, 0, LoadLibraryFlags.NoFlags);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr LoadLibraryEx(string fileName, int hFile, LoadLibraryFlags dwFlags);

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWow64Process([In] IntPtr hProcess, out bool isWow64);

	[DllImport("version.dll")]
	internal static extern bool GetFileVersionInfo(string sFileName, int handle, int size, byte[] infoBuffer);

	[DllImport("version.dll")]
	internal static extern int GetFileVersionInfoSize(string sFileName, out int handle);

	[DllImport("version.dll")]
	internal static extern bool VerQueryValue(byte[] pBlock, string pSubBlock, out IntPtr val, out int len);

	[DllImport("dbgeng.dll")]
	internal static extern uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);

	[DllImport("kernel32.dll")]
	internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

	[DllImport("dbghelp.dll")]
	internal static extern IntPtr ImageDirectoryEntryToData(IntPtr mapping, bool mappedAsImage, short directoryEntry, out uint size);

	[DllImport("dbghelp.dll")]
	public static extern IntPtr ImageRvaToVa(IntPtr mapping, IntPtr baseAddr, uint rva, IntPtr lastRvaSection);

	[DllImport("dbghelp.dll")]
	public static extern IntPtr ImageNtHeader(IntPtr imageBase);

	internal static bool IsEqualFileVersion(string file, VersionInfo version)
	{
		if (!GetFileVersion(file, out var major, out var minor, out var revision, out var patch))
		{
			return false;
		}
		if (major == version.Major && minor == version.Minor && revision == version.Revision)
		{
			return patch == version.Patch;
		}
		return false;
	}

	internal static bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
	{
		major = (minor = (revision = (patch = 0)));
		int handle;
		int len = GetFileVersionInfoSize(dll, out handle);
		if (len <= 0)
		{
			return false;
		}
		byte[] array = new byte[len];
		if (!GetFileVersionInfo(dll, handle, len, array))
		{
			return false;
		}
		if (!VerQueryValue(array, "\\", out var val, out len))
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

	internal static bool TryGetWow64(IntPtr proc, out bool result)
	{
		if (Environment.OSVersion.Version.Major > 5 || (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1))
		{
			return IsWow64Process(proc, out result);
		}
		result = false;
		return false;
	}
}
