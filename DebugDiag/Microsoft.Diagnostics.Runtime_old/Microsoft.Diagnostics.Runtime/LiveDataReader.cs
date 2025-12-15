using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal class LiveDataReader : IDataReader
{
	internal struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr Address;

		public IntPtr AllocationBase;

		public uint AllocationProtect;

		public IntPtr RegionSize;

		public uint State;

		public uint Protect;

		public uint Type;

		public ulong BaseAddress => (ulong)(long)Address;

		public ulong Size => (ulong)(long)RegionSize;
	}

	private enum ThreadAccess
	{
		THREAD_ALL_ACCESS = 2032639
	}

	private IntPtr _process;

	private int _pid;

	private const int PROCESS_VM_READ = 16;

	private const int PROCESS_QUERY_INFORMATION = 1024;

	private byte[] _ptrBuffer = new byte[IntPtr.Size];

	public bool IsMinidump => false;

	public bool CanReadAsync => false;

	public LiveDataReader(int pid)
	{
		_pid = pid;
		_process = OpenProcess(1040, bInheritHandle: false, pid);
		if (_process == IntPtr.Zero)
		{
			throw new ClrDiagnosticsException($"Could not attach to process. Error {Marshal.GetLastWin32Error()}.");
		}
		using Process process = Process.GetCurrentProcess();
		if (NativeMethods.TryGetWow64(process.Handle, out var result) && NativeMethods.TryGetWow64(_process, out var result2) && result != result2)
		{
			throw new ClrDiagnosticsException("Dac architecture mismatch!");
		}
	}

	public void Close()
	{
		if (_process != IntPtr.Zero)
		{
			CloseHandle(_process);
			_process = IntPtr.Zero;
		}
	}

	public void Flush()
	{
	}

	public Architecture GetArchitecture()
	{
		if (IntPtr.Size == 4)
		{
			return Architecture.X86;
		}
		return Architecture.Amd64;
	}

	public uint GetPointerSize()
	{
		return (uint)IntPtr.Size;
	}

	public IList<ModuleInfo> EnumerateModules()
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		EnumProcessModules(_process, null, 0u, out var lpcbNeeded);
		IntPtr[] array = new IntPtr[lpcbNeeded / 4];
		uint cb = (uint)(array.Length * 4);
		if (!EnumProcessModules(_process, array, cb, out lpcbNeeded))
		{
			throw new ClrDiagnosticsException("Unable to get process modules.", ClrDiagnosticsException.HR.DataRequestError);
		}
		for (int i = 0; i < array.Length; i++)
		{
			IntPtr intPtr = array[i];
			if (intPtr == IntPtr.Zero)
			{
				break;
			}
			StringBuilder stringBuilder = new StringBuilder(1024);
			GetModuleFileNameExA(_process, intPtr, stringBuilder, stringBuilder.Capacity);
			string fileName = stringBuilder.ToString();
			ModuleInfo moduleInfo = new ModuleInfo(this);
			moduleInfo.ImageBase = (ulong)intPtr.ToInt64();
			moduleInfo.FileName = fileName;
			GetFileProperties(moduleInfo.ImageBase, out var filesize, out var timestamp);
			moduleInfo.FileSize = filesize;
			moduleInfo.TimeStamp = timestamp;
			list.Add(moduleInfo);
		}
		return list;
	}

	public void GetVersionInfo(ulong addr, out VersionInfo version)
	{
		StringBuilder stringBuilder = new StringBuilder(1024);
		GetModuleFileNameExA(_process, new IntPtr((long)addr), stringBuilder, stringBuilder.Capacity);
		if (NativeMethods.GetFileVersion(stringBuilder.ToString(), out var major, out var minor, out var revision, out var patch))
		{
			version = new VersionInfo(major, minor, revision, patch);
		}
		else
		{
			version = default(VersionInfo);
		}
	}

	public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		try
		{
			return ReadProcessMemory(_process, new IntPtr((long)address), buffer, bytesRequested, out bytesRead) != 0;
		}
		catch
		{
			bytesRead = 0;
			return false;
		}
	}

	public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		try
		{
			return RawPinvokes.ReadProcessMemory(_process, new IntPtr((long)address), buffer, bytesRequested, out bytesRead) != 0;
		}
		catch
		{
			bytesRead = 0;
			return false;
		}
	}

	public unsafe ulong ReadPointerUnsafe(ulong addr)
	{
		if (!ReadMemory(addr, _ptrBuffer, IntPtr.Size, out var _))
		{
			return 0uL;
		}
		fixed (byte* ptrBuffer = _ptrBuffer)
		{
			if (IntPtr.Size == 4)
			{
				return *(uint*)ptrBuffer;
			}
			return *(ulong*)ptrBuffer;
		}
	}

	public unsafe uint ReadDwordUnsafe(ulong addr)
	{
		if (!ReadMemory(addr, _ptrBuffer, 4, out var _))
		{
			return 0u;
		}
		fixed (byte* ptrBuffer = _ptrBuffer)
		{
			return *(uint*)ptrBuffer;
		}
	}

	public ulong GetThreadTeb(uint thread)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<uint> EnumerateAllThreads()
	{
		Process processById = Process.GetProcessById(_pid);
		foreach (ProcessThread thread in processById.Threads)
		{
			yield return (uint)thread.Id;
		}
	}

	public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
	{
		vq = default(VirtualQueryData);
		MEMORY_BASIC_INFORMATION lpBuffer = default(MEMORY_BASIC_INFORMATION);
		if (VirtualQueryEx(lpAddress: new IntPtr((long)addr), hProcess: _process, lpBuffer: ref lpBuffer, dwLength: new IntPtr(Marshal.SizeOf(lpBuffer))) == 0)
		{
			return false;
		}
		vq.BaseAddress = lpBuffer.BaseAddress;
		vq.Size = lpBuffer.Size;
		return true;
	}

	public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		using SafeWin32Handle safeWin32Handle = OpenThread(ThreadAccess.THREAD_ALL_ACCESS, bInheritHandle: true, threadID);
		if (safeWin32Handle.IsInvalid)
		{
			return false;
		}
		return GetThreadContext(safeWin32Handle.DangerousGetHandle(), context);
	}

	public unsafe bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
	{
		using SafeWin32Handle safeWin32Handle = OpenThread(ThreadAccess.THREAD_ALL_ACCESS, bInheritHandle: true, threadID);
		if (safeWin32Handle.IsInvalid)
		{
			return false;
		}
		fixed (byte* value = context)
		{
			return GetThreadContext(safeWin32Handle.DangerousGetHandle(), new IntPtr(value));
		}
	}

	private void GetFileProperties(ulong moduleBase, out uint filesize, out uint timestamp)
	{
		filesize = 0u;
		timestamp = 0u;
		byte[] array = new byte[4];
		if (!ReadMemory(moduleBase + 60, array, array.Length, out var bytesRead) || bytesRead != array.Length)
		{
			return;
		}
		uint num = (uint)BitConverter.ToInt32(array, 0);
		int num2 = 4;
		if (ReadMemory(moduleBase + num, array, array.Length, out bytesRead) && bytesRead == array.Length && BitConverter.ToInt32(array, 0) == 17744)
		{
			if (ReadMemory((ulong)((long)(moduleBase + num) + (long)num2 + 4), array, array.Length, out bytesRead) && bytesRead == array.Length)
			{
				timestamp = (uint)BitConverter.ToInt32(array, 0);
			}
			if (ReadMemory((ulong)((long)(moduleBase + num) + (long)num2 + 76), array, array.Length, out bytesRead) && bytesRead == array.Length)
			{
				filesize = (uint)BitConverter.ToInt32(array, 0);
			}
		}
	}

	[DllImport("kernel32.dll")]
	public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("psapi.dll", SetLastError = true)]
	public static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

	[DllImport("psapi.dll", SetLastError = true)]
	public static extern uint GetModuleFileNameExA([In] IntPtr hProcess, [In] IntPtr hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);

	[DllImport("kernel32.dll")]
	private static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

	[DllImport("kernel32.dll")]
	private static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern SafeWin32Handle OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

	public AsyncMemoryReadResult ReadMemoryAsync(ulong address, int bytesRequested)
	{
		throw new NotImplementedException();
	}
}
