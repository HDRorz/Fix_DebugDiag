using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrRuntime
{
	public delegate void RuntimeFlushedCallback(ClrRuntime runtime);

	public DacLibrary DacLibrary { get; protected set; }

	public ClrInfo ClrInfo { get; protected set; }

	public abstract DataTarget DataTarget { get; }

	public bool ServerGC { get; protected set; }

	public int HeapCount { get; protected set; }

	public abstract int PointerSize { get; }

	public abstract IList<ClrAppDomain> AppDomains { get; }

	public abstract ClrAppDomain SystemDomain { get; }

	public abstract ClrAppDomain SharedDomain { get; }

	public abstract IList<ClrThread> Threads { get; }

	public abstract ClrHeap Heap { get; }

	public virtual ClrThreadPool ThreadPool
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public abstract IList<ClrModule> Modules { get; }

	internal bool HasArrayComponentMethodTables
	{
		get
		{
			if (ClrInfo.Flavor == ClrFlavor.Desktop)
			{
				VersionInfo version = ClrInfo.Version;
				if (version.Major > 4)
				{
					return false;
				}
				if (version.Major == 4 && version.Minor >= 6)
				{
					return false;
				}
			}
			else if (ClrInfo.Flavor == ClrFlavor.Core)
			{
				return false;
			}
			return true;
		}
	}

	public event RuntimeFlushedCallback RuntimeFlushed;

	public abstract IEnumerable<ClrException> EnumerateSerializedExceptions();

	public abstract IEnumerable<int> EnumerateGCThreads();

	public abstract IEnumerable<ulong> EnumerateFinalizerQueueObjectAddresses();

	public abstract ClrMethod GetMethodByHandle(ulong methodHandle);

	public abstract CcwData GetCcwDataByAddress(ulong addr);

	public abstract bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	public abstract bool ReadPointer(ulong address, out ulong value);

	public abstract IEnumerable<ClrHandle> EnumerateHandles();

	public abstract IEnumerable<ClrMemoryRegion> EnumerateMemoryRegions();

	public abstract ClrMethod GetMethodByAddress(ulong ip);

	public abstract void Flush();

	public abstract string GetJitHelperFunctionName(ulong address);

	public abstract string GetMethodTableName(ulong address);

	protected void OnRuntimeFlushed()
	{
		this.RuntimeFlushed?.Invoke(this);
	}
}
