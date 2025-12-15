using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct AppDomainStoreData : IAppDomainStoreData
{
	public readonly ulong SharedDomain;

	public readonly ulong SystemDomain;

	public readonly int AppDomainCount;

	ulong IAppDomainStoreData.SharedDomain => SharedDomain;

	ulong IAppDomainStoreData.SystemDomain => SystemDomain;

	int IAppDomainStoreData.Count => AppDomainCount;
}
