using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class DacInfo : ModuleInfo
{
	public string PlatformAgnosticFileName { get; set; }

	public Architecture TargetArchitecture { get; set; }

	[Obsolete("Use ClrInfoProvider.GetDacRequestFileName")]
	public static string GetDacRequestFileName(ClrFlavor flavor, Architecture currentArchitecture, Architecture targetArchitecture, VersionInfo clrVersion)
	{
		return ClrInfoProvider.GetDacRequestFileName(flavor, currentArchitecture, targetArchitecture, clrVersion, Platform.Windows);
	}

	public DacInfo(IDataReader reader, string agnosticName, Architecture targetArch)
		: base(reader, null)
	{
		PlatformAgnosticFileName = agnosticName;
		TargetArchitecture = targetArch;
	}
}
