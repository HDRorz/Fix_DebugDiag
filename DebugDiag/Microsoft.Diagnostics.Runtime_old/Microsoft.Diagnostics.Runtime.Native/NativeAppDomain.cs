using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeAppDomain : ClrAppDomain
{
	private IList<ClrModule> _modules;

	public override ulong Address => 0uL;

	public override int Id => 0;

	public override string Name => "default domain";

	public override IList<ClrModule> Modules => _modules;

	public override string ConfigurationFile => null;

	public override string ApplicationBase => null;

	public NativeAppDomain(IList<ClrModule> modules)
	{
		_modules = modules;
	}
}
