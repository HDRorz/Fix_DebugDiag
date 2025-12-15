using System;

namespace Microsoft.Diagnostics.Runtime;

public abstract class PlatformFunctions
{
	internal abstract bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch);

	public abstract bool TryGetWow64(IntPtr proc, out bool result);

	public abstract IntPtr LoadLibrary(string lpFileName);

	public abstract bool FreeLibrary(IntPtr module);

	public abstract IntPtr GetProcAddress(IntPtr module, string method);

	public virtual bool IsEqualFileVersion(string file, VersionInfo version)
	{
		if (!GetFileVersion(file, out var major, out var minor, out var revision, out var patch))
		{
			return false;
		}
		if (major == version.Major && minor == version.Minor && revision == version.Revision)
		{
			return patch == version.Patch;
		}
		return false;
	}
}
