using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Desktop;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime;

internal class DbgEngDataReader : IDisposable, IDataReader
{
	private static int s_totalInstanceCount = 0;

	private static bool s_needRelease = true;

	private IDebugClient _client;

	private IDebugDataSpaces _spaces;

	private IDebugDataSpaces2 _spaces2;

	private IDebugDataSpacesPtr _spacesPtr;

	private IDebugSymbols _symbols;

	private IDebugSymbols3 _symbols3;

	private IDebugControl2 _control;

	private IDebugAdvanced _advanced;

	private IDebugSystemObjects _systemObjects;

	private IDebugSystemObjects3 _systemObjects3;

	private uint _instance;

	private bool _disposed;

	private byte[] _ptrBuffer = new byte[IntPtr.Size];

	private List<ModuleInfo> _modules;

	private bool? _minidump;

	public bool IsMinidump
	{
		get
		{
			if (_minidump.HasValue)
			{
				return _minidump.Value;
			}
			SetClientInstance();
			_control.GetDebuggeeType(out var _, out var Qualifier);
			if (Qualifier == DEBUG_CLASS_QUALIFIER.KERNEL_SMALL_DUMP)
			{
				_control.GetDumpFormatFlags(out var FormatFlags);
				_minidump = (FormatFlags & DEBUG_FORMAT.USER_SMALL_FULL_MEMORY) == 0;
				return _minidump.Value;
			}
			_minidump = false;
			return false;
		}
	}

	internal IDebugClient DebuggerInterface => _client;

	public bool CanEnumerateModules => _symbols3 != null;

	public bool CanReadAsync => false;

	~DbgEngDataReader()
	{
		Dispose(disposing: false);
	}

	private void SetClientInstance()
	{
		if (_systemObjects3 != null && s_totalInstanceCount > 1)
		{
			_systemObjects3.SetCurrentSystemId(_instance);
		}
	}

	public DbgEngDataReader(string dumpFile)
	{
		if (!File.Exists(dumpFile))
		{
			throw new FileNotFoundException(dumpFile);
		}
		IDebugClient debugClient = CreateIDebugClient();
		int num = debugClient.OpenDumpFile(dumpFile);
		if (num != 0)
		{
			throw new ClrDiagnosticsException($"Could not load crash dump '{dumpFile}', HRESULT: 0x{num:x8}", ClrDiagnosticsException.HR.DebuggerError);
		}
		CreateClient(debugClient);
		_control.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
	}

	public DbgEngDataReader(IDebugClient client)
	{
		CreateClient(client);
		s_needRelease = false;
	}

	public DbgEngDataReader(int pid, AttachFlag flags, uint msecTimeout)
	{
		IDebugClient debugClient = CreateIDebugClient();
		CreateClient(debugClient);
		DEBUG_ATTACH attachFlags = ((flags != 0) ? DEBUG_ATTACH.LOCAL_KERNEL : DEBUG_ATTACH.KERNEL_CONNECTION);
		int num = _control.AddEngineOptions(DEBUG_ENGOPT.INITIAL_BREAK);
		if (num == 0)
		{
			num = debugClient.AttachProcess(0uL, (uint)pid, attachFlags);
		}
		if (num == 0)
		{
			num = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, msecTimeout);
		}
		switch (num)
		{
		case 1:
			throw new TimeoutException("Break in did not occur within the allotted timeout.");
		case -805306181:
			throw new InvalidOperationException("Mismatched architecture between this process and the target process.");
		default:
			throw new ClrDiagnosticsException($"Could not attach to pid {pid:X}, HRESULT: 0x{num:x8}", ClrDiagnosticsException.HR.DebuggerError);
		case 0:
			break;
		}
	}

	public Architecture GetArchitecture()
	{
		SetClientInstance();
		IMAGE_FILE_MACHINE Type;
		int executingProcessorType = _control.GetExecutingProcessorType(out Type);
		if (executingProcessorType != 0)
		{
			throw new ClrDiagnosticsException($"Failed to get proessor type, HRESULT: {executingProcessorType:x8}", ClrDiagnosticsException.HR.DebuggerError);
		}
		switch (Type)
		{
		case IMAGE_FILE_MACHINE.I386:
			return Architecture.X86;
		case IMAGE_FILE_MACHINE.AMD64:
			return Architecture.Amd64;
		case IMAGE_FILE_MACHINE.ARM:
		case IMAGE_FILE_MACHINE.THUMB:
		case IMAGE_FILE_MACHINE.THUMB2:
			return Architecture.Arm;
		default:
			return Architecture.Unknown;
		}
	}

	private static IDebugClient CreateIDebugClient()
	{
		Guid InterfaceId = new Guid("27fe5639-8407-4f47-8364-ee118fb08ac8");
		NativeMethods.DebugCreate(ref InterfaceId, out var Interface);
		return (IDebugClient)Interface;
	}

	public void Close()
	{
		Dispose();
	}

	public uint GetPointerSize()
	{
		SetClientInstance();
		int num = _control.IsPointer64Bit();
		return num switch
		{
			0 => 8u, 
			1 => 4u, 
			_ => throw new ClrDiagnosticsException($"IsPointer64Bit failed: {num:x8}", ClrDiagnosticsException.HR.DebuggerError), 
		};
	}

	public void Flush()
	{
		_modules = null;
	}

	public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		uint id = 0u;
		GetThreadIdBySystemId(threadID, out id);
		SetCurrentThreadId(id);
		GetThreadContext(context, contextSize);
		return true;
	}

	private void GetThreadContext(IntPtr context, uint contextSize)
	{
		SetClientInstance();
		_advanced.GetThreadContext(context, contextSize);
	}

	internal int ReadVirtual(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		SetClientInstance();
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (buffer.Length < bytesRequested)
		{
			bytesRequested = buffer.Length;
		}
		uint BytesRead = 0u;
		int result = _spaces.ReadVirtual(address, buffer, (uint)bytesRequested, out BytesRead);
		bytesRead = (int)BytesRead;
		return result;
	}

	private ulong[] GetImageBases()
	{
		List<ulong> list = null;
		if (GetNumberModules(out var count, out var unloadedCount) < 0)
		{
			return null;
		}
		list = new List<ulong>((int)count);
		for (uint num = 0u; num < count + unloadedCount; num++)
		{
			if (GetModuleByIndex(num, out var image) >= 0)
			{
				list.Add(image);
			}
		}
		return list.ToArray();
	}

	public IList<ModuleInfo> EnumerateModules()
	{
		if (_modules != null)
		{
			return _modules;
		}
		ulong[] imageBases = GetImageBases();
		DEBUG_MODULE_PARAMETERS[] array = new DEBUG_MODULE_PARAMETERS[imageBases.Length];
		List<ModuleInfo> list = new List<ModuleInfo>();
		if (imageBases != null && CanEnumerateModules && GetModuleParameters(imageBases.Length, imageBases, 0, array) >= 0)
		{
			for (int i = 0; i < imageBases.Length; i++)
			{
				ModuleInfo moduleInfo = new ModuleInfo(this);
				moduleInfo.TimeStamp = array[i].TimeDateStamp;
				moduleInfo.FileSize = array[i].Size;
				moduleInfo.ImageBase = imageBases[i];
				StringBuilder stringBuilder = new StringBuilder();
				if (GetModuleNameString(DEBUG_MODNAME.IMAGE, i, imageBases[i], null, 0u, out var NameSize) >= 0 && NameSize > 1)
				{
					stringBuilder.EnsureCapacity((int)NameSize);
					if (GetModuleNameString(DEBUG_MODNAME.IMAGE, i, imageBases[i], stringBuilder, NameSize, out NameSize) >= 0)
					{
						moduleInfo.FileName = stringBuilder.ToString();
					}
				}
				list.Add(moduleInfo);
			}
		}
		_modules = list;
		return list;
	}

	internal int GetModuleParameters(int count, ulong[] bases, int start, DEBUG_MODULE_PARAMETERS[] mods)
	{
		SetClientInstance();
		return _symbols.GetModuleParameters((uint)count, bases, (uint)start, mods);
	}

	private void CreateClient(IDebugClient client)
	{
		_client = client;
		_spaces = (IDebugDataSpaces)_client;
		_spacesPtr = (IDebugDataSpacesPtr)_client;
		_symbols = (IDebugSymbols)_client;
		_control = (IDebugControl2)_client;
		_spaces2 = _client as IDebugDataSpaces2;
		_symbols3 = _client as IDebugSymbols3;
		_advanced = _client as IDebugAdvanced;
		_systemObjects = _client as IDebugSystemObjects;
		_systemObjects3 = _client as IDebugSystemObjects3;
		Interlocked.Increment(ref s_totalInstanceCount);
		if (_systemObjects3 == null && s_totalInstanceCount > 1)
		{
			throw new ClrDiagnosticsException("This version of DbgEng is too old to create multiple instances of DataTarget.", ClrDiagnosticsException.HR.DebuggerError);
		}
		if (_systemObjects3 != null)
		{
			_systemObjects3.GetCurrentSystemId(out _instance);
		}
	}

	internal int GetModuleNameString(DEBUG_MODNAME Which, int Index, ulong Base, StringBuilder Buffer, uint BufferSize, out uint NameSize)
	{
		if (_symbols3 == null)
		{
			NameSize = 0u;
			return -1;
		}
		SetClientInstance();
		return _symbols3.GetModuleNameString(Which, (uint)Index, Base, Buffer, BufferSize, out NameSize);
	}

	internal int GetNumberModules(out uint count, out uint unloadedCount)
	{
		if (_symbols3 == null)
		{
			count = 0u;
			unloadedCount = 0u;
			return -1;
		}
		SetClientInstance();
		return _symbols3.GetNumberModules(out count, out unloadedCount);
	}

	internal int GetModuleByIndex(uint i, out ulong image)
	{
		if (_symbols3 == null)
		{
			image = 0uL;
			return -1;
		}
		SetClientInstance();
		return _symbols3.GetModuleByIndex(i, out image);
	}

	internal int GetNameByOffsetWide(ulong offset, StringBuilder sb, int p, out uint size, out ulong disp)
	{
		SetClientInstance();
		return _symbols3.GetNameByOffsetWide(offset, sb, p, out size, out disp);
	}

	public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
	{
		vq = default(VirtualQueryData);
		if (_spaces2 == null)
		{
			return false;
		}
		SetClientInstance();
		MEMORY_BASIC_INFORMATION64 Info;
		int num = _spaces2.QueryVirtual(addr, out Info);
		vq.BaseAddress = Info.BaseAddress;
		vq.Size = Info.RegionSize;
		return num == 0;
	}

	public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		return ReadVirtual(address, buffer, bytesRequested, out bytesRead) >= 0;
	}

	public unsafe ulong ReadPointerUnsafe(ulong addr)
	{
		if (ReadVirtual(addr, _ptrBuffer, IntPtr.Size, out var _) != 0)
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
		if (ReadVirtual(addr, _ptrBuffer, 4, out var _) != 0)
		{
			return 0u;
		}
		fixed (byte* ptrBuffer = _ptrBuffer)
		{
			return *(uint*)ptrBuffer;
		}
	}

	internal void SetSymbolPath(string path)
	{
		SetClientInstance();
		_symbols.SetSymbolPath(path);
		_control.Execute(DEBUG_OUTCTL.NOT_LOGGED, ".reload", DEBUG_EXECUTE.NOT_LOGGED);
	}

	internal int QueryVirtual(ulong addr, out MEMORY_BASIC_INFORMATION64 mem)
	{
		if (_spaces2 == null)
		{
			mem = default(MEMORY_BASIC_INFORMATION64);
			return -1;
		}
		SetClientInstance();
		return _spaces2.QueryVirtual(addr, out mem);
	}

	internal int GetModuleByModuleName(string image, int start, out uint index, out ulong baseAddress)
	{
		SetClientInstance();
		return _symbols.GetModuleByModuleName(image, (uint)start, out index, out baseAddress);
	}

	public void GetVersionInfo(ulong addr, out VersionInfo version)
	{
		version = default(VersionInfo);
		if (_symbols.GetModuleByOffset(addr, 0u, out var Index, out var Base) != 0)
		{
			return;
		}
		uint needed = 0u;
		if (GetModuleVersionInformation(Index, Base, "\\", null, 0u, out needed) == 0)
		{
			byte[] array = new byte[needed];
			if (GetModuleVersionInformation(Index, Base, "\\", array, needed, out needed) == 0)
			{
				version.Minor = (ushort)Marshal.ReadInt16(array, 8);
				version.Major = (ushort)Marshal.ReadInt16(array, 10);
				version.Patch = (ushort)Marshal.ReadInt16(array, 12);
				version.Revision = (ushort)Marshal.ReadInt16(array, 14);
			}
		}
	}

	internal int GetModuleVersionInformation(uint index, ulong baseAddress, string p, byte[] buffer, uint needed1, out uint needed2)
	{
		if (_symbols3 == null)
		{
			needed2 = 0u;
			return -1;
		}
		SetClientInstance();
		return _symbols3.GetModuleVersionInformation(index, baseAddress, "\\", buffer, needed1, out needed2);
	}

	internal int GetModuleNameString(DEBUG_MODNAME requestType, uint index, ulong baseAddress, StringBuilder sbpath, uint needed1, out uint needed2)
	{
		if (_symbols3 == null)
		{
			needed2 = 0u;
			return -1;
		}
		SetClientInstance();
		return _symbols3.GetModuleNameString(requestType, index, baseAddress, sbpath, needed1, out needed2);
	}

	internal int GetModuleParameters(uint Count, ulong[] Bases, uint Start, DEBUG_MODULE_PARAMETERS[] Params)
	{
		SetClientInstance();
		return _symbols.GetModuleParameters(Count, Bases, Start, Params);
	}

	internal void GetThreadIdBySystemId(uint threadID, out uint id)
	{
		SetClientInstance();
		_systemObjects.GetThreadIdBySystemId(threadID, out id);
	}

	internal void SetCurrentThreadId(uint id)
	{
		SetClientInstance();
		_systemObjects.SetCurrentThreadId(id);
	}

	internal void GetExecutingProcessorType(out IMAGE_FILE_MACHINE machineType)
	{
		SetClientInstance();
		_control.GetEffectiveProcessorType(out machineType);
	}

	public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		SetClientInstance();
		uint BytesRead;
		bool flag = _spacesPtr.ReadVirtual(address, buffer, (uint)bytesRequested, out BytesRead) >= 0;
		bytesRead = (int)(flag ? BytesRead : 0);
		return flag;
	}

	public int ReadVirtual(ulong address, byte[] buffer, uint bytesRequested, out uint bytesRead)
	{
		SetClientInstance();
		return _spaces.ReadVirtual(address, buffer, bytesRequested, out bytesRead);
	}

	public IEnumerable<uint> EnumerateAllThreads()
	{
		SetClientInstance();
		uint Number = 0u;
		if (_systemObjects.GetNumberThreads(out Number) == 0)
		{
			uint[] array = new uint[Number];
			if (_systemObjects.GetThreadIdsByIndex(0u, Number, null, array) == 0)
			{
				return array;
			}
		}
		return new uint[0];
	}

	public ulong GetThreadTeb(uint thread)
	{
		SetClientInstance();
		ulong Offset = 0uL;
		uint Id = 0u;
		bool num = _systemObjects.GetCurrentThreadId(out Id) == 0;
		if (_systemObjects.GetThreadIdBySystemId(thread, out Id) == 0 && _systemObjects.SetCurrentThreadId(Id) == 0)
		{
			_systemObjects.GetCurrentThreadTeb(out Offset);
		}
		if (num)
		{
			_systemObjects.SetCurrentThreadId(Id);
		}
		return Offset;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		int num = Interlocked.Decrement(ref s_totalInstanceCount);
		if (num == 0 && s_needRelease && disposing)
		{
			if (_systemObjects3 != null)
			{
				_systemObjects3.SetCurrentSystemId(_instance);
			}
			_client.EndSession(DEBUG_END.ACTIVE_DETACH);
			_client.DetachProcesses();
		}
		if (num == 0)
		{
			s_needRelease = true;
		}
	}

	public AsyncMemoryReadResult ReadMemoryAsync(ulong address, int bytesRequested)
	{
		throw new NotImplementedException();
	}

	public unsafe bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
	{
		uint id = 0u;
		GetThreadIdBySystemId(threadID, out id);
		SetCurrentThreadId(id);
		fixed (byte* value = &context[0])
		{
			GetThreadContext(new IntPtr(value), contextSize);
		}
		return true;
	}
}
