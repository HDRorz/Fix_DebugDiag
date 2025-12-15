namespace Microsoft.Diagnostics.Runtime;

public enum ClrMemoryRegionType
{
	LowFrequencyLoaderHeap,
	HighFrequencyLoaderHeap,
	StubHeap,
	IndcellHeap,
	LookupHeap,
	ResolveHeap,
	DispatchHeap,
	CacheEntryHeap,
	JitHostCodeHeap,
	JitLoaderCodeHeap,
	ModuleThunkHeap,
	ModuleLookupTableHeap,
	GCSegment,
	ReservedGCSegment,
	HandleTableChunk
}
