using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.DacInterface;

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

	private readonly List<MemoryRegion> _regions = new List<MemoryRegion>();

	private readonly SOSDac.LoaderHeapTraverse _delegate;

	private ClrMemoryRegionType _type;

	private ulong _appDomain;

	private readonly DesktopRuntimeBase _runtime;

	public AppDomainHeapWalker(DesktopRuntimeBase runtime)
	{
		_runtime = runtime;
		_delegate = VisitOneHeap;
	}

	public IEnumerable<MemoryRegion> EnumerateHeaps(IAppDomainData appDomain)
	{
		_appDomain = appDomain.Address;
		_regions.Clear();
		_type = ClrMemoryRegionType.LowFrequencyLoaderHeap;
		_runtime.TraverseHeap(appDomain.LowFrequencyHeap, _delegate);
		_type = ClrMemoryRegionType.HighFrequencyLoaderHeap;
		_runtime.TraverseHeap(appDomain.HighFrequencyHeap, _delegate);
		_type = ClrMemoryRegionType.StubHeap;
		_runtime.TraverseHeap(appDomain.StubHeap, _delegate);
		_type = ClrMemoryRegionType.IndcellHeap;
		_runtime.TraverseStubHeap(_appDomain, 0, _delegate);
		_type = ClrMemoryRegionType.LookupHeap;
		_runtime.TraverseStubHeap(_appDomain, 1, _delegate);
		_type = ClrMemoryRegionType.ResolveHeap;
		_runtime.TraverseStubHeap(_appDomain, 2, _delegate);
		_type = ClrMemoryRegionType.DispatchHeap;
		_runtime.TraverseStubHeap(_appDomain, 3, _delegate);
		_type = ClrMemoryRegionType.CacheEntryHeap;
		_runtime.TraverseStubHeap(_appDomain, 4, _delegate);
		return _regions;
	}

	public IEnumerable<MemoryRegion> EnumerateModuleHeaps(IAppDomainData appDomain, ulong addr)
	{
		_appDomain = appDomain.Address;
		_regions.Clear();
		if (addr == 0L)
		{
			return _regions;
		}
		IModuleData moduleData = _runtime.GetModuleData(addr);
		if (moduleData != null)
		{
			_type = ClrMemoryRegionType.ModuleThunkHeap;
			_runtime.TraverseHeap(moduleData.ThunkHeap, _delegate);
			_type = ClrMemoryRegionType.ModuleLookupTableHeap;
			_runtime.TraverseHeap(moduleData.LookupTableHeap, _delegate);
		}
		return _regions;
	}

	public IEnumerable<MemoryRegion> EnumerateJitHeap(ulong heap)
	{
		_appDomain = 0uL;
		_regions.Clear();
		_type = ClrMemoryRegionType.JitLoaderCodeHeap;
		_runtime.TraverseHeap(heap, _delegate);
		return _regions;
	}

	private void VisitOneHeap(ulong address, IntPtr size, int isCurrent)
	{
		if (_appDomain == 0L)
		{
			_regions.Add(new MemoryRegion(_runtime, address, (ulong)size.ToInt64(), _type));
		}
		else
		{
			_regions.Add(new MemoryRegion(_runtime, address, (ulong)size.ToInt64(), _type, _appDomain));
		}
	}
}
