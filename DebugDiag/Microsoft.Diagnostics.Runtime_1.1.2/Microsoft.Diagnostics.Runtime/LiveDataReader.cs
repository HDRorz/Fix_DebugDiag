using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime;

internal class LiveDataReader : IDataReader2, IDataReader
{
	private readonly int _originalPid;

	private readonly IntPtr _snapshotHandle;

	private readonly IntPtr _cloneHandle;

	private readonly IntPtr _process;

	private readonly int _pid;

	private const int PROCESS_VM_READ = 16;

	private const int PROCESS_QUERY_INFORMATION = 1024;

	private readonly byte[] _ptrBuffer = new byte[IntPtr.Size];

	public uint ProcessId => (uint)_pid;

	public bool IsMinidump => false;

	public LiveDataReader(int pid, bool createSnapshot)
	{
		if (createSnapshot)
		{
			_originalPid = pid;
			int num = PssCaptureSnapshot(Process.GetProcessById(pid).Handle, PSS_CAPTURE_FLAGS.PSS_CAPTURE_VA_CLONE, (IntPtr.Size == 8) ? 1048607 : 65599, out _snapshotHandle);
			if (num != 0)
			{
				throw new ClrDiagnosticsException($"Could not create snapshot to process. Error {num}.", ClrDiagnosticsExceptionKind.Unknown, num);
			}
			num = PssQuerySnapshot(_snapshotHandle, PSS_QUERY_INFORMATION_CLASS.PSS_QUERY_VA_CLONE_INFORMATION, out _cloneHandle, IntPtr.Size);
			if (num != 0)
			{
				throw new ClrDiagnosticsException($"Could not query the snapshot. Error {num}.", ClrDiagnosticsExceptionKind.Unknown, num);
			}
			_pid = GetProcessId(_cloneHandle);
		}
		else
		{
			_pid = pid;
		}
		_process = OpenProcess(1040, bInheritHandle: false, _pid);
		if (_process == IntPtr.Zero)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			throw new ClrDiagnosticsException($"Could not attach to process. Error {lastWin32Error}.", ClrDiagnosticsExceptionKind.Unknown, lastWin32Error);
		}
		using Process process = Process.GetCurrentProcess();
		if (DataTarget.PlatformFunctions.TryGetWow64(process.Handle, out var result) && DataTarget.PlatformFunctions.TryGetWow64(_process, out var result2) && result != result2)
		{
			throw new ClrDiagnosticsException("Dac architecture mismatch!", ClrDiagnosticsExceptionKind.DacError);
		}
	}

	public void Close()
	{
		if (_originalPid != 0)
		{
			int num = PssFreeSnapshot(Process.GetCurrentProcess().Handle, _snapshotHandle);
			if (num != 0)
			{
				throw new ClrDiagnosticsException($"Could not free the snapshot. Error {num}.", ClrDiagnosticsExceptionKind.Unknown, num);
			}
			try
			{
				Process.GetProcessById(_pid).Kill();
			}
			catch (Win32Exception)
			{
			}
		}
		if (_process != IntPtr.Zero)
		{
			CloseHandle(_process);
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
			throw new ClrDiagnosticsException("Unable to get process modules.", ClrDiagnosticsExceptionKind.DataRequestError);
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
			ulong num = (ulong)intPtr.ToInt64();
			GetFileProperties(num, out var filesize, out var timestamp);
			string fileName = stringBuilder.ToString();
			ModuleInfo item = new ModuleInfo(this, null)
			{
				ImageBase = num,
				FileName = fileName,
				FileSize = filesize,
				TimeStamp = timestamp
			};
			list.Add(item);
		}
		return list;
	}

	public void GetVersionInfo(ulong addr, out VersionInfo version)
	{
		StringBuilder stringBuilder = new StringBuilder(1024);
		GetModuleFileNameExA(_process, addr.AsIntPtr(), stringBuilder, stringBuilder.Capacity);
		if (DataTarget.PlatformFunctions.GetFileVersion(stringBuilder.ToString(), out var major, out var minor, out var revision, out var patch))
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
			return ReadProcessMemory(_process, address.AsIntPtr(), buffer, bytesRequested, out bytesRead) != 0;
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
			return ReadProcessMemory(_process, address.AsIntPtr(), buffer, bytesRequested, out bytesRead) != 0;
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
		IntPtr lpAddress = addr.AsIntPtr();
		if (VirtualQueryEx(_process, lpAddress, ref lpBuffer, new IntPtr(Marshal.SizeOf((object)lpBuffer))) == 0)
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

	[DllImport("kernel32")]
	private static extern int PssCaptureSnapshot(IntPtr ProcessHandle, PSS_CAPTURE_FLAGS CaptureFlags, int ThreadContextFlags, out IntPtr SnapshotHandle);

	[DllImport("kernel32")]
	private static extern int PssFreeSnapshot(IntPtr ProcessHandle, IntPtr SnapshotHandle);

	[DllImport("kernel32")]
	private static extern int PssQuerySnapshot(IntPtr SnapshotHandle, PSS_QUERY_INFORMATION_CLASS InformationClass, out IntPtr Buffer, int BufferLength);

	[DllImport("kernel32")]
	private static extern int GetProcessId(IntPtr hObject);

	[DllImport("kernel32.dll")]
	internal static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out int lpNumberOfBytesRead);
}
