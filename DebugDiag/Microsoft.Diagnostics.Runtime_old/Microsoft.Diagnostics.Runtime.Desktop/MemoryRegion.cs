using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class MemoryRegion : ClrMemoryRegion
{
	private DesktopRuntimeBase _runtime;

	private ulong _domainModuleHeap;

	private GCSegmentType _segmentType;

	private bool HasAppDomainData
	{
		get
		{
			if (base.Type > ClrMemoryRegionType.CacheEntryHeap)
			{
				return base.Type == ClrMemoryRegionType.HandleTableChunk;
			}
			return true;
		}
	}

	private bool HasModuleData
	{
		get
		{
			if (base.Type != ClrMemoryRegionType.ModuleThunkHeap)
			{
				return base.Type == ClrMemoryRegionType.ModuleLookupTableHeap;
			}
			return true;
		}
	}

	private bool HasGCHeapData
	{
		get
		{
			if (base.Type != ClrMemoryRegionType.GCSegment)
			{
				return base.Type == ClrMemoryRegionType.ReservedGCSegment;
			}
			return true;
		}
	}

	public override ClrAppDomain AppDomain
	{
		get
		{
			if (!HasAppDomainData)
			{
				return null;
			}
			return _runtime.GetAppDomainByAddress(_domainModuleHeap);
		}
	}

	public override string Module
	{
		get
		{
			if (!HasModuleData)
			{
				return null;
			}
			return _runtime.GetModule(_domainModuleHeap).FileName;
		}
	}

	public override int HeapNumber
	{
		get
		{
			if (!HasGCHeapData)
			{
				return -1;
			}
			return (int)_domainModuleHeap;
		}
		set
		{
			_domainModuleHeap = (ulong)value;
		}
	}

	public override GCSegmentType GCSegmentType
	{
		get
		{
			if (!HasGCHeapData)
			{
				throw new NotSupportedException();
			}
			return _segmentType;
		}
		set
		{
			_segmentType = value;
		}
	}

	public override string ToString(bool detailed)
	{
		string text = null;
		switch (base.Type)
		{
		case ClrMemoryRegionType.LowFrequencyLoaderHeap:
			text = "Low Frequency Loader Heap";
			break;
		case ClrMemoryRegionType.HighFrequencyLoaderHeap:
			text = "High Frequency Loader Heap";
			break;
		case ClrMemoryRegionType.StubHeap:
			text = "Stub Heap";
			break;
		case ClrMemoryRegionType.IndcellHeap:
			text = "Indirection Cell Heap";
			break;
		case ClrMemoryRegionType.LookupHeap:
			text = "Loopup Heap";
			break;
		case ClrMemoryRegionType.ResolveHeap:
			text = "Resolver Heap";
			break;
		case ClrMemoryRegionType.DispatchHeap:
			text = "Dispatch Heap";
			break;
		case ClrMemoryRegionType.CacheEntryHeap:
			text = "Cache Entry Heap";
			break;
		case ClrMemoryRegionType.JitHostCodeHeap:
			text = "JIT Host Code Heap";
			break;
		case ClrMemoryRegionType.JitLoaderCodeHeap:
			text = "JIT Loader Code Heap";
			break;
		case ClrMemoryRegionType.ModuleThunkHeap:
			text = "Thunk Heap";
			break;
		case ClrMemoryRegionType.ModuleLookupTableHeap:
			text = "Lookup Table Heap";
			break;
		case ClrMemoryRegionType.HandleTableChunk:
			text = "GC Handle Table Chunk";
			break;
		case ClrMemoryRegionType.GCSegment:
		case ClrMemoryRegionType.ReservedGCSegment:
			text = ((_segmentType == GCSegmentType.Ephemeral) ? "Ephemeral Segment" : ((_segmentType != GCSegmentType.LargeObject) ? "GC Segment" : "Large Object Segment"));
			if (base.Type == ClrMemoryRegionType.ReservedGCSegment)
			{
				text += " (Reserved)";
			}
			break;
		default:
			text = "<unknown>";
			break;
		}
		if (detailed)
		{
			if (HasAppDomainData)
			{
				if (_domainModuleHeap == _runtime.SharedDomainAddress)
				{
					text = $"{text} for Shared AppDomain";
				}
				else if (_domainModuleHeap == _runtime.SystemDomainAddress)
				{
					text = $"{text} for System AppDomain";
				}
				else
				{
					ClrAppDomain appDomain = AppDomain;
					text = $"{text} for AppDomain {appDomain.Id}: {appDomain.Name}";
				}
			}
			else if (HasModuleData)
			{
				string fileName = _runtime.GetModule(_domainModuleHeap).FileName;
				text = $"{text} for Module: {Path.GetFileName(fileName)}";
			}
			else if (HasGCHeapData)
			{
				text = $"{text} for Heap {HeapNumber}";
			}
		}
		return text;
	}

	public override string ToString()
	{
		return ToString(detailed: false);
	}

	internal MemoryRegion(DesktopRuntimeBase clr, ulong addr, ulong size, ClrMemoryRegionType type, ulong moduleOrAppDomain)
	{
		base.Address = addr;
		base.Size = size;
		_runtime = clr;
		base.Type = type;
		_domainModuleHeap = moduleOrAppDomain;
	}

	internal MemoryRegion(DesktopRuntimeBase clr, ulong addr, ulong size, ClrMemoryRegionType type, ClrAppDomain domain)
	{
		base.Address = addr;
		base.Size = size;
		_runtime = clr;
		base.Type = type;
		_domainModuleHeap = domain.Address;
	}

	internal MemoryRegion(DesktopRuntimeBase clr, ulong addr, ulong size, ClrMemoryRegionType type)
	{
		base.Address = addr;
		base.Size = size;
		_runtime = clr;
		base.Type = type;
	}

	internal MemoryRegion(DesktopRuntimeBase clr, ulong addr, ulong size, ClrMemoryRegionType type, uint heap, GCSegmentType seg)
	{
		base.Address = addr;
		base.Size = size;
		_runtime = clr;
		base.Type = type;
		_domainModuleHeap = heap;
		_segmentType = seg;
	}
}
