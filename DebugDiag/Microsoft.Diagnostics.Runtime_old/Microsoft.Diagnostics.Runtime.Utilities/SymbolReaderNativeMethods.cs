using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class SymbolReaderNativeMethods
{
	internal struct IMAGEHLP_CBA_EVENT
	{
		public int Severity;

		public unsafe char* pStrDesc;

		public unsafe void* pData;
	}

	internal struct IMAGEHLP_DEFERRED_SYMBOL_LOAD64
	{
		public int SizeOfStruct;

		public long BaseOfImage;

		public int CheckSum;

		public int TimeDateStamp;

		public unsafe fixed sbyte FileName[260];

		public bool Reparse;

		public unsafe void* hFile;

		public int Flags;
	}

	internal struct SYMBOL_INFO
	{
		public uint SizeOfStruct;

		public uint TypeIndex;

		public ulong Reserved1;

		public ulong Reserved2;

		public uint Index;

		public uint Size;

		public ulong ModBase;

		public uint Flags;

		public ulong Value;

		public ulong Address;

		public uint Register;

		public uint Scope;

		public uint Tag;

		public uint NameLen;

		public uint MaxNameLen;

		public byte Name;
	}

	internal struct IMAGEHLP_LINE64
	{
		public uint SizeOfStruct;

		public unsafe void* Key;

		public uint LineNumber;

		public unsafe byte* FileName;

		public ulong Address;
	}

	internal delegate bool SymRegisterCallbackProc(IntPtr hProcess, SymCallbackActions ActionCode, ulong UserData, ulong UserContext);

	[Flags]
	public enum SymCallbackActions
	{
		CBA_DEBUG_INFO = 0x10000000,
		CBA_DEFERRED_SYMBOL_LOAD_CANCEL = 7,
		CBA_DEFERRED_SYMBOL_LOAD_COMPLETE = 2,
		CBA_DEFERRED_SYMBOL_LOAD_FAILURE = 3,
		CBA_DEFERRED_SYMBOL_LOAD_PARTIAL = 0x20,
		CBA_DEFERRED_SYMBOL_LOAD_START = 1,
		CBA_DUPLICATE_SYMBOL = 5,
		CBA_EVENT = 0x10,
		CBA_READ_MEMORY = 6,
		CBA_SET_OPTIONS = 8,
		CBA_SRCSRV_EVENT = 0x40000000,
		CBA_SRCSRV_INFO = 0x20000000,
		CBA_SYMBOLS_UNLOADED = 4
	}

	[Flags]
	public enum SymOptions : uint
	{
		SYMOPT_ALLOW_ABSOLUTE_SYMBOLS = 0x800u,
		SYMOPT_ALLOW_ZERO_ADDRESS = 0x1000000u,
		SYMOPT_AUTO_PUBLICS = 0x10000u,
		SYMOPT_CASE_INSENSITIVE = 1u,
		SYMOPT_DEBUG = 0x80000000u,
		SYMOPT_DEFERRED_LOADS = 4u,
		SYMOPT_DISABLE_SYMSRV_AUTODETECT = 0x2000000u,
		SYMOPT_EXACT_SYMBOLS = 0x400u,
		SYMOPT_FAIL_CRITICAL_ERRORS = 0x200u,
		SYMOPT_FAVOR_COMPRESSED = 0x800000u,
		SYMOPT_FLAT_DIRECTORY = 0x400000u,
		SYMOPT_IGNORE_CVREC = 0x80u,
		SYMOPT_IGNORE_IMAGEDIR = 0x200000u,
		SYMOPT_IGNORE_NT_SYMPATH = 0x1000u,
		SYMOPT_INCLUDE_32BIT_MODULES = 0x2000u,
		SYMOPT_LOAD_ANYTHING = 0x40u,
		SYMOPT_LOAD_LINES = 0x10u,
		SYMOPT_NO_CPP = 8u,
		SYMOPT_NO_IMAGE_SEARCH = 0x20000u,
		SYMOPT_NO_PROMPTS = 0x80000u,
		SYMOPT_NO_PUBLICS = 0x8000u,
		SYMOPT_NO_UNQUALIFIED_LOADS = 0x100u,
		SYMOPT_OVERWRITE = 0x100000u,
		SYMOPT_PUBLICS_ONLY = 0x4000u,
		SYMOPT_SECURE = 0x40000u,
		SYMOPT_UNDNAME = 2u
	}

	internal delegate bool SymFindFileInPathProc([In][MarshalAs(UnmanagedType.LPWStr)] string fileName, IntPtr context);

	internal const int SSRVOPT_DWORD = 2;

	internal const int SSRVOPT_DWORDPTR = 4;

	internal const int SSRVOPT_GUIDPTR = 8;

	internal const int MAX_PATH = 260;

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymFindFileInPathW(IntPtr hProcess, string searchPath, [In][MarshalAs(UnmanagedType.LPWStr)] string fileName, ref Guid id, int two, int three, int flags, [Out] StringBuilder filepath, SymFindFileInPathProc findCallback, IntPtr context);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymSrvGetFileIndexesW(string filePath, ref Guid id, ref int val1, ref int val2, int flags);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymInitializeW(IntPtr hProcess, string UserSearchPath, [MarshalAs(UnmanagedType.Bool)] bool fInvadeProcess);

	[DllImport("dbghelp.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymCleanup(IntPtr hProcess);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	internal unsafe static extern ulong SymLoadModuleExW(IntPtr hProcess, IntPtr hFile, string ImageName, string ModuleName, ulong BaseOfDll, uint DllSize, void* Data, uint Flags);

	[DllImport("dbghelp.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymUnloadModule64(IntPtr hProcess, ulong BaseOfDll);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymGetLineFromAddrW64(IntPtr hProcess, ulong Address, ref int Displacement, ref IMAGEHLP_LINE64 Line);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static extern bool SymFromAddrW(IntPtr hProcess, ulong Address, ref ulong Displacement, SYMBOL_INFO* Symbol);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool SymRegisterCallbackW64(IntPtr hProcess, SymRegisterCallbackProc callBack, ulong UserContext);

	[DllImport("dbghelp.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	internal static extern SymOptions SymSetOptions(SymOptions SymOptions);

	[DllImport("dbghelp.dll", SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	internal static extern SymOptions SymGetOptions();

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	internal static extern bool SymGetSourceFileW(IntPtr hProcess, ulong ImageBase, IntPtr Params, string fileSpec, StringBuilder filePathRet, int filePathRetSize);

	[DllImport("dbghelp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[SuppressUnmanagedCodeSecurity]
	internal static extern IntPtr SymSetHomeDirectoryW(IntPtr hProcess, string dir);
}
