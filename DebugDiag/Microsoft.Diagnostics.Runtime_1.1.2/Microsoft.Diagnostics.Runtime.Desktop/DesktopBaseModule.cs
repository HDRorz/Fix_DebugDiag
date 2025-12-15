using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class DesktopBaseModule : ClrModule
{
	protected DesktopRuntimeBase _runtime;

	public override ClrRuntime Runtime => _runtime;

	internal ulong ModuleId { get; set; }

	public int Revision { get; set; }

	internal abstract ulong GetDomainModule(ClrAppDomain appDomain);

	internal virtual MetaDataImport GetMetadataImport()
	{
		return null;
	}

	public DesktopBaseModule(DesktopRuntimeBase runtime)
	{
		_runtime = runtime;
	}
}
