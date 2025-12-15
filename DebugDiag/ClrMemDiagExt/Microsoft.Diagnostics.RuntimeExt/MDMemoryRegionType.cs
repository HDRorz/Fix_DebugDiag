namespace Microsoft.Diagnostics.RuntimeExt;

public enum MDMemoryRegionType
{
	MDRegion_LowFrequencyLoaderHeap,
	MDRegion_HighFrequencyLoaderHeap,
	MDRegion_StubHeap,
	MDRegion_IndcellHeap,
	MDRegion_LookupHeap,
	MDRegion_ResolveHeap,
	MDRegion_DispatchHeap,
	MDRegion_CacheEntryHeap,
	MDRegion_JITHostCodeHeap,
	MDRegion_JITLoaderCodeHeap,
	MDRegion_ModuleThunkHeap,
	MDRegion_ModuleLookupTableHeap,
	MDRegion_GCSegment,
	MDRegion_ReservedGCSegment,
	MDRegion_HandleTableChunk
}
