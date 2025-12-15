using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class DesktopRuntimeBase : RuntimeBase
{
	private class DomainContainer
	{
		public readonly DesktopAppDomain System;

		public readonly DesktopAppDomain Shared;

		public readonly IList<ClrAppDomain> Domains;

		public static readonly DomainContainer Empty = new DomainContainer(null, null, new ClrAppDomain[0]);

		public DomainContainer(DesktopAppDomain system, DesktopAppDomain shared, IList<ClrAppDomain> domains)
		{
			System = system;
			Shared = shared;
			Domains = domains ?? throw new ArgumentNullException("domains");
		}
	}

	protected CommonMethodTables _commonMTs;

	private Dictionary<uint, ICorDebugThread> _corDebugThreads;

	private ClrModule[] _moduleList;

	private Lazy<List<ClrThread>> _threads;

	private Lazy<DesktopGCHeap> _heap;

	private Lazy<DesktopThreadPool> _threadpool;

	private ErrorModule _errorModule;

	private Lazy<DomainContainer> _appDomains;

	private readonly Lazy<Dictionary<ulong, uint>> _moduleSizes;

	private Dictionary<ulong, DesktopModule> _modules = new Dictionary<ulong, DesktopModule>();

	private Dictionary<string, DesktopModule> _moduleFiles = new Dictionary<string, DesktopModule>();

	private Lazy<ClrModule> _mscorlib;

	internal int Revision { get; set; }

	public ErrorModule ErrorModule
	{
		get
		{
			if (_errorModule == null)
			{
				_errorModule = new ErrorModule(this);
			}
			return _errorModule;
		}
	}

	internal abstract DesktopVersion CLRVersion { get; }

	public override int PointerSize => IntPtr.Size;

	public ulong ArrayMethodTable => _commonMTs.ArrayMethodTable;

	public override IList<ClrThread> Threads => _threads.Value;

	public ulong ExceptionMethodTable => _commonMTs.ExceptionMethodTable;

	public ulong ObjectMethodTable => _commonMTs.ObjectMethodTable;

	public ulong StringMethodTable => _commonMTs.StringMethodTable;

	public ulong FreeMethodTable => _commonMTs.FreeMethodTable;

	public override ClrHeap Heap => _heap.Value;

	public override ClrThreadPool ThreadPool => _threadpool.Value;

	public override ClrAppDomain SystemDomain => _appDomains.Value.System;

	public override ClrAppDomain SharedDomain => _appDomains.Value.Shared;

	public override IList<ClrAppDomain> AppDomains => _appDomains.Value.Domains;

	public bool IsSingleDomain => _appDomains.Value.Domains.Count == 1;

	public override IList<ClrModule> Modules
	{
		get
		{
			if (_moduleList == null)
			{
				if (!_appDomains.IsValueCreated)
				{
					_ = _appDomains.Value;
				}
				_moduleList = UniqueModules(_modules.Values).ToArray();
			}
			return _moduleList;
		}
	}

	public ClrModule Mscorlib => _mscorlib.Value;

	internal DesktopRuntimeBase(ClrInfo info, DataTarget dt, DacLibrary lib)
		: base(info, dt, lib)
	{
		_heap = new Lazy<DesktopGCHeap>(CreateHeap);
		_threads = new Lazy<List<ClrThread>>(CreateThreadList);
		_appDomains = new Lazy<DomainContainer>(CreateAppDomainList);
		_threadpool = new Lazy<DesktopThreadPool>(CreateThreadPoolData);
		_moduleSizes = new Lazy<Dictionary<ulong, uint>>(() => _dataReader.EnumerateModules().ToDictionary((ModuleInfo module) => module.ImageBase, (ModuleInfo module) => module.FileSize));
		_mscorlib = new Lazy<ClrModule>(GetMscorlib);
	}

	public override void Flush()
	{
		OnRuntimeFlushed();
		Revision++;
		_dacInterface.Flush();
		_dataTarget.DataReader.Flush();
		base.MemoryReader = null;
		_moduleList = null;
		_modules = new Dictionary<ulong, DesktopModule>();
		_moduleFiles = new Dictionary<string, DesktopModule>();
		_threads = new Lazy<List<ClrThread>>(CreateThreadList);
		_appDomains = new Lazy<DomainContainer>(CreateAppDomainList);
		_heap = new Lazy<DesktopGCHeap>(CreateHeap);
		_threadpool = new Lazy<DesktopThreadPool>(CreateThreadPoolData);
		_mscorlib = new Lazy<ClrModule>(GetMscorlib);
	}

	internal ulong GetModuleSize(ulong address)
	{
		_moduleSizes.Value.TryGetValue(address, out var value);
		return value;
	}

	internal override IGCInfo GetGCInfo()
	{
		return GetGCInfoImpl() ?? throw new ClrDiagnosticsException("This runtime is not initialized and contains no data.", ClrDiagnosticsExceptionKind.RuntimeUninitialized);
	}

	public override IEnumerable<ClrException> EnumerateSerializedExceptions()
	{
		return new ClrException[0];
	}

	public override IEnumerable<int> EnumerateGCThreads()
	{
		foreach (uint item in _dataReader.EnumerateAllThreads())
		{
			ulong threadTeb = _dataReader.GetThreadTeb(item);
			if ((ThreadBase.GetTlsSlotForThread(this, threadTeb) & 1) == 1)
			{
				yield return (int)item;
			}
		}
	}

	internal abstract IGCInfo GetGCInfoImpl();

	public override CcwData GetCcwDataByAddress(ulong addr)
	{
		ICCWData cCWData = GetCCWData(addr);
		if (cCWData == null)
		{
			return null;
		}
		return new DesktopCCWData(_heap.Value, addr, cCWData);
	}

	internal ICorDebugThread GetCorDebugThread(uint osid)
	{
		if (_corDebugThreads == null)
		{
			_corDebugThreads = new Dictionary<uint, ICorDebugThread>();
			ICorDebugProcess corDebugProcess = base.CorDebugProcess;
			if (corDebugProcess == null)
			{
				return null;
			}
			corDebugProcess.EnumerateThreads(out var ppThreads);
			ICorDebugThread[] array = new ICorDebugThread[1];
			uint pceltFetched;
			while (ppThreads.Next(1u, array, out pceltFetched) == 0 && pceltFetched == 1)
			{
				try
				{
					array[0].GetID(out var pdwThreadId);
					_corDebugThreads[pdwThreadId] = array[0];
				}
				catch
				{
				}
			}
		}
		_corDebugThreads.TryGetValue(osid, out var value);
		return value;
	}

	private List<ClrThread> CreateThreadList()
	{
		IThreadStoreData threadStoreData = GetThreadStoreData();
		ulong num = 18446744073709551614uL;
		if (threadStoreData != null)
		{
			num = threadStoreData.Finalizer;
		}
		List<ClrThread> list = new List<ClrThread>();
		ulong num2 = GetFirstThread();
		IThreadData thread = GetThread(num2);
		HashSet<ulong> hashSet = new HashSet<ulong> { num2 };
		while (thread != null)
		{
			list.Add(new DesktopThread(this, thread, num2, num2 == num));
			num2 = thread.Next;
			if (hashSet.Contains(num2) || num2 == 0L)
			{
				break;
			}
			hashSet.Add(num2);
			thread = GetThread(num2);
		}
		return list;
	}

	private DesktopGCHeap CreateHeap()
	{
		if (base.HasArrayComponentMethodTables)
		{
			return new LegacyGCHeap(this);
		}
		return new V46GCHeap(this);
	}

	public override ClrMethod GetMethodByHandle(ulong methodHandle)
	{
		if (methodHandle == 0L)
		{
			return null;
		}
		IMethodDescData methodDescData = GetMethodDescData(methodHandle);
		if (methodDescData == null)
		{
			return null;
		}
		return Heap.GetTypeByMethodTable(methodDescData.MethodTable)?.GetMethod(methodDescData.MDToken);
	}

	public override IEnumerable<ClrMemoryRegion> EnumerateMemoryRegions()
	{
		IHeapDetails[] heaps;
		if (!base.ServerGC)
		{
			heaps = new IHeapDetails[1] { GetWksHeapDetails() };
		}
		else
		{
			heaps = new IHeapDetails[base.HeapCount];
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
		HashSet<ulong> addresses = new HashSet<ulong>();
		int i2 = 0;
		while (i2 < heaps.Length)
		{
			for (ISegmentData segment = GetSegmentData(heaps[i2].FirstHeapSegment); segment != null; segment = GetSegmentData(segment.Next))
			{
				GCSegmentType type = ((segment.Address != heaps[i2].EphemeralSegment) ? GCSegmentType.Regular : GCSegmentType.Ephemeral);
				yield return new MemoryRegion(this, segment.Start, segment.Committed - segment.Start, ClrMemoryRegionType.GCSegment, (uint)i2, type);
				if (segment.Committed <= segment.Reserved)
				{
					yield return new MemoryRegion(this, segment.Committed, segment.Reserved - segment.Committed, ClrMemoryRegionType.ReservedGCSegment, (uint)i2, type);
				}
				if (segment.Address == segment.Next || segment.Address == 0L || !addresses.Add(segment.Next))
				{
					break;
				}
			}
			for (ISegmentData segment = GetSegmentData(heaps[i2].FirstLargeHeapSegment); segment != null; segment = GetSegmentData(segment.Next))
			{
				yield return new MemoryRegion(this, segment.Start, segment.Committed - segment.Start, ClrMemoryRegionType.GCSegment, (uint)i2, GCSegmentType.LargeObject);
				if (segment.Committed <= segment.Reserved)
				{
					yield return new MemoryRegion(this, segment.Committed, segment.Reserved - segment.Committed, ClrMemoryRegionType.ReservedGCSegment, (uint)i2, GCSegmentType.LargeObject);
				}
				if (segment.Address == segment.Next || segment.Address == 0L || !addresses.Add(segment.Next))
				{
					break;
				}
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
		if (SystemDomain != null)
		{
			IAppDomainData ad = GetAppDomainData(SystemDomain.Address);
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
		}
		if (SharedDomain != null)
		{
			IAppDomainData ad = GetAppDomainData(SharedDomain.Address);
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
		}
		IAppDomainStoreData appDomainStoreData = GetAppDomainStoreData();
		if (appDomainStoreData != null)
		{
			ulong[] appDomainList = GetAppDomainList(appDomainStoreData.Count);
			if (appDomainList != null)
			{
				ulong[] array2 = appDomainList;
				foreach (ulong addr2 in array2)
				{
					IAppDomainData ad = GetAppDomainData(addr2);
					foreach (MemoryRegion item8 in adhw.EnumerateHeaps(ad))
					{
						yield return item8;
					}
					foreach (ulong item9 in EnumerateModules(ad))
					{
						foreach (MemoryRegion item10 in adhw.EnumerateModuleHeaps(ad, item9))
						{
							yield return item10;
						}
					}
				}
			}
		}
		regions.Clear();
		foreach (ICodeHeap item11 in EnumerateJitHeaps())
		{
			if (item11.Type == CodeHeapType.Host)
			{
				if (_dataReader.VirtualQuery(item11.Address, out var vq2))
				{
					yield return new MemoryRegion(this, vq2.BaseAddress, vq2.Size, ClrMemoryRegionType.JitHostCodeHeap);
				}
				else
				{
					yield return new MemoryRegion(this, item11.Address, 0uL, ClrMemoryRegionType.JitHostCodeHeap);
				}
			}
			else
			{
				if (item11.Type != 0)
				{
					continue;
				}
				foreach (MemoryRegion item12 in adhw.EnumerateJitHeap(item11.Address))
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

	protected ClrThread GetThreadByStackAddress(ulong address)
	{
		foreach (ClrThread item in _threads.Value)
		{
			ulong num = item.StackBase;
			ulong num2 = item.StackLimit;
			if (num > num2)
			{
				ulong num3 = num;
				num = num2;
				num2 = num3;
			}
			if (num <= address && address <= num2)
			{
				return item;
			}
		}
		return null;
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
		foreach (ClrThread item in _threads.Value)
		{
			if (item.Address == threadFromThinlock)
			{
				return item;
			}
		}
		return null;
	}

	private ClrModule GetMscorlib()
	{
		ClrModule clrModule = null;
		string value = ((base.ClrInfo.Flavor == ClrFlavor.Core) ? "system.private.corelib" : "mscorlib");
		foreach (DesktopModule value2 in _modules.Values)
		{
			if (value2.Name.ToLowerInvariant().Contains(value))
			{
				clrModule = value2;
				break;
			}
		}
		if (clrModule == null)
		{
			IAppDomainStoreData appDomainStoreData = GetAppDomainStoreData();
			IAppDomainData appDomainData = GetAppDomainData(appDomainStoreData.SharedDomain);
			ulong[] assemblyList = GetAssemblyList(appDomainStoreData.SharedDomain, appDomainData.AssemblyCount);
			foreach (ulong assembly in assemblyList)
			{
				if (GetAssemblyName(assembly).ToLowerInvariant().Contains(value))
				{
					IAssemblyData assemblyData = GetAssemblyData(appDomainStoreData.SharedDomain, assembly);
					ulong module = GetModuleList(assembly, assemblyData.ModuleCount).Single();
					clrModule = GetModule(module);
				}
			}
		}
		return clrModule;
	}

	private static IEnumerable<ClrModule> UniqueModules(Dictionary<ulong, DesktopModule>.ValueCollection self)
	{
		HashSet<DesktopModule> set = new HashSet<DesktopModule>();
		foreach (DesktopModule item in self)
		{
			if (!set.Contains(item))
			{
				set.Add(item);
				yield return item;
			}
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

	private DesktopThreadPool CreateThreadPoolData()
	{
		IThreadPoolData threadPoolData = GetThreadPoolData();
		if (threadPoolData == null)
		{
			return null;
		}
		return new DesktopThreadPool(this, threadPoolData);
	}

	private DomainContainer CreateAppDomainList()
	{
		IAppDomainStoreData appDomainStoreData = GetAppDomainStoreData();
		if (appDomainStoreData == null)
		{
			return DomainContainer.Empty;
		}
		ulong[] appDomainList = GetAppDomainList(appDomainStoreData.Count);
		if (appDomainList == null)
		{
			return DomainContainer.Empty;
		}
		return new DomainContainer(InitDomain(appDomainStoreData.SystemDomain, "System Domain"), InitDomain(appDomainStoreData.SharedDomain, "Shared Domain"), (from ad in ((IEnumerable<ulong>)appDomainList).Select((Func<ulong, ClrAppDomain>)((ulong ad) => InitDomain(ad)))
			where ad != null
			select ad).ToArray());
	}

	private DesktopAppDomain InitDomain(ulong domain, string name = null)
	{
		IAppDomainData appDomainData = GetAppDomainData(domain);
		if (appDomainData == null)
		{
			return null;
		}
		DesktopAppDomain desktopAppDomain = new DesktopAppDomain(this, appDomainData, name ?? GetAppDomaminName(domain));
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
		if (pEFileName == null)
		{
			value = new DesktopModule(this, module, moduleData, pEFileName, assemblyName);
		}
		else if (!_moduleFiles.TryGetValue(pEFileName, out value))
		{
			value = new DesktopModule(this, module, moduleData, pEFileName, assemblyName);
			_moduleFiles[pEFileName] = value;
		}
		_moduleList = null;
		_modules[module] = value;
		return value;
	}

	internal string GetTypeName(TypeHandle id)
	{
		if (id.MethodTable == FreeMethodTable)
		{
			return "Free";
		}
		if (id.MethodTable == ArrayMethodTable && id.ComponentMethodTable != 0L)
		{
			string methodTableName = GetMethodTableName(id.ComponentMethodTable);
			if (methodTableName != null)
			{
				return methodTableName + "[]";
			}
		}
		return GetMethodTableName(id.MethodTable);
	}

	internal IEnumerable<ClrStackFrame> EnumerateStackFrames(DesktopThread thread)
	{
		using ClrStackWalk stackwalk = _dacInterface.CreateStackWalk(thread.OSThreadId, 15u);
		if (stackwalk == null)
		{
			yield break;
		}
		byte[] context = ContextHelper.Context;
		uint contextSize;
		while (stackwalk.GetContext(ContextHelper.ContextFlags, ContextHelper.Length, out contextSize, context))
		{
			ulong ip;
			ulong num;
			if (PointerSize == 4)
			{
				ip = BitConverter.ToUInt32(context, ContextHelper.InstructionPointerOffset);
				num = BitConverter.ToUInt32(context, ContextHelper.StackPointerOffset);
			}
			else
			{
				ip = BitConverter.ToUInt64(context, ContextHelper.InstructionPointerOffset);
				num = BitConverter.ToUInt64(context, ContextHelper.StackPointerOffset);
			}
			ulong value = stackwalk.GetFrameVtable();
			if (value != 0L)
			{
				num = value;
				ReadPointer(num, out value);
			}
			byte[] array = new byte[context.Length];
			Buffer.BlockCopy(context, 0, array, 0, context.Length);
			yield return GetStackFrame(thread, array, ip, num, value);
			if (!stackwalk.Next())
			{
				break;
			}
		}
	}

	internal ILToNativeMap[] GetILMap(ulong ip, HotColdRegions hotColdInfo)
	{
		List<ILToNativeMap> list = new List<ILToNativeMap>();
		_ = hotColdInfo.ColdSize;
		_ = hotColdInfo.HotSize;
		_ = hotColdInfo.ColdStart;
		_ = hotColdInfo.ColdSize;
		_ = hotColdInfo.HotStart;
		_ = hotColdInfo.HotSize;
		foreach (ClrDataMethod item in _dacInterface.EnumerateMethodInstancesByAddress(ip))
		{
			ILToNativeMap[] iLToNativeMap = item.GetILToNativeMap();
			if (iLToNativeMap != null)
			{
				for (int i = 0; i < iLToNativeMap.Length; i++)
				{
					if (iLToNativeMap[i].StartAddress > iLToNativeMap[i].EndAddress)
					{
						if (i + 1 == iLToNativeMap.Length)
						{
							iLToNativeMap[i].EndAddress = FindEnd(hotColdInfo, iLToNativeMap[i].StartAddress);
						}
						else
						{
							iLToNativeMap[i].EndAddress = iLToNativeMap[i + 1].StartAddress - 1;
						}
					}
				}
				list.AddRange(iLToNativeMap);
			}
			item.Dispose();
		}
		return list.ToArray();
	}

	private static ulong FindEnd(HotColdRegions reg, ulong address)
	{
		ulong num = reg.HotStart + reg.HotSize;
		if (reg.HotStart <= address && address < num)
		{
			return num;
		}
		ulong num2 = reg.ColdStart + reg.ColdSize;
		if (reg.ColdStart <= address && address < num2)
		{
			return num2;
		}
		return address + 32;
	}

	internal abstract Dictionary<ulong, List<ulong>> GetDependentHandleMap(CancellationToken cancelToken);

	internal abstract uint GetExceptionHROffset();

	internal abstract ulong[] GetAppDomainList(int count);

	internal abstract ulong[] GetAssemblyList(ulong appDomain, int count);

	internal abstract ulong[] GetModuleList(ulong assembly, int count);

	internal abstract IAssemblyData GetAssemblyData(ulong domain, ulong assembly);

	internal abstract IAppDomainStoreData GetAppDomainStoreData();

	internal abstract bool GetCommonMethodTables(ref CommonMethodTables mCommonMTs);

	internal abstract string GetPEFileName(ulong addr);

	internal abstract IModuleData GetModuleData(ulong addr);

	internal abstract IAppDomainData GetAppDomainData(ulong addr);

	internal abstract string GetAppDomaminName(ulong addr);

	internal abstract bool TraverseHeap(ulong heap, SOSDac.LoaderHeapTraverse callback);

	internal abstract bool TraverseStubHeap(ulong appDomain, int type, SOSDac.LoaderHeapTraverse callback);

	internal abstract IEnumerable<ICodeHeap> EnumerateJitHeaps();

	internal abstract ulong GetModuleForMT(ulong mt);

	internal abstract IFieldInfo GetFieldInfo(ulong mt);

	internal abstract IFieldData GetFieldData(ulong fieldDesc);

	internal abstract MetaDataImport GetMetadataImport(ulong module);

	internal abstract IObjectData GetObjectData(ulong objRef);

	internal abstract ulong GetMethodTableByEEClass(ulong eeclass);

	internal abstract IList<MethodTableTokenPair> GetMethodTableList(ulong module);

	internal abstract IDomainLocalModuleData GetDomainLocalModuleById(ulong appDomain, ulong id);

	internal abstract ICCWData GetCCWData(ulong ccw);

	internal abstract IRCWData GetRCWData(ulong rcw);

	internal abstract COMInterfacePointerData[] GetCCWInterfaces(ulong ccw, int count);

	internal abstract COMInterfacePointerData[] GetRCWInterfaces(ulong rcw, int count);

	internal abstract ulong GetThreadStaticPointer(ulong thread, ClrElementType type, uint offset, uint moduleId, bool shared);

	internal abstract IDomainLocalModuleData GetDomainLocalModule(ulong appDomain, ulong module);

	internal abstract IList<ulong> GetMethodDescList(ulong methodTable);

	internal abstract string GetNameForMD(ulong md);

	internal abstract IMethodDescData GetMethodDescData(ulong md);

	internal abstract uint GetMetadataToken(ulong mt);

	protected abstract DesktopStackFrame GetStackFrame(DesktopThread thread, byte[] context, ulong ip, ulong sp, ulong frameVtbl);

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

	internal abstract ulong GetILForModule(ClrModule module, uint rva);
}
