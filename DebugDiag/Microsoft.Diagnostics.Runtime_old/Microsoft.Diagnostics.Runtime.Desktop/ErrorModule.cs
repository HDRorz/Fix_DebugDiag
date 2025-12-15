using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class ErrorModule : DesktopBaseModule
{
	private static uint s_id;

	private uint _id = s_id++;

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

	public override bool IsPdbLoaded => false;

	public override object PdbInterface => null;

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

	public override bool IsMatchingPdb(string pdbPath)
	{
		return false;
	}

	public override void LoadPdb(string path)
	{
	}

	public override string TryDownloadPdb()
	{
		return null;
	}

	public override SourceLocation GetSourceInformation(uint token, int ilOffset)
	{
		return null;
	}

	public override SourceLocation GetSourceInformation(ClrMethod method, int ilOffset)
	{
		return null;
	}
}
