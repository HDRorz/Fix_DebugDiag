using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct MethodTableCollectibleData : IMethodTableCollectibleData
{
	public readonly ulong LoaderAllocatorObjectHandle;

	public readonly uint Collectible;

	ulong IMethodTableCollectibleData.LoaderAllocatorObjectHandle => LoaderAllocatorObjectHandle;

	bool IMethodTableCollectibleData.Collectible => Collectible != 0;
}
