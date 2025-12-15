using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class DesktopRuntimeBase : RuntimeBase
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	internal delegate void LoaderHeapTraverse(ulong address, IntPtr size, int isCurrent);

	protected CommonMethodTables _commonMTs;

	private Dictionary<ulong, DesktopModule> _modules = new Dictionary<ulong, DesktopModule>();

	private Dictionary<ulong, uint> _moduleSizes;

	private Dictionary<string, DesktopModule> _moduleFiles;

	private DesktopAppDomain _system;

	private DesktopAppDomain _shared;

	private List<ClrAppDomain> _domains;

	private List<ClrThread> _threads;

	private DesktopGCHeap _heap;

	private DesktopThreadPool _threadpool;

	internal int Revision { get; set; }

	internal abstract DesktopVersion CLRVersion { get; }

	public override int PointerSize => IntPtr.Size;

	public ulong ArrayMethodTable => _commonMTs.ArrayMethodTable;

	public override IList<ClrAppDomain> AppDomains
	{
		get
		{
			if (_domains == null)
			{
				InitDomains();
			}
			return _domains;
		}
	}

	public override IList<ClrThread> Threads
	{
		get
		{
			if (_threads == null)
			{
				InitThreads();
			}
			return _threads;
		}
	}

	public ulong ExceptionMethodTable => _commonMTs.ExceptionMethodTable;

	public ulong ObjectMethodTable => _commonMTs.ObjectMethodTable;

	public ulong StringMethodTable => _commonMTs.StringMethodTable;

	public ulong FreeMethodTable => _commonMTs.FreeMethodTable;

	public ulong SystemDomainAddress
	{
		get
		{
			if (_domains == null)
			{
				InitDomains();
			}
			if (_system == null)
			{
				return 0uL;
			}
			return _system.Address;
		}
	}

	public ulong SharedDomainAddress
	{
		get
		{
			if (_domains == null)
			{
				InitDomains();
			}
			if (_shared == null)
			{
				return 0uL;
			}
			return _shared.Address;
		}
	}

	public override IEnumerable<int> EnumerateGCThreads()
	{
		foreach (uint item in _dataReader.EnumerateAllThreads())
		{
			ulong threadTeb = _dataReader.GetThreadTeb(item);
			if ((DesktopThread.GetTlsSlotForThread(this, threadTeb) & 1) == 1)
			{
				yield return (int)item;
			}
		}
	}

	internal DesktopGCHeap TryGetHeap()
	{
		return _heap;
	}

	internal DesktopModule GetModule(ulong module)
	{
		if (module == 0L)
		{
			return null;
		}
		if (_modules.TryGetValue(module, out var value))
		{
			return value;
		}
		IModuleData moduleData = GetModuleData(module);
		if (moduleData == null)
		{
			return null;
		}
		string pEFileName = GetPEFileName(moduleData.PEFile);
		string assemblyName = GetAssemblyName(moduleData.Assembly);
		if (_moduleSizes == null)
		{
			_moduleSizes = new Dictionary<ulong, uint>();
			foreach (ModuleInfo item in _dataReader.EnumerateModules())
			{
				_moduleSizes[item.ImageBase] = item.FileSize;
			}
		}
		if (_moduleFiles == null)
		{
			_moduleFiles = new Dictionary<string, DesktopModule>();
		}
		uint value2 = 0u;
		_moduleSizes.TryGetValue(moduleData.ImageBase, out value2);
		if (pEFileName == null)
		{
			value = new DesktopModule(this, module, moduleData, pEFileName, assemblyName, value2);
		}
		else if (!_moduleFiles.TryGetValue(pEFileName, out value))
		{
			value = new DesktopModule(this, module, moduleData, pEFileName, assemblyName, value2);
			_moduleFiles[pEFileName] = value;
		}
		_modules[module] = value;
		return value;
	}

	public override CcwData GetCcwDataFromAddress(ulong addr)
	{
		ICCWData cCWData = GetCCWData(addr);
		if (cCWData == null)
		{
			return null;
		}
		return new DesktopCCWData((DesktopGCHeap)GetHeap(), addr, cCWData);
	}

	private void InitThreads()
	{
		if (_threads == null)
		{
			IThreadStoreData threadStoreData = GetThreadStoreData();
			ulong num = 18446744073709551614uL;
			if (threadStoreData != null)
			{
				num = threadStoreData.Finalizer;
			}
			List<ClrThread> list = new List<ClrThread>();
			int num2 = 4098;
			ulong num3 = GetFirstThread();
			IThreadData thread = GetThread(num3);
			while (num2-- > 0 && thread != null)
			{
				IThreadData thread2 = thread;
				ulong num4 = num3;
				list.Add(new DesktopThread(this, thread2, num4, num4 == num));
				num3 = thread.Next;
				thread = GetThread(num3);
			}
			_threads = list;
		}
	}

	public override ClrHeap GetHeap(TextWriter diagnosticLog)
	{
		if (_heap == null)
		{
			_heap = new DesktopGCHeap(this, diagnosticLog);
		}
		return _heap;
	}

	public override ClrHeap GetHeap()
	{
		if (_heap == null)
		{
			_heap = new DesktopGCHeap(this, null);
		}
		return _heap;
	}

	public override ClrThreadPool GetThreadPool()
	{
		if (_threadpool != null)
		{
			return _threadpool;
		}
		IThreadPoolData threadPoolData = GetThreadPoolData();
		if (threadPoolData == null)
		{
			return null;
		}
		_threadpool = new DesktopThreadPool(this, threadPoolData);
		return _threadpool;
	}

	public override IEnumerable<ClrMemoryRegion> EnumerateMemoryRegions()
	{
		IHeapDetails[] heaps;
		if (!ServerGC)
		{
			heaps = new IHeapDetails[1] { GetWksHeapDetails() };
		}
		else
		{
			heaps = new IHeapDetails[HeapCount];
			int num = 0;
			ulong[] serverHeapList = GetServerHeapList();
			if (serverHeapList != null)
			{
				ulong[] array = serverHeapList;
				foreach (ulong addr in array)
				{
					heaps[num++] = GetSvrHeapDetails(addr);
					if (num == heaps.Length)
					{
						break;
					}
				}
			}
			else
			{
				heaps = new IHeapDetails[0];
			}
		}
		int max = 2048;
		int i2 = 0;
		while (i2 < heaps.Length)
		{
			ISegmentData segment = GetSegmentData(heaps[i2].FirstHeapSegment);
			while (segment != null && max-- > 0)
			{
				GCSegmentType type = ((segment.Address != heaps[i2].EphemeralSegment) ? GCSegmentType.Regular : GCSegmentType.Ephemeral);
				yield return new MemoryRegion(this, segment.Start, segment.Committed - segment.Start, ClrMemoryRegionType.GCSegment, (uint)i2, type);
				yield return new MemoryRegion(this, segment.Committed, segment.Reserved - segment.Committed, ClrMemoryRegionType.ReservedGCSegment, (uint)i2, type);
				segment = ((segment.Address != segment.Next && segment.Address != 0L) ? GetSegmentData(segment.Next) : null);
			}
			segment = GetSegmentData(heaps[i2].FirstLargeHeapSegment);
			while (segment != null && max-- > 0)
			{
				yield return new MemoryRegion(this, segment.Start, segment.Committed - segment.Start, ClrMemoryRegionType.GCSegment, (uint)i2, GCSegmentType.LargeObject);
				yield return new MemoryRegion(this, segment.Committed, segment.Reserved - segment.Committed, ClrMemoryRegionType.ReservedGCSegment, (uint)i2, GCSegmentType.LargeObject);
				segment = ((segment.Address != segment.Next && segment.Address != 0L) ? GetSegmentData(segment.Next) : null);
			}
			int i = i2 + 1;
			i2 = i;
		}
		HashSet<ulong> regions = new HashSet<ulong>();
		foreach (ClrHandle item in EnumerateHandles())
		{
			if (_dataReader.VirtualQuery(item.Address, out var vq) && !regions.Contains(vq.BaseAddress))
			{
				regions.Add(vq.BaseAddress);
				yield return new MemoryRegion(this, vq.BaseAddress, vq.Size, ClrMemoryRegionType.HandleTableChunk, item.AppDomain);
			}
		}
		AppDomainHeapWalker adhw = new AppDomainHeapWalker(this);
		IAppDomainData ad = GetAppDomainData(SystemDomainAddress);
		foreach (MemoryRegion item2 in adhw.EnumerateHeaps(ad))
		{
			yield return item2;
		}
		foreach (ulong item3 in EnumerateModules(ad))
		{
			foreach (MemoryRegion item4 in adhw.EnumerateModuleHeaps(ad, item3))
			{
				yield return item4;
			}
		}
		ad = GetAppDomainData(SharedDomainAddress);
		foreach (MemoryRegion item5 in adhw.EnumerateHeaps(ad))
		{
			yield return item5;
		}
		foreach (ulong item6 in EnumerateModules(ad))
		{
			foreach (MemoryRegion item7 in adhw.EnumerateModuleHeaps(ad, item6))
			{
				yield return item7;
			}
		}
		IAppDomainStoreData appDomainStoreData = GetAppDomainStoreData();
		if (appDomainStoreData != null)
		{
			IList<ulong> appDomainList = GetAppDomainList(appDomainStoreData.Count);
			if (appDomainList != null)
			{
				foreach (ulong item8 in appDomainList)
				{
					ad = GetAppDomainData(item8);
					foreach (MemoryRegion item9 in adhw.EnumerateHeaps(ad))
					{
						yield return item9;
					}
					foreach (ulong item10 in EnumerateModules(ad))
					{
						foreach (MemoryRegion item11 in adhw.EnumerateModuleHeaps(ad, item10))
						{
							yield return item11;
						}
					}
				}
			}
		}
		regions.Clear();
		foreach (ICodeHeap jitHeap in EnumerateJitHeaps())
		{
			if (jitHeap.Type == CodeHeapType.Host)
			{
				if (_dataReader.VirtualQuery(jitHeap.Address, out var vq2))
				{
					yield return new MemoryRegion(this, vq2.BaseAddress, vq2.Size, ClrMemoryRegionType.JitHostCodeHeap);
				}
				else
				{
					yield return new MemoryRegion(this, jitHeap.Address, 0uL, ClrMemoryRegionType.JitHostCodeHeap);
				}
			}
			else
			{
				if (jitHeap.Type != 0)
				{
					continue;
				}
				foreach (MemoryRegion item12 in adhw.EnumerateJitHeap(jitHeap.Address))
				{
					yield return item12;
				}
			}
		}
	}

	internal override ClrAppDomain GetAppDomainByAddress(ulong address)
	{
		foreach (ClrAppDomain appDomain in AppDomains)
		{
			if (appDomain.Address == address)
			{
				return appDomain;
			}
		}
		return null;
	}

	public override void Flush()
	{
		OnRuntimeFlushed();
		Revision++;
		_dacInterface.Flush();
		_modules.Clear();
		_moduleFiles = null;
		_moduleSizes = null;
		_domains = null;
		_system = null;
		_shared = null;
		_threads = null;
		base.MemoryReader = null;
		_heap = null;
		_threadpool = null;
	}

	public override ClrMethod GetMethodByAddress(ulong ip)
	{
		IMethodDescData mDForIP = GetMDForIP(ip);
		if (mDForIP == null)
		{
			return null;
		}
		return DesktopMethod.Create(this, mDForIP);
	}

	internal IEnumerable<IRWLockData> EnumerateLockData(ulong thread)
	{
		thread += GetRWLockDataOffset();
		if (!ReadPointer(thread, out var firstEntry))
		{
			yield break;
		}
		ulong lockEntry = firstEntry;
		byte[] output = GetByteArrayForStruct<RWLockData>();
		int bytesRead;
		while (ReadMemory(lockEntry, output, output.Length, out bytesRead) && bytesRead == output.Length)
		{
			IRWLockData result = ConvertStruct<IRWLockData, RWLockData>(output);
			if (result != null)
			{
				yield return result;
			}
			if (result.Next != lockEntry)
			{
				lockEntry = result.Next;
				if (lockEntry == firstEntry)
				{
					break;
				}
				continue;
			}
			break;
		}
	}

	internal uint GetExceptionMessageOffset()
	{
		if (PointerSize == 8)
		{
			return 32u;
		}
		return 16u;
	}

	internal uint GetStackTraceOffset()
	{
		if (PointerSize == 8)
		{
			return 64u;
		}
		return 32u;
	}

	internal ClrThread GetThreadFromThinlockID(uint threadId)
	{
		ulong threadFromThinlock = GetThreadFromThinlock(threadId);
		if (threadFromThinlock == 0L)
		{
			return null;
		}
		if (_threads == null)
		{
			InitThreads();
		}
		foreach (ClrThread thread in _threads)
		{
			if (thread.Address == threadFromThinlock)
			{
				return thread;
			}
		}
		return null;
	}

	public override IEnumerable<ClrModule> EnumerateModules()
	{
		if (_domains == null)
		{
			InitDomains();
		}
		foreach (DesktopModule value in _modules.Values)
		{
			yield return value;
		}
	}

	internal IEnumerable<ulong> EnumerateModules(IAppDomainData appDomain)
	{
		if (appDomain == null)
		{
			yield break;
		}
		ulong[] assemblyList = GetAssemblyList(appDomain.Address, appDomain.AssemblyCount);
		if (assemblyList == null)
		{
			yield break;
		}
		ulong[] array = assemblyList;
		foreach (ulong assembly in array)
		{
			IAssemblyData assemblyData = GetAssemblyData(appDomain.Address, assembly);
			if (assemblyData == null)
			{
				continue;
			}
			ulong[] moduleList = GetModuleList(assembly, assemblyData.ModuleCount);
			if (moduleList != null)
			{
				ulong[] array2 = moduleList;
				for (int j = 0; j < array2.Length; j++)
				{
					yield return array2[j];
				}
			}
		}
	}

	internal DesktopRuntimeBase(DataTargetImpl dt, DacLibrary lib)
		: base(dt, lib)
	{
	}

	internal void InitDomains()
	{
		if (_domains != null)
		{
			return;
		}
		_modules.Clear();
		_domains = new List<ClrAppDomain>();
		IAppDomainStoreData appDomainStoreData = GetAppDomainStoreData();
		if (appDomainStoreData == null)
		{
			return;
		}
		foreach (ulong appDomain in GetAppDomainList(appDomainStoreData.Count))
		{
			DesktopAppDomain desktopAppDomain = InitDomain(appDomain);
			if (desktopAppDomain != null)
			{
				_domains.Add(desktopAppDomain);
			}
		}
		_system = InitDomain(appDomainStoreData.SystemDomain);
		_shared = InitDomain(appDomainStoreData.SharedDomain);
		_moduleFiles = null;
		_moduleSizes = null;
	}

	private DesktopAppDomain InitDomain(ulong domain)
	{
		_ = new ulong[1];
		IAppDomainData appDomainData = GetAppDomainData(domain);
		if (appDomainData == null)
		{
			return null;
		}
		DesktopAppDomain desktopAppDomain = new DesktopAppDomain(this, appDomainData, GetAppDomaminName(domain));
		if (appDomainData.AssemblyCount > 0)
		{
			ulong[] assemblyList = GetAssemblyList(domain, appDomainData.AssemblyCount);
			foreach (ulong assembly in assemblyList)
			{
				IAssemblyData assemblyData = GetAssemblyData(domain, assembly);
				if (assemblyData == null || assemblyData.ModuleCount <= 0)
				{
					continue;
				}
				ulong[] moduleList = GetModuleList(assembly, assemblyData.ModuleCount);
				foreach (ulong num in moduleList)
				{
					DesktopModule module = GetModule(num);
					if (module != null)
					{
						module.AddMapping(desktopAppDomain, num);
						desktopAppDomain.AddModule(module);
					}
				}
			}
		}
		return desktopAppDomain;
	}

	private IEnumerable<DesktopModule> EnumerateImages()
	{
		InitDomains();
		foreach (DesktopModule value in _modules.Values)
		{
			if (value.ImageBase != 0L)
			{
				yield return value;
			}
		}
	}

	private IEnumerable<ulong> EnumerateImageBases(IEnumerable<DesktopModule> modules)
	{
		foreach (DesktopModule module in modules)
		{
			yield return module.ImageBase;
		}
	}

	internal string GetTypeName(TypeHandle id)
	{
		if (id.MethodTable == FreeMethodTable)
		{
			return "Free";
		}
		if (id.MethodTable == ArrayMethodTable && id.ComponentMethodTable != 0L)
		{
			string nameForMT = GetNameForMT(id.ComponentMethodTable);
			if (nameForMT != null)
			{
				return nameForMT + "[]";
			}
		}
		return GetNameForMT(id.MethodTable);
	}

	protected IXCLRDataProcess GetClrDataProcess()
	{
		return _dacInterface;
	}

	internal override IEnumerable<ClrStackFrame> EnumerateStackFrames(uint osThreadId)
	{
		int taskByOSThreadID = GetClrDataProcess().GetTaskByOSThreadID(osThreadId, out var task);
		if (taskByOSThreadID < 0)
		{
			yield break;
		}
		taskByOSThreadID = ((IXCLRDataTask)task).CreateStackWalk(15u, out task);
		if (taskByOSThreadID < 0)
		{
			yield break;
		}
		IXCLRDataStackWalk stackWalk = (IXCLRDataStackWalk)task;
		byte[] ulongBuffer = new byte[8];
		byte[] context = new byte[(PointerSize == 4) ? 716 : 1232];
		_ = new byte[256];
		int ip_offset = 184;
		int sp_offset = 196;
		if (PointerSize == 8)
		{
			ip_offset = 248;
			sp_offset = 152;
		}
		do
		{
			taskByOSThreadID = stackWalk.GetContext(65599u, (uint)context.Length, out var _, context);
			if (taskByOSThreadID < 0 || taskByOSThreadID == 1)
			{
				break;
			}
			ulong ip;
			ulong num;
			if (PointerSize == 4)
			{
				ip = BitConverter.ToUInt32(context, ip_offset);
				num = BitConverter.ToUInt32(context, sp_offset);
			}
			else
			{
				ip = BitConverter.ToUInt64(context, ip_offset);
				num = BitConverter.ToUInt64(context, sp_offset);
			}
			taskByOSThreadID = stackWalk.Request(4026531840u, 0u, null, (uint)ulongBuffer.Length, ulongBuffer);
			ulong value = 0uL;
			if (taskByOSThreadID >= 0)
			{
				value = BitConverter.ToUInt64(ulongBuffer, 0);
				if (value != 0L)
				{
					num = value;
					ReadPointer(num, out value);
				}
			}
			yield return GetStackFrame(taskByOSThreadID, ip, num, value);
		}
		while (stackWalk.Next() == 0);
	}

	internal ILToNativeMap[] GetILMap(ulong ip)
	{
		ILToNativeMap[] array = null;
		if (_dacInterface.StartEnumMethodInstancesByAddress(ip, null, out var handle) < 0)
		{
			return null;
		}
		if (_dacInterface.EnumMethodInstanceByAddress(ref handle, out var method) == 0)
		{
			IXCLRDataMethodInstance iXCLRDataMethodInstance = (IXCLRDataMethodInstance)method;
			uint mapNeeded = 0u;
			if (iXCLRDataMethodInstance.GetILAddressMap(0u, out mapNeeded, null) == 0)
			{
				array = new ILToNativeMap[mapNeeded];
				if (iXCLRDataMethodInstance.GetILAddressMap(mapNeeded, out mapNeeded, array) != 0)
				{
					array = null;
				}
			}
			_dacInterface.EndEnumMethodInstancesByAddress(handle);
		}
		return array;
	}

	internal abstract uint GetExceptionHROffset();

	internal abstract IList<ulong> GetAppDomainList(int count);

	internal abstract ulong[] GetAssemblyList(ulong appDomain, int count);

	internal abstract ulong[] GetModuleList(ulong assembly, int count);

	internal abstract IAssemblyData GetAssemblyData(ulong domain, ulong assembly);

	internal abstract IAppDomainStoreData GetAppDomainStoreData();

	internal abstract bool GetCommonMethodTables(ref CommonMethodTables mCommonMTs);

	internal abstract string GetNameForMT(ulong mt);

	internal abstract string GetPEFileName(ulong addr);

	internal abstract IModuleData GetModuleData(ulong addr);

	internal abstract IAppDomainData GetAppDomainData(ulong addr);

	internal abstract string GetAppDomaminName(ulong addr);

	internal abstract bool TraverseHeap(ulong heap, LoaderHeapTraverse callback);

	internal abstract bool TraverseStubHeap(ulong appDomain, int type, LoaderHeapTraverse callback);

	internal abstract IEnumerable<ICodeHeap> EnumerateJitHeaps();

	internal abstract ulong GetModuleForMT(ulong mt);

	internal abstract IFieldInfo GetFieldInfo(ulong mt);

	internal abstract IFieldData GetFieldData(ulong fieldDesc);

	internal abstract IMetadata GetMetadataImport(ulong module);

	internal abstract IObjectData GetObjectData(ulong objRef);

	internal abstract IList<ulong> GetMethodTableList(ulong module);

	internal abstract IDomainLocalModuleData GetDomainLocalModule(ulong appDomain, ulong id);

	internal abstract ICCWData GetCCWData(ulong ccw);

	internal abstract IRCWData GetRCWData(ulong rcw);

	internal abstract COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count);

	internal abstract COMInterfacePointerData[] GetRCWInterfaces(ulong rcw, int count);

	internal abstract ulong GetThreadStaticPointer(ulong thread, ClrElementType type, uint offset, uint moduleId, bool shared);

	internal abstract IDomainLocalModuleData GetDomainLocalModule(ulong module);

	internal abstract IList<ulong> GetMethodDescList(ulong methodTable);

	internal abstract string GetNameForMD(ulong md);

	internal abstract IMethodDescData GetMethodDescData(ulong md);

	internal abstract uint GetMetadataToken(ulong mt);

	protected abstract DesktopStackFrame GetStackFrame(int res, ulong ip, ulong sp, ulong frameVtbl);

	internal abstract IList<ClrStackFrame> GetExceptionStackTrace(ulong obj, ClrType type);

	internal abstract string GetAssemblyName(ulong assembly);

	internal abstract string GetAppBase(ulong appDomain);

	internal abstract string GetConfigFile(ulong appDomain);

	internal abstract IMethodDescData GetMDForIP(ulong ip);

	protected abstract ulong GetThreadFromThinlock(uint threadId);

	internal abstract int GetSyncblkCount();

	internal abstract ISyncBlkData GetSyncblkData(int index);

	internal abstract IThreadPoolData GetThreadPoolData();

	protected abstract uint GetRWLockDataOffset();

	internal abstract IEnumerable<NativeWorkItem> EnumerateWorkItems();

	internal abstract uint GetStringFirstCharOffset();

	internal abstract uint GetStringLengthOffset();
}
