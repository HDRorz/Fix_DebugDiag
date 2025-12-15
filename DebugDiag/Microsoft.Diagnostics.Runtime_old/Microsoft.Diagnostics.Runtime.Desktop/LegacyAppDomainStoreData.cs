namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct LegacyAppDomainStoreData : IAppDomainStoreData
{
	private ulong _shared;

	private ulong _system;

	private int _domainCount;

	public ulong SharedDomain => _shared;

	public ulong SystemDomain => _system;

	public int Count => _domainCount;
}
