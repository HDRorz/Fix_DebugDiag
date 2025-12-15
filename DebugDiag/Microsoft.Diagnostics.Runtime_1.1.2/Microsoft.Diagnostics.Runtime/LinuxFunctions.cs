using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Linux;

namespace Microsoft.Diagnostics.Runtime;

internal sealed class LinuxFunctions : PlatformFunctions
{
	private delegate bool TryGetExport(IntPtr handle, string name, out IntPtr address);

	private const string LibDlGlibc = "libdl.so.2";

	private const string LibDl = "libdl.so";

	private static readonly byte[] s_versionString = Encoding.ASCII.GetBytes("@(#)Version ");

	private static readonly int s_versionLength = s_versionString.Length;

	private readonly Func<string, IntPtr> _loadLibrary;

	private readonly Func<IntPtr, bool> _freeLibrary;

	private readonly Func<IntPtr, string, IntPtr> _getExport;

	private const int RTLD_NOW = 2;

	public LinuxFunctions()
	{
		Type type = Type.GetType("System.Runtime.InteropServices.NativeLibrary, System.Runtime.InteropServices", throwOnError: false);
		if (type != null)
		{
			_loadLibrary = (Func<string, IntPtr>)(type.GetMethod("Load", new Type[1] { typeof(string) })?.CreateDelegate(typeof(Func<string, IntPtr>)));
			Action<IntPtr> freeLibrary = (Action<IntPtr>)(type.GetMethod("Free", new Type[1] { typeof(IntPtr) })?.CreateDelegate(typeof(Action<IntPtr>)));
			if (freeLibrary != null)
			{
				_freeLibrary = delegate(IntPtr ptr)
				{
					freeLibrary(ptr);
					return true;
				};
			}
			TryGetExport tryGetExport = (TryGetExport)(type.GetMethod("TryGetExport", new Type[3]
			{
				typeof(IntPtr),
				typeof(string),
				typeof(IntPtr).MakeByRefType()
			})?.CreateDelegate(typeof(TryGetExport)));
			if (tryGetExport != null)
			{
				_getExport = delegate(IntPtr handle, string name)
				{
					tryGetExport(handle, name, out var address);
					return address;
				};
			}
		}
		if (_loadLibrary != null && _freeLibrary != null && _getExport != null)
		{
			return;
		}
		bool flag = false;
		try
		{
			dlopen("/", 0);
		}
		catch (DllNotFoundException)
		{
			try
			{
				dlopen_glibc("/", 0);
				flag = true;
			}
			catch (DllNotFoundException)
			{
			}
		}
		if (flag)
		{
			_loadLibrary = (string filename) => dlopen_glibc(filename, 2);
			_freeLibrary = (IntPtr ptr) => dlclose_glibc(ptr) == 0;
			_getExport = dlsym_glibc;
		}
		else
		{
			_loadLibrary = (string filename) => dlopen(filename, 2);
			_freeLibrary = (IntPtr ptr) => dlclose(ptr) == 0;
			_getExport = dlsym;
		}
	}

	internal static void GetVersionInfo(IDataReader dataReader, ulong baseAddress, ElfFile loadedFile, out VersionInfo version)
	{
		foreach (ElfProgramHeader programHeader in loadedFile.ProgramHeaders)
		{
			if (programHeader.Type == ElfProgramHeaderType.Load && programHeader.IsWritable)
			{
				long virtualAddress = programHeader.VirtualAddress;
				long virtualSize = programHeader.VirtualSize;
				GetVersionInfo(dataReader, baseAddress + (ulong)virtualAddress, (ulong)virtualSize, out version);
				return;
			}
		}
		version = default(VersionInfo);
	}

	internal unsafe static void GetVersionInfo(IDataReader dataReader, ulong address, ulong size, out VersionInfo version)
	{
		byte[] array = new byte[1];
		char[] array2 = new char[1];
		byte[] array3 = new byte[s_versionLength];
		ulong num = address + size;
		while (address < num)
		{
			if (!dataReader.ReadMemory(address, array3, array3.Length, out var bytesRead) || bytesRead < s_versionLength)
			{
				address += (uint)s_versionLength;
				continue;
			}
			if (!EqualsVersion(array3))
			{
				address++;
				continue;
			}
			address += (uint)s_versionLength;
			StringBuilder stringBuilder = new StringBuilder();
			while (address < num && dataReader.ReadMemory(address, array, array.Length, out bytesRead) && bytesRead >= array.Length && array[0] != 0)
			{
				if (array[0] == 32)
				{
					try
					{
						Version version2 = Version.Parse(stringBuilder.ToString());
						version = new VersionInfo(version2.Major, version2.Minor, version2.Build, version2.Revision);
						return;
					}
					catch (FormatException)
					{
					}
					break;
				}
				fixed (byte* bytes = array)
				{
					fixed (char* chars = array2)
					{
						Encoding.ASCII.GetChars(bytes, array.Length, chars, array2.Length);
					}
				}
				stringBuilder.Append(array2[0]);
				address++;
			}
			break;
		}
		version = default(VersionInfo);
	}

	private static bool EqualsVersion(byte[] buffer)
	{
		if (buffer.Length < s_versionLength)
		{
			return false;
		}
		for (int i = 0; i < s_versionLength; i++)
		{
			if (buffer[i] != s_versionString[i])
			{
				return false;
			}
		}
		return true;
	}

	internal unsafe override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
	{
		using FileStream stream = File.OpenRead(dll);
		StreamAddressSpace streamAddressSpace = new StreamAddressSpace(stream);
		Reader reader = new Reader(streamAddressSpace);
		IElfHeader header = new ElfFile(reader, 0L).Header;
		long fileOffset = (long)new ElfSectionHeader(reader, header.Is64Bit, header.SectionHeaderOffset + header.SectionHeaderStringIndex * header.SectionHeaderEntrySize).FileOffset;
		long num = 0L;
		long num2 = 0L;
		for (int i = 0; i < header.SectionHeaderCount; i++)
		{
			if (i != header.SectionHeaderStringIndex)
			{
				ElfSectionHeader elfSectionHeader = new ElfSectionHeader(reader, header.Is64Bit, header.SectionHeaderOffset + i * header.SectionHeaderEntrySize);
				if (elfSectionHeader.Type == ElfSectionHeaderType.ProgBits && reader.ReadNullTerminatedAscii(fileOffset + elfSectionHeader.NameIndex) == ".data")
				{
					num = (long)elfSectionHeader.FileOffset;
					num2 = (long)elfSectionHeader.FileSize;
					break;
				}
			}
		}
		byte[] array = new byte[1];
		char[] array2 = new char[1];
		byte[] array3 = new byte[s_versionLength];
		long num3 = num;
		for (long num4 = num3 + num2; num3 < num4 && streamAddressSpace.Read(num3, array3, 0, array3.Length) >= s_versionLength; num3++)
		{
			if (!EqualsVersion(array3))
			{
				continue;
			}
			num3 += s_versionLength;
			StringBuilder stringBuilder = new StringBuilder();
			for (; num3 < num4 && streamAddressSpace.Read(num3, array, 0, array.Length) >= array.Length; num3++)
			{
				if (array[0] == 0)
				{
					break;
				}
				if (array[0] == 32)
				{
					try
					{
						Version version = Version.Parse(stringBuilder.ToString());
						major = version.Major;
						minor = version.Minor;
						revision = version.Build;
						patch = version.Revision;
						return true;
					}
					catch (FormatException)
					{
					}
					break;
				}
				fixed (byte* bytes = array)
				{
					fixed (char* chars = array2)
					{
						Encoding.ASCII.GetChars(bytes, array.Length, chars, array2.Length);
					}
				}
				stringBuilder.Append(array2[0]);
			}
			break;
		}
		major = (minor = (revision = (patch = 0)));
		return false;
	}

	public override bool TryGetWow64(IntPtr proc, out bool result)
	{
		result = false;
		return true;
	}

	public override IntPtr LoadLibrary(string filename)
	{
		return _loadLibrary(filename);
	}

	public override bool FreeLibrary(IntPtr module)
	{
		return _freeLibrary(module);
	}

	public override IntPtr GetProcAddress(IntPtr module, string method)
	{
		return _getExport(module, method);
	}

	[DllImport("libdl.so.2", EntryPoint = "dlopen")]
	private static extern IntPtr dlopen_glibc(string filename, int flags);

	[DllImport("libdl.so.2", EntryPoint = "dlclose")]
	private static extern int dlclose_glibc(IntPtr module);

	[DllImport("libdl.so.2", EntryPoint = "dlsym")]
	private static extern IntPtr dlsym_glibc(IntPtr handle, string symbol);

	[DllImport("libdl.so")]
	private static extern IntPtr dlopen(string filename, int flags);

	[DllImport("libdl.so")]
	private static extern int dlclose(IntPtr module);

	[DllImport("libdl.so")]
	private static extern IntPtr dlsym(IntPtr handle, string symbol);

	[DllImport("libc")]
	public static extern int symlink(string file, string symlink);
}
