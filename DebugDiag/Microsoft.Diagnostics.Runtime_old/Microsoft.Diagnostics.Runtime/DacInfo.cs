using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class DacInfo : ModuleInfo
{
	public string PlatformAgnosticFileName { get; set; }

	public Architecture TargetArchitecture { get; set; }

	public static string GetDacRequestFileName(ClrFlavor flavor, Architecture currentArchitecture, Architecture targetArchitecture, VersionInfo clrVersion)
	{
		object obj;
		switch (flavor)
		{
		case ClrFlavor.Redhawk:
			if (targetArchitecture != Architecture.Amd64)
			{
				return "mrt100dac_winx86.dll";
			}
			return "mrt100dac_winamd64.dll";
		default:
			obj = "mscordacwks";
			break;
		case ClrFlavor.CoreCLR:
			obj = "mscordaccore";
			break;
		}
		string text = (string)obj;
		return $"{text}_{currentArchitecture}_{targetArchitecture}_{clrVersion.Major}.{clrVersion.Minor}.{clrVersion.Revision}.{clrVersion.Patch:D2}.dll";
	}

	public DacInfo(IDataReader reader, string agnosticName, Architecture targetArch)
		: base(reader)
	{
		PlatformAgnosticFileName = agnosticName;
		TargetArchitecture = targetArch;
	}
}
