using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class DumpReader : IDisposable
{
	private volatile bool _disposing;

	private volatile int _lock;

	protected MinidumpMemoryChunks _memoryChunks;

	protected LoadedFileMemoryLookups _mappedFileMemory;

	private readonly FileStream _file;

	private readonly SafeWin32Handle _fileMapping;

	private readonly SafeMapViewHandle _view;

	private DumpPointer _base;

	private MINIDUMP_SYSTEM_INFO _info;

	public bool IsMinidump { get; set; }

	public Version Version => _info.Version;

	public ProcessorArchitecture ProcessorArchitecture
	{
		get
		{
			EnsureValid();
			return _info.ProcessorArchitecture;
		}
	}

	protected internal DumpPointer TranslateDescriptor(MINIDUMP_LOCATION_DESCRIPTOR location)
	{
		DumpPointer result = TranslateRVA(location.Rva);
		result.Shrink(location.DataSize);
		return result;
	}

	protected internal DumpPointer TranslateRVA(ulong rva)
	{
		return _base.Adjust(rva);
	}

	protected internal DumpPointer TranslateRVA(RVA rva)
	{
		return _base.Adjust(rva.Value);
	}

	protected internal DumpPointer TranslateRVA(RVA64 rva)
	{
		return _base.Adjust(rva.Value);
	}

	protected internal string GetString(RVA rva)
	{
		DumpPointer ptr = TranslateRVA(rva);
		return GetString(ptr);
	}

	protected internal string GetString(DumpPointer ptr)
	{
		EnsureValid();
		int num = ptr.ReadInt32();
		ptr = ptr.Adjust(4u);
		int lengthChars = num / 2;
		return ptr.ReadAsUnicodeString(lengthChars);
	}

	public bool VirtualQuery(ulong addr, out VirtualQueryData data)
	{
		uint num = 0u;
		uint num2 = (uint)((int)_memoryChunks.Count - 1);
		while (num <= num2)
		{
			uint num3 = (num2 + num) / 2;
			ulong num4 = _memoryChunks.StartAddress(num3);
			if (addr < num4)
			{
				num2 = num3 - 1;
				continue;
			}
			if (_memoryChunks.EndAddress(num3) < addr)
			{
				num = num3 + 1;
				continue;
			}
			data = new VirtualQueryData(num4, _memoryChunks.Size(num3));
			return true;
		}
		data = default(VirtualQueryData);
		return false;
	}

	public IEnumerable<VirtualQueryData> EnumerateMemoryRanges(ulong startAddress, ulong endAddress)
	{
		for (ulong i = 0uL; i < _memoryChunks.Count; i++)
		{
			ulong num = _memoryChunks.StartAddress(i);
			if (_memoryChunks.EndAddress(i) >= startAddress && endAddress >= num)
			{
				ulong size = _memoryChunks.Size(i);
				yield return new VirtualQueryData(num, size);
			}
		}
	}

	public byte[] ReadMemory(ulong targetAddress, int length)
	{
		byte[] array = new byte[length];
		ReadMemory(targetAddress, array, length);
		return array;
	}

	public void ReadMemory(ulong targetAddress, byte[] buffer, int cbRequestSize)
	{
		GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
		try
		{
			ReadMemory(targetAddress, gCHandle.AddrOfPinnedObject(), (uint)cbRequestSize);
		}
		finally
		{
			gCHandle.Free();
		}
	}

	public void ReadMemory(ulong targetRequestStart, IntPtr destinationBuffer, uint destinationBufferSizeInBytes)
	{
		uint num = ReadPartialMemory(targetRequestStart, destinationBuffer, destinationBufferSizeInBytes);
		if (num != destinationBufferSizeInBytes)
		{
			throw new ClrDiagnosticsException(string.Format(CultureInfo.CurrentUICulture, "Memory missing at {0}. Could only read {1} bytes of {2} total bytes requested.", new object[3]
			{
				targetRequestStart.ToString("x"),
				num,
				destinationBufferSizeInBytes
			}), ClrDiagnosticsExceptionKind.CrashDumpError);
		}
	}

	public virtual uint ReadPartialMemory(ulong targetRequestStart, IntPtr destinationBuffer, uint destinationBufferSizeInBytes)
	{
		return ReadPartialMemoryInternal(targetRequestStart, destinationBuffer, destinationBufferSizeInBytes, 0u);
	}

	internal ulong ReadPointerUnsafe(ulong addr)
	{
		int chunkContainingAddress = _memoryChunks.GetChunkContainingAddress(addr);
		if (chunkContainingAddress == -1)
		{
			return 0uL;
		}
		DumpPointer dumpPointer = TranslateRVA(_memoryChunks.RVA((uint)chunkContainingAddress));
		ulong delta = addr - _memoryChunks.StartAddress((uint)chunkContainingAddress);
		if (IntPtr.Size == 4)
		{
			return dumpPointer.Adjust(delta).GetDword();
		}
		return dumpPointer.Adjust(delta).GetUlong();
	}

	internal uint ReadDwordUnsafe(ulong addr)
	{
		int chunkContainingAddress = _memoryChunks.GetChunkContainingAddress(addr);
		if (chunkContainingAddress == -1)
		{
			return 0u;
		}
		DumpPointer dumpPointer = TranslateRVA(_memoryChunks.RVA((uint)chunkContainingAddress));
		ulong delta = addr - _memoryChunks.StartAddress((uint)chunkContainingAddress);
		return dumpPointer.Adjust(delta).GetDword();
	}

	public virtual int ReadPartialMemory(ulong targetRequestStart, byte[] destinationBuffer, int bytesRequested)
	{
		EnsureValid();
		if (bytesRequested <= 0)
		{
			return 0;
		}
		if (bytesRequested > destinationBuffer.Length)
		{
			bytesRequested = destinationBuffer.Length;
		}
		int num = 0;
		do
		{
			int chunkContainingAddress = _memoryChunks.GetChunkContainingAddress(targetRequestStart + (uint)num);
			if (chunkContainingAddress == -1)
			{
				break;
			}
			DumpPointer dumpPointer = TranslateRVA(_memoryChunks.RVA((uint)chunkContainingAddress));
			ulong num2 = targetRequestStart + (uint)num - _memoryChunks.StartAddress((uint)chunkContainingAddress);
			ulong num3 = _memoryChunks.Size((uint)chunkContainingAddress) - num2;
			int num4 = bytesRequested - num;
			if (num3 < (uint)num4)
			{
				num4 = (int)num3;
			}
			if (num4 == 0)
			{
				break;
			}
			dumpPointer.Adjust(num2).Copy(destinationBuffer, num, num4);
			num += num4;
		}
		while (num < bytesRequested);
		return num;
	}

	private bool AcquireReadLock()
	{
		int num = 0;
		int num2 = 0;
		do
		{
			num2 = _lock;
			if (_disposing || num2 < 0)
			{
				return false;
			}
			num = Interlocked.CompareExchange(ref _lock, num2 + 1, num2);
		}
		while (num != num2);
		return true;
	}

	private void ReleaseReadLock()
	{
		Interlocked.Decrement(ref _lock);
	}

	private bool AcquireWriteLock()
	{
		int num = 0;
		for (num = Interlocked.CompareExchange(ref _lock, -1, 0); num != 0; num = Interlocked.CompareExchange(ref _lock, -1, 0))
		{
			Thread.Sleep(50);
		}
		return true;
	}

	private void ReleaseWriteLock()
	{
		Interlocked.Increment(ref _lock);
	}

	protected uint ReadPartialMemoryInternal(ulong targetRequestStart, IntPtr destinationBuffer, uint destinationBufferSizeInBytes, uint startIndex)
	{
		EnsureValid();
		if (destinationBufferSizeInBytes == 0)
		{
			return 0u;
		}
		uint num = 0u;
		do
		{
			int chunkContainingAddress = _memoryChunks.GetChunkContainingAddress(targetRequestStart + num);
			if (chunkContainingAddress == -1)
			{
				break;
			}
			DumpPointer dumpPointer = TranslateRVA(_memoryChunks.RVA((uint)chunkContainingAddress));
			uint num2 = (uint)(targetRequestStart + num - _memoryChunks.StartAddress((uint)chunkContainingAddress));
			int val = (int)_memoryChunks.Size((uint)chunkContainingAddress) - (int)num2;
			uint val2 = destinationBufferSizeInBytes - num;
			uint num3 = Math.Min((uint)val, val2);
			if (num3 == 0)
			{
				break;
			}
			IntPtr dest = new IntPtr(destinationBuffer.ToInt64() + num);
			uint destinationBufferSizeInBytes2 = destinationBufferSizeInBytes - num;
			dumpPointer.Adjust(num2).Copy(dest, destinationBufferSizeInBytes2, num3);
			num += num3;
		}
		while (num < destinationBufferSizeInBytes);
		return num;
	}

	public override string ToString()
	{
		if (_file == null)
		{
			return "Empty";
		}
		return _file.Name;
	}

	public DumpReader(string path)
	{
		_file = File.OpenRead(path);
		long length = _file.Length;
		_fileMapping = CreateFileMapping(_file.SafeFileHandle, IntPtr.Zero, PageProtection.Readonly, 0u, 0u, null);
		if (_fileMapping.IsInvalid)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
		}
		_view = MapViewOfFile(_fileMapping, 4u, 0u, 0u, IntPtr.Zero);
		if (_view.IsInvalid)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
		}
		_base = DumpPointer.DangerousMakeDumpPointer(_view.BaseAddress, (uint)length);
		DumpPointer stream = GetStream(MINIDUMP_STREAM_TYPE.SystemInfoStream);
		_info = stream.PtrToStructure<MINIDUMP_SYSTEM_INFO>();
		if (TryGetStream(MINIDUMP_STREAM_TYPE.Memory64ListStream, out stream))
		{
			_memoryChunks = new MinidumpMemoryChunks(stream, MINIDUMP_STREAM_TYPE.Memory64ListStream);
		}
		else
		{
			stream = GetStream(MINIDUMP_STREAM_TYPE.MemoryListStream);
			_memoryChunks = new MinidumpMemoryChunks(stream, MINIDUMP_STREAM_TYPE.MemoryListStream);
		}
		_mappedFileMemory = new LoadedFileMemoryLookups();
		IsMinidump = DumpNative.IsMiniDump(_view.BaseAddress);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern SafeWin32Handle CreateFileMapping(SafeFileHandle hFile, IntPtr lpFileMappingAttributes, PageProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern SafeMapViewHandle MapViewOfFile(SafeWin32Handle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);

	[DllImport("kernel32.dll")]
	internal static extern void RtlMoveMemory(IntPtr destination, IntPtr source, IntPtr numberBytes);

	public void Dispose()
	{
		_disposing = true;
		AcquireWriteLock();
		_info = null;
		_memoryChunks = null;
		_mappedFileMemory = null;
		if (_fileMapping != null)
		{
			_fileMapping.Close();
		}
		if (_view != null)
		{
			_view.Close();
		}
		if (_file != null)
		{
			_file.Dispose();
		}
	}

	private void EnsureValid()
	{
		if (_file == null)
		{
			throw new ObjectDisposedException("DumpReader");
		}
	}

	private DumpPointer GetStream(MINIDUMP_STREAM_TYPE type)
	{
		if (!TryGetStream(type, out var stream))
		{
			throw new ClrDiagnosticsException("Dump does not contain a " + type.ToString() + " stream.", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		return stream;
	}

	private bool TryGetStream(MINIDUMP_STREAM_TYPE type, out DumpPointer stream)
	{
		EnsureValid();
		if (!DumpNative.MiniDumpReadDumpStream(_view.BaseAddress, type, out var streamPointer, out var cbStreamSize) || IntPtr.Zero == streamPointer || cbStreamSize < 1)
		{
			stream = default(DumpPointer);
			return false;
		}
		stream = DumpPointer.DangerousMakeDumpPointer(streamPointer, cbStreamSize);
		return true;
	}

	public DumpThread GetThread(int threadId)
	{
		EnsureValid();
		MINIDUMP_THREAD rawThread = GetRawThread(threadId);
		if (rawThread == null)
		{
			return null;
		}
		return new DumpThread(this, rawThread);
	}

	private IMinidumpThreadList GetThreadList()
	{
		EnsureValid();
		try
		{
			MINIDUMP_STREAM_TYPE mINIDUMP_STREAM_TYPE = MINIDUMP_STREAM_TYPE.ThreadListStream;
			return new MINIDUMP_THREAD_LIST<MINIDUMP_THREAD>(GetStream(mINIDUMP_STREAM_TYPE), mINIDUMP_STREAM_TYPE);
		}
		catch (ClrDiagnosticsException)
		{
			MINIDUMP_STREAM_TYPE mINIDUMP_STREAM_TYPE = MINIDUMP_STREAM_TYPE.ThreadExListStream;
			return new MINIDUMP_THREAD_LIST<MINIDUMP_THREAD_EX>(GetStream(mINIDUMP_STREAM_TYPE), mINIDUMP_STREAM_TYPE);
		}
	}

	public IEnumerable<DumpThread> EnumerateThreads()
	{
		IMinidumpThreadList list = GetThreadList();
		uint num = list.Count();
		for (uint i = 0u; i < num; i++)
		{
			MINIDUMP_THREAD element = list.GetElement(i);
			yield return new DumpThread(this, element);
		}
	}

	private MINIDUMP_THREAD GetRawThread(int threadId)
	{
		IMinidumpThreadList threadList = GetThreadList();
		uint num = threadList.Count();
		for (uint num2 = 0u; num2 < num; num2++)
		{
			MINIDUMP_THREAD element = threadList.GetElement(num2);
			if (threadId == element.ThreadId)
			{
				return element;
			}
		}
		return null;
	}

	internal void GetThreadContext(MINIDUMP_LOCATION_DESCRIPTOR loc, IntPtr buffer, int sizeBufferBytes)
	{
		if (loc.IsNull)
		{
			throw new ClrDiagnosticsException("Context not present", ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		DumpPointer dumpPointer = TranslateDescriptor(loc);
		int dataSize = (int)loc.DataSize;
		if (sizeBufferBytes < dataSize)
		{
			throw new ClrDiagnosticsException("Context size mismatch. Expected = 0x" + sizeBufferBytes.ToString("x") + ", Size in dump = 0x" + dataSize.ToString("x"), ClrDiagnosticsExceptionKind.CrashDumpError);
		}
		dumpPointer.Copy(buffer, (uint)dataSize);
	}

	private MINIDUMP_MODULE_LIST GetModuleList()
	{
		EnsureValid();
		return new MINIDUMP_MODULE_LIST(GetStream(MINIDUMP_STREAM_TYPE.ModuleListStream));
	}

	private MINIDUMP_EXCEPTION_STREAM GetExceptionStream()
	{
		return new MINIDUMP_EXCEPTION_STREAM(GetStream(MINIDUMP_STREAM_TYPE.ExceptionStream));
	}

	public bool IsExceptionStream()
	{
		bool result = true;
		try
		{
			GetExceptionStream();
		}
		catch (ClrDiagnosticsException)
		{
			result = false;
		}
		return result;
	}

	public uint ExceptionStreamThreadId()
	{
		return GetExceptionStream().ThreadId;
	}

	public DumpModule LookupModule(string nameModule)
	{
		MINIDUMP_MODULE_LIST moduleList = GetModuleList();
		uint count = moduleList.Count;
		for (uint num = 0u; num < count; num++)
		{
			MINIDUMP_MODULE element = moduleList.GetElement(num);
			RVA moduleNameRva = element.ModuleNameRva;
			DumpPointer ptr = TranslateRVA(moduleNameRva);
			string @string = GetString(ptr);
			if (nameModule == @string || @string.EndsWith(nameModule))
			{
				return new DumpModule(this, element);
			}
		}
		return null;
	}

	public DumpModule TryLookupModuleByAddress(ulong targetAddress)
	{
		MINIDUMP_MODULE_LIST moduleList = GetModuleList();
		uint count = moduleList.Count;
		for (uint num = 0u; num < count; num++)
		{
			MINIDUMP_MODULE element = moduleList.GetElement(num);
			ulong baseOfImage = element.BaseOfImage;
			ulong num2 = baseOfImage + element.SizeOfImage;
			if (baseOfImage <= targetAddress && num2 > targetAddress)
			{
				return new DumpModule(this, element);
			}
		}
		return null;
	}

	public IEnumerable<DumpModule> EnumerateModules()
	{
		MINIDUMP_MODULE_LIST list = GetModuleList();
		uint num = list.Count;
		for (uint i = 0u; i < num; i++)
		{
			MINIDUMP_MODULE element = list.GetElement(i);
			yield return new DumpModule(this, element);
		}
	}
}
