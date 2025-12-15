using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

public abstract class DataTarget : IDisposable
{
	private SymbolLocator _symbolLocator = new SymbolLocator();

	public abstract IDataReader DataReader { get; }

	public SymbolLocator SymbolLocator => _symbolLocator;

	public abstract bool IsMinidump { get; }

	public abstract Architecture Architecture { get; }

	public abstract IList<ClrInfo> ClrVersions { get; }

	public abstract uint PointerSize { get; }

	public abstract IDebugClient DebuggerInterface { get; }

	public static DataTarget LoadCrashDump(string fileName)
	{
		DbgEngDataReader dbgEngDataReader = new DbgEngDataReader(fileName);
		return CreateFromReader(dbgEngDataReader, dbgEngDataReader.DebuggerInterface);
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
			dataReader = new LiveDataReader(pid);
		}
		else
		{
			client = ((DbgEngDataReader)(dataReader = new DbgEngDataReader(pid, attachFlag, msecTimeout))).DebuggerInterface;
		}
		return new DataTargetImpl(dataReader, client);
	}

	public abstract bool ReadProcessMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	public abstract ClrRuntime CreateRuntime(string dacFileName);

	public abstract ClrRuntime CreateRuntime(object clrDataProcess);

	public abstract IEnumerable<ModuleInfo> EnumerateModules();

	public abstract void Dispose();

	internal abstract string ResolveSymbol(ulong addr);
}
