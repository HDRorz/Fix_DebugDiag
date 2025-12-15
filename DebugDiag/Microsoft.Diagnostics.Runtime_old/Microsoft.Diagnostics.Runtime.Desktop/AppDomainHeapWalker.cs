using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class AppDomainHeapWalker
{
	private enum InternalHeapTypes
	{
		IndcellHeap,
		LookupHeap,
		ResolveHeap,
		DispatchHeap,
		CacheEntryHeap
	}

	private List<MemoryRegion> _mRegions = new List<MemoryRegion>();

	private DesktopRuntimeBase.LoaderHeapTraverse _mDelegate;

	private ClrMemoryRegionType _mType;

	private ulong _mAppDomain;

	private DesktopRuntimeBase _mRuntime;

	public AppDomainHeapWalker(DesktopRuntimeBase runtime)
	{
		_mRuntime = runtime;
		_mDelegate = VisitOneHeap;
	}

	public IEnumerable<MemoryRegion> EnumerateHeaps(IAppDomainData appDomain)
	{
		_mAppDomain = appDomain.Address;
		_mRegions.Clear();
		_mType = ClrMemoryRegionType.LowFrequencyLoaderHeap;
		_mRuntime.TraverseHeap(appDomain.LowFrequencyHeap, _mDelegate);
		_mType = ClrMemoryRegionType.HighFrequencyLoaderHeap;
		_mRuntime.TraverseHeap(appDomain.HighFrequencyHeap, _mDelegate);
		_mType = ClrMemoryRegionType.StubHeap;
		_mRuntime.TraverseHeap(appDomain.StubHeap, _mDelegate);
		_mType = ClrMemoryRegionType.IndcellHeap;
		_mRuntime.TraverseStubHeap(_mAppDomain, 0, _mDelegate);
		_mType = ClrMemoryRegionType.LookupHeap;
		_mRuntime.TraverseStubHeap(_mAppDomain, 1, _mDelegate);
		_mType = ClrMemoryRegionType.ResolveHeap;
		_mRuntime.TraverseStubHeap(_mAppDomain, 2, _mDelegate);
		_mType = ClrMemoryRegionType.DispatchHeap;
		_mRuntime.TraverseStubHeap(_mAppDomain, 3, _mDelegate);
		_mType = ClrMemoryRegionType.CacheEntryHeap;
		_mRuntime.TraverseStubHeap(_mAppDomain, 4, _mDelegate);
		return _mRegions;
	}

	public IEnumerable<MemoryRegion> EnumerateModuleHeaps(IAppDomainData appDomain, ulong addr)
	{
		_mAppDomain = appDomain.Address;
		_mRegions.Clear();
		if (addr == 0L)
		{
			return _mRegions;
		}
		IModuleData moduleData = _mRuntime.GetModuleData(addr);
		if (moduleData != null)
		{
			_mType = ClrMemoryRegionType.ModuleThunkHeap;
			_mRuntime.TraverseHeap(moduleData.ThunkHeap, _mDelegate);
			_mType = ClrMemoryRegionType.ModuleLookupTableHeap;
			_mRuntime.TraverseHeap(moduleData.LookupTableHeap, _mDelegate);
		}
		return _mRegions;
	}

	public IEnumerable<MemoryRegion> EnumerateJitHeap(ulong heap)
	{
		_mAppDomain = 0uL;
		_mRegions.Clear();
		_mType = ClrMemoryRegionType.JitLoaderCodeHeap;
		_mRuntime.TraverseHeap(heap, _mDelegate);
		return _mRegions;
	}

	private void VisitOneHeap(ulong address, IntPtr size, int isCurrent)
	{
		if (_mAppDomain == 0L)
		{
			_mRegions.Add(new MemoryRegion(_mRuntime, address, (ulong)size.ToInt64(), _mType));
		}
		else
		{
			_mRegions.Add(new MemoryRegion(_mRuntime, address, (ulong)size.ToInt64(), _mType, _mAppDomain));
		}
	}
}
