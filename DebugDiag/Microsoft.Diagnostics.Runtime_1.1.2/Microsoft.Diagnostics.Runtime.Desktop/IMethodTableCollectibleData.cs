namespace Microsoft.Diagnostics.Runtime.Desktop;

internal interface IMethodTableCollectibleData
{
	ulong LoaderAllocatorObjectHandle { get; }

	bool Collectible { get; }
}
