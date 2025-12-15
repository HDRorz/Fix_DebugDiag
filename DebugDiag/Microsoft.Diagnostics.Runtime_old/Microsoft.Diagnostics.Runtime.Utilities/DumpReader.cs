using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class DumpReader : IDisposable
{
	protected internal static class DumpNative
	{
		public enum MINIDUMP_STREAM_TYPE
		{
			UnusedStream = 0,
			ReservedStream0 = 1,
			ReservedStream1 = 2,
			ThreadListStream = 3,
			ModuleListStream = 4,
			MemoryListStream = 5,
			ExceptionStream = 6,
			SystemInfoStream = 7,
			ThreadExListStream = 8,
			Memory64ListStream = 9,
			CommentStreamA = 10,
			CommentStreamW = 11,
			HandleDataStream = 12,
			FunctionTableStream = 13,
			UnloadedModuleListStream = 14,
			MiscInfoStream = 15,
			MemoryInfoListStream = 16,
			ThreadInfoListStream = 17,
			LastReservedStream = 65535
		}

		private struct MINIDUMP_HEADER
		{
			public uint Singature;

			public uint Version;

			public uint NumberOfStreams;

			public uint StreamDirectoryRva;

			public uint CheckSum;

			public uint TimeDateStamp;

			public ulong Flags;
		}

		private struct MINIDUMP_DIRECTORY
		{
			public MINIDUMP_STREAM_TYPE StreamType;

			public uint DataSize;

			public uint Rva;
		}

		public struct RVA
		{
			public uint Value;

			public bool IsNull => Value == 0;
		}

		public struct RVA64
		{
			public ulong Value;
		}

		public struct MINIDUMP_LOCATION_DESCRIPTOR
		{
			public uint DataSize;

			public RVA Rva;

			public bool IsNull
			{
				get
				{
					if (DataSize != 0)
					{
						return Rva.IsNull;
					}
					return true;
				}
			}
		}

		public struct MINIDUMP_LOCATION_DESCRIPTOR64
		{
			public ulong DataSize;

			public RVA64 Rva;
		}

		public struct MINIDUMP_MEMORY_DESCRIPTOR
		{
			public const int SizeOf = 16;

			private ulong _startofmemoryrange;

			public MINIDUMP_LOCATION_DESCRIPTOR Memory;

			public ulong StartOfMemoryRange => ZeroExtendAddress(_startofmemoryrange);
		}

		public struct MINIDUMP_MEMORY_DESCRIPTOR64
		{
			public const int SizeOf = 16;

			private ulong _startofmemoryrange;

			public ulong DataSize;

			public ulong StartOfMemoryRange => ZeroExtendAddress(_startofmemoryrange);
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MINIDUMP_EXCEPTION
		{
			public uint ExceptionCode;

			public uint ExceptionFlags;

			public ulong ExceptionRecord;

			private ulong _exceptionaddress;

			public uint NumberParameters;

			public uint __unusedAlignment;

			public ulong[] ExceptionInformation;

			public ulong ExceptionAddress
			{
				get
				{
					return ZeroExtendAddress(_exceptionaddress);
				}
				set
				{
					_exceptionaddress = value;
				}
			}

			public MINIDUMP_EXCEPTION()
			{
				ExceptionInformation = new ulong[15];
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MINIDUMP_EXCEPTION_STREAM
		{
			public uint ThreadId;

			public uint __alignment;

			public MINIDUMP_EXCEPTION ExceptionRecord;

			public MINIDUMP_LOCATION_DESCRIPTOR ThreadContext;

			public MINIDUMP_EXCEPTION_STREAM(DumpPointer dump)
			{
				uint offset = 0u;
				ThreadId = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				__alignment = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				ExceptionRecord = new MINIDUMP_EXCEPTION();
				ExceptionRecord.ExceptionCode = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				ExceptionRecord.ExceptionFlags = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				ExceptionRecord.ExceptionRecord = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
				ExceptionRecord.ExceptionAddress = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
				ExceptionRecord.NumberParameters = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				ExceptionRecord.__unusedAlignment = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				if ((long)ExceptionRecord.ExceptionInformation.Length != 15)
				{
					throw new ClrDiagnosticsException("Crash dump error: Expected to find " + 15u + " exception params, but found " + ExceptionRecord.ExceptionInformation.Length + " instead.", ClrDiagnosticsException.HR.CrashDumpError);
				}
				for (int i = 0; (long)i < 15L; i++)
				{
					ExceptionRecord.ExceptionInformation[i] = dump.PtrToStructureAdjustOffset<ulong>(ref offset);
				}
				ThreadContext.DataSize = dump.PtrToStructureAdjustOffset<uint>(ref offset);
				ThreadContext.Rva.Value = dump.PtrToStructureAdjustOffset<uint>(ref offset);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MINIDUMP_SYSTEM_INFO
		{
			public ProcessorArchitecture ProcessorArchitecture;

			public ushort ProcessorLevel;

			public ushort ProcessorRevision;

			public byte NumberOfProcessors;

			public byte ProductType;

			public uint MajorVersion;

			public uint MinorVersion;

			public uint BuildNumber;

			public PlatformID PlatformId;

			public RVA CSDVersionRva;

			public Version Version => new Version((int)MajorVersion, (int)MinorVersion, (int)BuildNumber);
		}

		internal struct VS_FIXEDFILEINFO
		{
			public uint dwSignature;

			public uint dwStrucVersion;

			public uint dwFileVersionMS;

			public uint dwFileVersionLS;

			public uint dwProductVersionMS;

			public uint dwProductVersionLS;

			public uint dwFileFlagsMask;

			public uint dwFileFlags;

			public uint dwFileOS;

			public uint dwFileType;

			public uint dwFileSubtype;

			public uint dwFileDateMS;

			public uint dwFileDateLS;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public sealed class MINIDUMP_MODULE
		{
			private ulong _baseofimage;

			public uint SizeOfImage;

			public uint CheckSum;

			public uint TimeDateStamp;

			public RVA ModuleNameRva;

			internal VS_FIXEDFILEINFO VersionInfo;

			private MINIDUMP_LOCATION_DESCRIPTOR _cvRecord;

			private MINIDUMP_LOCATION_DESCRIPTOR _miscRecord;

			private ulong _reserved0;

			private ulong _reserved1;

			public ulong BaseOfImage => ZeroExtendAddress(_baseofimage);

			public DateTime Timestamp => DateTime.FromFileTimeUtc(10000000L * (long)TimeDateStamp + 116444736000000000L);
		}

		public class MINIDUMP_MODULE_LIST : MinidumpArray<MINIDUMP_MODULE>
		{
			internal MINIDUMP_MODULE_LIST(DumpPointer streamPointer)
				: base(streamPointer, MINIDUMP_STREAM_TYPE.ModuleListStream)
			{
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public class MINIDUMP_THREAD
		{
			public uint ThreadId;

			public uint SuspendCount;

			public uint PriorityClass;

			public uint Priority;

			private ulong _teb;

			public MINIDUMP_MEMORY_DESCRIPTOR Stack;

			public MINIDUMP_LOCATION_DESCRIPTOR ThreadContext;

			public ulong Teb => ZeroExtendAddress(_teb);

			public virtual MINIDUMP_MEMORY_DESCRIPTOR BackingStore
			{
				get
				{
					throw new MissingMemberException("MINIDUMP_THREAD has no backing store!");
				}
				set
				{
					throw new MissingMemberException("MINIDUMP_THREAD has no backing store!");
				}
			}

			public virtual bool HasBackingStore()
			{
				return false;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public sealed class MINIDUMP_THREAD_EX : MINIDUMP_THREAD
		{
			public override MINIDUMP_MEMORY_DESCRIPTOR BackingStore { get; set; }

			public override bool HasBackingStore()
			{
				return true;
			}
		}

		public class MinidumpArray<T>
		{
			private DumpPointer _streamPointer;

			public uint Count => _streamPointer.ReadUInt32();

			protected MinidumpArray(DumpPointer streamPointer, MINIDUMP_STREAM_TYPE streamType)
			{
				if (streamType != MINIDUMP_STREAM_TYPE.ModuleListStream && streamType != MINIDUMP_STREAM_TYPE.ThreadListStream && streamType != MINIDUMP_STREAM_TYPE.ThreadExListStream)
				{
					throw new ClrDiagnosticsException("MinidumpArray does not support this stream type.", ClrDiagnosticsException.HR.CrashDumpError);
				}
				_streamPointer = streamPointer;
			}

			public T GetElement(uint idx)
			{
				if (idx > Count)
				{
					throw new ClrDiagnosticsException("Dump error: index " + idx + "is out of range.", ClrDiagnosticsException.HR.CrashDumpError);
				}
				uint offset = (uint)(4 + (int)idx * Marshal.SizeOf(typeof(T)));
				return _streamPointer.PtrToStructure<T>(offset);
			}
		}

		public interface IMinidumpThreadList
		{
			uint Count();

			MINIDUMP_THREAD GetElement(uint idx);
		}

		public class MINIDUMP_THREAD_LIST<T> : MinidumpArray<T>, IMinidumpThreadList where T : MINIDUMP_THREAD
		{
			internal MINIDUMP_THREAD_LIST(DumpPointer streamPointer, MINIDUMP_STREAM_TYPE streamType)
				: base(streamPointer, streamType)
			{
				if (streamType != MINIDUMP_STREAM_TYPE.ThreadListStream && streamType != MINIDUMP_STREAM_TYPE.ThreadExListStream)
				{
					throw new ClrDiagnosticsException("Only ThreadListStream and ThreadExListStream are supported.", ClrDiagnosticsException.HR.CrashDumpError);
				}
			}

			public new MINIDUMP_THREAD GetElement(uint idx)
			{
				return base.GetElement(idx);
			}

			public new uint Count()
			{
				return base.Count;
			}
		}

		public class MinidumpMemoryChunk : IComparable<MinidumpMemoryChunk>
		{
			public ulong Size;

			public ulong TargetStartAddress;

			public ulong TargetEndAddress;

			public ulong RVA;

			public int CompareTo(MinidumpMemoryChunk other)
			{
				return TargetStartAddress.CompareTo(other.TargetStartAddress);
			}
		}

		public class MinidumpMemoryChunks
		{
			private ulong _count;

			private MinidumpMemory64List _memory64List;

			private MinidumpMemoryList _memoryList;

			private MinidumpMemoryChunk[] _chunks;

			private DumpPointer _dumpStream;

			private MINIDUMP_STREAM_TYPE _listType;

			public ulong Count => _count;

			public ulong Size(ulong i)
			{
				return _chunks[i].Size;
			}

			public ulong RVA(ulong i)
			{
				return _chunks[i].RVA;
			}

			public ulong StartAddress(ulong i)
			{
				return _chunks[i].TargetStartAddress;
			}

			public ulong EndAddress(ulong i)
			{
				return _chunks[i].TargetEndAddress;
			}

			public int GetChunkContainingAddress(ulong address)
			{
				MinidumpMemoryChunk minidumpMemoryChunk = new MinidumpMemoryChunk();
				minidumpMemoryChunk.TargetStartAddress = address;
				int num = Array.BinarySearch(_chunks, minidumpMemoryChunk);
				if (num >= 0)
				{
					return num;
				}
				if (~num != 0)
				{
					int num2 = Math.Min(_chunks.Length, ~num) - 1;
					if (_chunks[num2].TargetStartAddress <= address && _chunks[num2].TargetEndAddress > address)
					{
						return num2;
					}
				}
				return -1;
			}

			public MinidumpMemoryChunks(DumpPointer rawStream, MINIDUMP_STREAM_TYPE type)
			{
				_count = 0uL;
				_memory64List = null;
				_memoryList = null;
				_listType = MINIDUMP_STREAM_TYPE.UnusedStream;
				if (type != MINIDUMP_STREAM_TYPE.MemoryListStream && type != MINIDUMP_STREAM_TYPE.Memory64ListStream)
				{
					throw new ClrDiagnosticsException("Type must be either MemoryListStream or Memory64ListStream", ClrDiagnosticsException.HR.CrashDumpError);
				}
				_listType = type;
				_dumpStream = rawStream;
				if (MINIDUMP_STREAM_TYPE.Memory64ListStream == type)
				{
					InitFromMemory64List();
				}
				else
				{
					InitFromMemoryList();
				}
			}

			private void InitFromMemory64List()
			{
				_memory64List = new MinidumpMemory64List(_dumpStream);
				RVA64 baseRva = _memory64List.BaseRva;
				ulong count = _memory64List.Count;
				List<MinidumpMemoryChunk> list = new List<MinidumpMemoryChunk>();
				for (ulong num = 0uL; num < count; num++)
				{
					MINIDUMP_MEMORY_DESCRIPTOR64 element = _memory64List.GetElement((uint)num);
					MinidumpMemoryChunk minidumpMemoryChunk = new MinidumpMemoryChunk();
					minidumpMemoryChunk.Size = element.DataSize;
					minidumpMemoryChunk.TargetStartAddress = element.StartOfMemoryRange;
					minidumpMemoryChunk.TargetEndAddress = element.StartOfMemoryRange + element.DataSize;
					minidumpMemoryChunk.RVA = baseRva.Value;
					baseRva.Value += element.DataSize;
					list.Add(minidumpMemoryChunk);
				}
				list.Sort();
				SplitAndMergeChunks(list);
				_chunks = list.ToArray();
				_count = (ulong)list.Count;
				ValidateChunks();
			}

			public void InitFromMemoryList()
			{
				_memoryList = new MinidumpMemoryList(_dumpStream);
				uint count = _memoryList.Count;
				List<MinidumpMemoryChunk> list = new List<MinidumpMemoryChunk>();
				for (ulong num = 0uL; num < count; num++)
				{
					MinidumpMemoryChunk minidumpMemoryChunk = new MinidumpMemoryChunk();
					MINIDUMP_MEMORY_DESCRIPTOR element = _memoryList.GetElement((uint)num);
					minidumpMemoryChunk.Size = element.Memory.DataSize;
					minidumpMemoryChunk.TargetStartAddress = element.StartOfMemoryRange;
					minidumpMemoryChunk.TargetEndAddress = element.StartOfMemoryRange + element.Memory.DataSize;
					minidumpMemoryChunk.RVA = element.Memory.Rva.Value;
					list.Add(minidumpMemoryChunk);
				}
				list.Sort();
				SplitAndMergeChunks(list);
				_chunks = list.ToArray();
				_count = (ulong)list.Count;
				ValidateChunks();
			}

			private void SplitAndMergeChunks(List<MinidumpMemoryChunk> chunks)
			{
				for (int i = 1; i < chunks.Count; i++)
				{
					MinidumpMemoryChunk minidumpMemoryChunk = chunks[i - 1];
					MinidumpMemoryChunk minidumpMemoryChunk2 = chunks[i];
					if (minidumpMemoryChunk.TargetEndAddress <= minidumpMemoryChunk2.TargetStartAddress)
					{
						continue;
					}
					if (minidumpMemoryChunk.TargetEndAddress >= minidumpMemoryChunk2.TargetEndAddress)
					{
						chunks.RemoveAt(i);
						i--;
						continue;
					}
					ulong num = minidumpMemoryChunk.TargetEndAddress - minidumpMemoryChunk2.TargetStartAddress;
					minidumpMemoryChunk2.TargetStartAddress += num;
					minidumpMemoryChunk2.RVA += num;
					minidumpMemoryChunk2.Size -= num;
					int j;
					for (j = i; j < chunks.Count - 1 && minidumpMemoryChunk2.TargetStartAddress > chunks[j + 1].TargetStartAddress; j++)
					{
					}
					if (j != i)
					{
						chunks.RemoveAt(i);
						chunks.Insert(j - 1, minidumpMemoryChunk2);
						i--;
					}
				}
			}

			private void ValidateChunks()
			{
				for (ulong num = 0uL; num < _count; num++)
				{
					if (_chunks[num].Size != _chunks[num].TargetEndAddress - _chunks[num].TargetStartAddress || _chunks[num].TargetStartAddress > _chunks[num].TargetEndAddress)
					{
						throw new ClrDiagnosticsException("Unexpected inconsistency error in dump memory chunk " + num + " with target base address " + _chunks[num].TargetStartAddress + ".", ClrDiagnosticsException.HR.CrashDumpError);
					}
					if (num < _count - 1 && _listType == MINIDUMP_STREAM_TYPE.Memory64ListStream && (_chunks[num].RVA >= _chunks[num + 1].RVA || _chunks[num].TargetEndAddress > _chunks[num + 1].TargetStartAddress))
					{
						throw new ClrDiagnosticsException("Unexpected relative addresses inconsistency between dump memory chunks " + num + " and " + (num + 1) + ".", ClrDiagnosticsException.HR.CrashDumpError);
					}
					if (num < _count - 1 && _chunks[num].TargetEndAddress > _chunks[num + 1].TargetStartAddress)
					{
						throw new ClrDiagnosticsException("Unexpected overlap between memory chunks", ClrDiagnosticsException.HR.CrashDumpError);
					}
				}
			}
		}

		public class MinidumpMemory64List
		{
			private DumpPointer _streamPointer;

			public ulong Count => (ulong)_streamPointer.ReadInt64();

			public RVA64 BaseRva => _streamPointer.PtrToStructure<RVA64>(8u);

			public MinidumpMemory64List(DumpPointer streamPointer)
			{
				_streamPointer = streamPointer;
			}

			public MINIDUMP_MEMORY_DESCRIPTOR64 GetElement(uint idx)
			{
				uint offset = 16 + idx * 16;
				return _streamPointer.PtrToStructure<MINIDUMP_MEMORY_DESCRIPTOR64>(offset);
			}
		}

		public class MinidumpMemoryList
		{
			private DumpPointer _streamPointer;

			public uint Count => (uint)_streamPointer.ReadInt32();

			public MinidumpMemoryList(DumpPointer streamPointer)
			{
				_streamPointer = streamPointer;
			}

			public MINIDUMP_MEMORY_DESCRIPTOR GetElement(uint idx)
			{
				uint offset = 4 + idx * 16;
				return _streamPointer.PtrToStructure<MINIDUMP_MEMORY_DESCRIPTOR>(offset);
			}
		}

		public class LoadedFileMemoryLookups
		{
			private Dictionary<string, SafeLoadLibraryHandle> _files;

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
					handle = NativeMethods.LoadLibraryEx(fileName, 0, NativeMethods.LoadLibraryFlags.DontResolveDllReferences);
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

		private const uint MINIDUMP_SIGNATURE = 1347241037u;

		private const uint MINIDUMP_VERSION = 42899u;

		private const uint MiniDumpWithFullMemoryInfo = 2u;

		public const uint EXCEPTION_MAXIMUM_PARAMETERS = 15u;

		private static ulong ZeroExtendAddress(ulong addr)
		{
			if (IntPtr.Size == 4)
			{
				return addr &= 0xFFFFFFFFu;
			}
			return addr;
		}

		public static bool IsMiniDump(IntPtr pbase)
		{
			return (((MINIDUMP_HEADER)Marshal.PtrToStructure(pbase, typeof(MINIDUMP_HEADER))).Flags & 2) == 0;
		}

		public static bool MiniDumpReadDumpStream(IntPtr pBase, MINIDUMP_STREAM_TYPE type, out IntPtr streamPointer, out uint cbStreamSize)
		{
			MINIDUMP_HEADER mINIDUMP_HEADER = (MINIDUMP_HEADER)Marshal.PtrToStructure(pBase, typeof(MINIDUMP_HEADER));
			streamPointer = IntPtr.Zero;
			cbStreamSize = 0u;
			if (mINIDUMP_HEADER.Singature != 1347241037 || (mINIDUMP_HEADER.Version & 0xFFFF) != 42899)
			{
				return false;
			}
			int num = Marshal.SizeOf(typeof(MINIDUMP_DIRECTORY));
			long num2 = pBase.ToInt64() + (int)mINIDUMP_HEADER.StreamDirectoryRva;
			for (int i = 0; i < (int)mINIDUMP_HEADER.NumberOfStreams; i++)
			{
				MINIDUMP_DIRECTORY mINIDUMP_DIRECTORY = (MINIDUMP_DIRECTORY)Marshal.PtrToStructure(new IntPtr(num2 + i * num), typeof(MINIDUMP_DIRECTORY));
				if (mINIDUMP_DIRECTORY.StreamType == type)
				{
					streamPointer = new IntPtr(pBase.ToInt64() + (int)mINIDUMP_DIRECTORY.Rva);
					cbStreamSize = mINIDUMP_DIRECTORY.DataSize;
					return true;
				}
			}
			return false;
		}
	}

	private volatile bool _disposing;

	private volatile int _lock;

	protected DumpNative.MinidumpMemoryChunks _memoryChunks;

	protected DumpNative.LoadedFileMemoryLookups _mappedFileMemory;

	private FileStream _file;

	private SafeWin32Handle _fileMapping;

	private SafeMapViewHandle _view;

	private DumpPointer _base;

	private DumpNative.MINIDUMP_SYSTEM_INFO _info;

	public bool IsMinidump { get; set; }

	public Version Version => _info.Version;

	public OperatingSystem OSVersion
	{
		get
		{
			PlatformID platformId = _info.PlatformId;
			Version version = Version;
			return new OperatingSystem(platformId, version);
		}
	}

	public string OSVersionString
	{
		get
		{
			EnsureValid();
			string @string = GetString(_info.CSDVersionRva);
			return OSVersion.ToString() + " " + @string;
		}
	}

	public ProcessorArchitecture ProcessorArchitecture
	{
		get
		{
			EnsureValid();
			return _info.ProcessorArchitecture;
		}
	}

	protected internal DumpPointer TranslateDescriptor(DumpNative.MINIDUMP_LOCATION_DESCRIPTOR location)
	{
		DumpPointer result = TranslateRVA(location.Rva);
		result.Shrink(location.DataSize);
		return result;
	}

	protected internal DumpPointer TranslateRVA(ulong rva)
	{
		return _base.Adjust(rva);
	}

	protected internal DumpPointer TranslateRVA(DumpNative.RVA rva)
	{
		return _base.Adjust(rva.Value);
	}

	protected internal DumpPointer TranslateRVA(DumpNative.RVA64 rva)
	{
		return _base.Adjust(rva.Value);
	}

	protected internal string GetString(DumpNative.RVA rva)
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
			throw new ClrDiagnosticsException(string.Format(CultureInfo.CurrentUICulture, "Memory missing at {0}. Could only read {1} bytes of {2} total bytes requested.", targetRequestStart.ToString("x"), num, destinationBufferSizeInBytes), ClrDiagnosticsException.HR.CrashDumpError);
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

	public virtual void ReadMemory(AsyncMemoryReadResult result)
	{
		EnsureValid();
		byte[] array = new byte[result.BytesRequested];
		bool flag = false;
		try
		{
			flag = AcquireReadLock();
			if (flag)
			{
				result.BytesRead = ReadPartialMemory(result.Address, array, array.Length);
			}
			else
			{
				result.BytesRead = 0;
			}
		}
		finally
		{
			if (flag)
			{
				ReleaseReadLock();
			}
		}
		result.Result = array;
		result.Complete.Set();
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
		_fileMapping = NativeMethods.CreateFileMapping(_file.SafeFileHandle, IntPtr.Zero, NativeMethods.PageProtection.Readonly, 0u, 0u, null);
		if (_fileMapping.IsInvalid)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
		}
		_view = NativeMethods.MapViewOfFile(_fileMapping, 4u, 0u, 0u, IntPtr.Zero);
		if (_view.IsInvalid)
		{
			Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
		}
		_base = DumpPointer.DangerousMakeDumpPointer(_view.BaseAddress, (uint)length);
		DumpPointer stream = GetStream(DumpNative.MINIDUMP_STREAM_TYPE.SystemInfoStream);
		_info = stream.PtrToStructure<DumpNative.MINIDUMP_SYSTEM_INFO>();
		if (TryGetStream(DumpNative.MINIDUMP_STREAM_TYPE.Memory64ListStream, out stream))
		{
			_memoryChunks = new DumpNative.MinidumpMemoryChunks(stream, DumpNative.MINIDUMP_STREAM_TYPE.Memory64ListStream);
		}
		else
		{
			stream = GetStream(DumpNative.MINIDUMP_STREAM_TYPE.MemoryListStream);
			_memoryChunks = new DumpNative.MinidumpMemoryChunks(stream, DumpNative.MINIDUMP_STREAM_TYPE.MemoryListStream);
		}
		_mappedFileMemory = new DumpNative.LoadedFileMemoryLookups();
		IsMinidump = DumpNative.IsMiniDump(_view.BaseAddress);
	}

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

	private DumpPointer GetStream(DumpNative.MINIDUMP_STREAM_TYPE type)
	{
		if (!TryGetStream(type, out var stream))
		{
			throw new ClrDiagnosticsException(string.Concat("Dump does not contain a ", type, " stream."), ClrDiagnosticsException.HR.CrashDumpError);
		}
		return stream;
	}

	private bool TryGetStream(DumpNative.MINIDUMP_STREAM_TYPE type, out DumpPointer stream)
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
		DumpNative.MINIDUMP_THREAD rawThread = GetRawThread(threadId);
		if (rawThread == null)
		{
			return null;
		}
		return new DumpThread(this, rawThread);
	}

	private DumpNative.IMinidumpThreadList GetThreadList()
	{
		EnsureValid();
		try
		{
			DumpNative.MINIDUMP_STREAM_TYPE mINIDUMP_STREAM_TYPE = DumpNative.MINIDUMP_STREAM_TYPE.ThreadListStream;
			return new DumpNative.MINIDUMP_THREAD_LIST<DumpNative.MINIDUMP_THREAD>(GetStream(mINIDUMP_STREAM_TYPE), mINIDUMP_STREAM_TYPE);
		}
		catch (ClrDiagnosticsException)
		{
			DumpNative.MINIDUMP_STREAM_TYPE mINIDUMP_STREAM_TYPE = DumpNative.MINIDUMP_STREAM_TYPE.ThreadExListStream;
			return new DumpNative.MINIDUMP_THREAD_LIST<DumpNative.MINIDUMP_THREAD_EX>(GetStream(mINIDUMP_STREAM_TYPE), mINIDUMP_STREAM_TYPE);
		}
	}

	public IEnumerable<DumpThread> EnumerateThreads()
	{
		DumpNative.IMinidumpThreadList list = GetThreadList();
		uint num = list.Count();
		for (uint i = 0u; i < num; i++)
		{
			DumpNative.MINIDUMP_THREAD element = list.GetElement(i);
			yield return new DumpThread(this, element);
		}
	}

	private DumpNative.MINIDUMP_THREAD GetRawThread(int threadId)
	{
		DumpNative.IMinidumpThreadList threadList = GetThreadList();
		uint num = threadList.Count();
		for (uint num2 = 0u; num2 < num; num2++)
		{
			DumpNative.MINIDUMP_THREAD element = threadList.GetElement(num2);
			if (threadId == element.ThreadId)
			{
				return element;
			}
		}
		return null;
	}

	internal void GetThreadContext(DumpNative.MINIDUMP_LOCATION_DESCRIPTOR loc, IntPtr buffer, int sizeBufferBytes)
	{
		if (loc.IsNull)
		{
			throw new ClrDiagnosticsException("Context not present", ClrDiagnosticsException.HR.CrashDumpError);
		}
		DumpPointer dumpPointer = TranslateDescriptor(loc);
		int dataSize = (int)loc.DataSize;
		if (sizeBufferBytes < dataSize)
		{
			throw new ClrDiagnosticsException("Context size mismatch. Expected = 0x" + sizeBufferBytes.ToString("x") + ", Size in dump = 0x" + dataSize.ToString("x"), ClrDiagnosticsException.HR.CrashDumpError);
		}
		dumpPointer.Copy(buffer, (uint)dataSize);
	}

	private DumpNative.MINIDUMP_MODULE_LIST GetModuleList()
	{
		EnsureValid();
		return new DumpNative.MINIDUMP_MODULE_LIST(GetStream(DumpNative.MINIDUMP_STREAM_TYPE.ModuleListStream));
	}

	private DumpNative.MINIDUMP_EXCEPTION_STREAM GetExceptionStream()
	{
		return new DumpNative.MINIDUMP_EXCEPTION_STREAM(GetStream(DumpNative.MINIDUMP_STREAM_TYPE.ExceptionStream));
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
		DumpNative.MINIDUMP_MODULE_LIST moduleList = GetModuleList();
		uint count = moduleList.Count;
		for (uint num = 0u; num < count; num++)
		{
			DumpNative.MINIDUMP_MODULE element = moduleList.GetElement(num);
			DumpNative.RVA moduleNameRva = element.ModuleNameRva;
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
		DumpNative.MINIDUMP_MODULE_LIST moduleList = GetModuleList();
		uint count = moduleList.Count;
		for (uint num = 0u; num < count; num++)
		{
			DumpNative.MINIDUMP_MODULE element = moduleList.GetElement(num);
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
		DumpNative.MINIDUMP_MODULE_LIST list = GetModuleList();
		uint num = list.Count;
		for (uint i = 0u; i < num; i++)
		{
			DumpNative.MINIDUMP_MODULE element = list.GetElement(i);
			yield return new DumpModule(this, element);
		}
	}
}
