namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class DesktopBaseModule : ClrModule
{
	internal ulong ModuleId { get; set; }

	public int Revision { get; set; }

	internal abstract ulong GetDomainModule(ClrAppDomain appDomain);

	internal virtual IMetadata GetMetadataImport()
	{
		return null;
	}
}
