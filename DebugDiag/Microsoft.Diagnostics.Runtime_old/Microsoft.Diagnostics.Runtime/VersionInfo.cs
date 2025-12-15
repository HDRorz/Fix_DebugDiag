using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public struct VersionInfo
{
	public int Major;

	public int Minor;

	public int Revision;

	public int Patch;

	internal VersionInfo(int major, int minor, int revision, int patch)
	{
		Major = major;
		Minor = minor;
		Revision = revision;
		Patch = patch;
	}

	public override string ToString()
	{
		return $"v{Major}.{Minor}.{Revision}.{Patch:D2}";
	}
}
