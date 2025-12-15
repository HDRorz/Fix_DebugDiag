using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class ErrorModule : DesktopBaseModule
{
	private static uint s_id;

	private readonly uint _id = s_id++;

	public override PdbInfo Pdb => null;

	public override IList<ClrAppDomain> AppDomains => new ClrAppDomain[0];

	public override string AssemblyName => "<error>";

	public override string Name => "<error>";

	public override bool IsDynamic => false;

	public override bool IsFile => false;

	public override string FileName => "<error>";

	public override ulong ImageBase => 0uL;

	public override ulong Size => 0uL;

	public override ulong MetadataAddress => 0uL;

	public override ulong MetadataLength => 0uL;

	public override object MetadataImport => null;

	public override DebuggableAttribute.DebuggingModes DebuggingMode => DebuggableAttribute.DebuggingModes.None;

	public override ulong AssemblyId => _id;

	public ErrorModule(DesktopRuntimeBase runtime)
		: base(runtime)
	{
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		return new ClrType[0];
	}

	internal override ulong GetDomainModule(ClrAppDomain appDomain)
	{
		return 0uL;
	}

	public override ClrType GetTypeByName(string name)
	{
		return null;
	}
}
