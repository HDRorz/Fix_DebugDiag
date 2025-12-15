using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Desktop;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

public abstract class DataTarget : IDisposable
{
	private SymbolLocator _symbolLocator;

	private FileLoader _fileLoader;

	public static PlatformFunctions PlatformFunctions { get; }

	public abstract uint ProcessId { get; }

	public abstract IDataReader DataReader { get; }

	public SymbolLocator SymbolLocator
	{
		get
		{
			if (_symbolLocator == null)
			{
				_symbolLocator = new DefaultSymbolLocator();
			}
			return _symbolLocator;
		}
		set
		{
			_symbolLocator = value;
		}
	}

	internal FileLoader FileLoader
	{
		get
		{
			if (_fileLoader == null)
			{
				_fileLoader = new FileLoader(this);
			}
			return _fileLoader;
		}
	}

	public abstract bool IsMinidump { get; }

	public abstract Architecture Architecture { get; }

	public abstract IList<ClrInfo> ClrVersions { get; }

	public abstract uint PointerSize { get; }

	public abstract IDebugClient DebuggerInterface { get; }

	static DataTarget()
	{
		PlatformFunctions = new WindowsFunctions();
	}

	public static DataTarget LoadCrashDump(string fileName)
	{
		DbgEngDataReader dbgEngDataReader = new DbgEngDataReader(fileName);
		return CreateFromReader(dbgEngDataReader, dbgEngDataReader.DebuggerInterface);
	}

	public static DataTarget LoadCoreDump(string filename)
	{
		return CreateFromReader(new CoreDumpReader(filename), null);
	}

	public static DataTarget LoadCrashDump(string fileName, CrashDumpReader dumpReader)
	{
		if (dumpReader == CrashDumpReader.DbgEng)
		{
			DbgEngDataReader dbgEngDataReader = new DbgEngDataReader(fileName);
			return CreateFromReader(dbgEngDataReader, dbgEngDataReader.DebuggerInterface);
		}
		return CreateFromReader(new DumpDataReader(fileName), null);
	}

	public static DataTarget CreateFromDataReader(IDataReader reader)
	{
		return CreateFromReader(reader, null);
	}

	private static DataTarget CreateFromReader(IDataReader reader, IDebugClient client)
	{
		return new DataTargetImpl(reader, client);
	}

	public static DataTarget CreateFromDebuggerInterface(IDebugClient client)
	{
		DbgEngDataReader dbgEngDataReader = new DbgEngDataReader(client);
		return new DataTargetImpl(dbgEngDataReader, dbgEngDataReader.DebuggerInterface);
	}

	public static DataTarget AttachToProcess(int pid, uint msecTimeout)
	{
		return AttachToProcess(pid, msecTimeout, AttachFlag.Invasive);
	}

	public static DataTarget AttachToProcess(int pid, uint msecTimeout, AttachFlag attachFlag)
	{
		IDebugClient client = null;
		IDataReader dataReader;
		if (attachFlag == AttachFlag.Passive)
		{
			dataReader = new LiveDataReader(pid, createSnapshot: false);
		}
		else
		{
			client = ((DbgEngDataReader)(dataReader = new DbgEngDataReader(pid, attachFlag, msecTimeout))).DebuggerInterface;
		}
		return new DataTargetImpl(dataReader, client);
	}

	public static DataTarget CreateSnapshotAndAttach(int pid)
	{
		return new DataTargetImpl(new LiveDataReader(pid, createSnapshot: true), null);
	}

	public abstract bool ReadProcessMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	public abstract IEnumerable<ModuleInfo> EnumerateModules();

	public abstract void Dispose();

	protected internal abstract void AddDacLibrary(DacLibrary dacLibrary);

	internal ClrRuntime CreateRuntime(ClrInfo clrInfo)
	{
		if (clrInfo == null)
		{
			throw new ArgumentNullException("clrInfo");
		}
		string text = clrInfo.LocalMatchingDac;
		if (text != null && !File.Exists(text))
		{
			text = null;
		}
		if (text == null)
		{
			text = SymbolLocator.FindBinary(clrInfo.DacInfo);
		}
		if (!File.Exists(text))
		{
			throw new FileNotFoundException("Could not find matching DAC for this runtime.", clrInfo.DacInfo.FileName);
		}
		if (IntPtr.Size != (int)DataReader.GetPointerSize())
		{
			throw new InvalidOperationException("Mismatched architecture between this process and the dac.");
		}
		return ConstructRuntime(clrInfo, text);
	}

	internal ClrRuntime CreateRuntime(ClrInfo clrInfo, object clrDataProcess)
	{
		if (clrInfo == null)
		{
			throw new ArgumentNullException("clrInfo");
		}
		if (clrDataProcess == null)
		{
			throw new ArgumentNullException("clrDataProcess");
		}
		DacLibrary dacLibrary = new DacLibrary(this, DacLibrary.TryGetDacPtr(clrDataProcess));
		if (dacLibrary.GetSOSInterfaceNoAddRef() != null)
		{
			return new V45Runtime(clrInfo, this, dacLibrary);
		}
		byte[] array = new byte[Marshal.SizeOf(typeof(V2HeapDetails))];
		if (dacLibrary.InternalDacPrivateInterface.Request(4026531885u, 0u, null, (uint)array.Length, array) == -2147024809)
		{
			return new LegacyRuntime(clrInfo, this, dacLibrary, DesktopVersion.v4, 10000);
		}
		return new LegacyRuntime(clrInfo, this, dacLibrary, DesktopVersion.v2, 3054);
	}

	internal ClrRuntime CreateRuntime(ClrInfo clrInfo, string dacFilename, bool ignoreMismatch = false)
	{
		if (clrInfo == null)
		{
			throw new ArgumentNullException("clrInfo");
		}
		if (string.IsNullOrEmpty(dacFilename))
		{
			throw new ArgumentNullException("dacFilename");
		}
		if (!File.Exists(dacFilename))
		{
			throw new FileNotFoundException(dacFilename);
		}
		if (!ignoreMismatch)
		{
			PlatformFunctions.GetFileVersion(dacFilename, out var major, out var minor, out var revision, out var patch);
			if (major != clrInfo.Version.Major || minor != clrInfo.Version.Minor || revision != clrInfo.Version.Revision || patch != clrInfo.Version.Patch)
			{
				throw new InvalidOperationException($"Mismatched dac. Version: {major}.{minor}.{revision}.{patch}");
			}
		}
		return ConstructRuntime(clrInfo, dacFilename);
	}

	private ClrRuntime ConstructRuntime(ClrInfo clrInfo, string dac)
	{
		if (IntPtr.Size != (int)DataReader.GetPointerSize())
		{
			throw new InvalidOperationException("Mismatched architecture between this process and the dac.");
		}
		if (IsMinidump)
		{
			SymbolLocator.PrefetchBinary(clrInfo.ModuleInfo.FileName, (int)clrInfo.ModuleInfo.TimeStamp, (int)clrInfo.ModuleInfo.FileSize);
		}
		DacLibrary lib = new DacLibrary(this, dac);
		if (clrInfo.Flavor == ClrFlavor.Core)
		{
			return new V45Runtime(clrInfo, this, lib);
		}
		DesktopVersion version;
		if (clrInfo.Version.Major == 2)
		{
			version = DesktopVersion.v2;
		}
		else
		{
			if (clrInfo.Version.Major != 4 || clrInfo.Version.Minor != 0 || clrInfo.Version.Patch >= 10000)
			{
				return new V45Runtime(clrInfo, this, lib);
			}
			version = DesktopVersion.v4;
		}
		return new LegacyRuntime(clrInfo, this, lib, version, clrInfo.Version.Patch);
	}
}
