using System.IO;

namespace Microsoft.Diagnostics.Runtime;

public static class ClrInfoProvider
{
	private const string c_desktopModuleName1 = "clr";

	private const string c_desktopModuleName2 = "mscorwks";

	private const string c_coreModuleName = "coreclr";

	private const string c_linuxCoreModuleName = "libcoreclr";

	private const string c_desktopDacFileNameBase = "mscordacwks";

	private const string c_coreDacFileNameBase = "mscordaccore";

	private const string c_desktopDacFileName = "mscordacwks.dll";

	private const string c_coreDacFileName = "mscordaccore.dll";

	private const string c_linuxCoreDacFileName = "libmscordaccore.so";

	private static bool TryGetModuleName(ModuleInfo moduleInfo, out string moduleName)
	{
		moduleName = Path.GetFileNameWithoutExtension(moduleInfo.FileName);
		if (moduleName == null)
		{
			return false;
		}
		moduleName = moduleName.ToLower();
		return true;
	}

	public static bool IsSupportedRuntime(ModuleInfo moduleInfo, out ClrFlavor flavor, out Platform platform)
	{
		flavor = ClrFlavor.Desktop;
		platform = Platform.Windows;
		if (!TryGetModuleName(moduleInfo, out var moduleName))
		{
			return false;
		}
		switch (moduleName)
		{
		case "clr":
		case "mscorwks":
			flavor = ClrFlavor.Desktop;
			platform = Platform.Windows;
			return true;
		case "coreclr":
			flavor = ClrFlavor.Core;
			platform = Platform.Windows;
			return true;
		case "libcoreclr":
			flavor = ClrFlavor.Core;
			platform = Platform.Linux;
			return true;
		default:
			return false;
		}
	}

	public static string GetDacFileName(ClrFlavor flavor, Platform platform)
	{
		if (platform == Platform.Linux)
		{
			return "libmscordaccore.so";
		}
		if (flavor != ClrFlavor.Core)
		{
			return "mscordacwks.dll";
		}
		return "mscordaccore.dll";
	}

	public static string GetDacRequestFileName(ClrFlavor flavor, Architecture currentArchitecture, Architecture targetArchitecture, VersionInfo version, Platform platform)
	{
		if (platform == Platform.Linux)
		{
			return "libmscordaccore.so";
		}
		string text = ((flavor == ClrFlavor.Core) ? "mscordaccore" : "mscordacwks");
		return $"{text}_{currentArchitecture}_{targetArchitecture}_{version.Major}.{version.Minor}.{version.Revision}.{version.Patch:D2}.dll";
	}
}
