using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public struct VersionInfo : IEquatable<VersionInfo>, IComparable<VersionInfo>
{
	public readonly int Major;

	public readonly int Minor;

	public readonly int Revision;

	public readonly int Patch;

	internal VersionInfo(int major, int minor, int revision, int patch)
	{
		Major = major;
		Minor = minor;
		Revision = revision;
		Patch = patch;
	}

	public bool Equals(VersionInfo other)
	{
		if (Major == other.Major && Minor == other.Minor && Revision == other.Revision)
		{
			return Patch == other.Patch;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is VersionInfo other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((Major * 397) ^ Minor) * 397) ^ Revision) * 397) ^ Patch;
	}

	public int CompareTo(VersionInfo other)
	{
		if (Major != other.Major)
		{
			return Major.CompareTo(other.Major);
		}
		if (Minor != other.Minor)
		{
			return Minor.CompareTo(other.Minor);
		}
		if (Revision != other.Revision)
		{
			return Revision.CompareTo(other.Revision);
		}
		return Patch.CompareTo(other.Patch);
	}

	public override string ToString()
	{
		return $"v{Major}.{Minor}.{Revision}.{Patch:D2}";
	}
}
