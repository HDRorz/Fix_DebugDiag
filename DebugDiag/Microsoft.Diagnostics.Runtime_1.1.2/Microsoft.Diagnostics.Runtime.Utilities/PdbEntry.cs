using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct PdbEntry : IEquatable<PdbEntry>
{
	public string FileName;

	public Guid Guid;

	public int Revision;

	public PdbEntry(string filename, Guid guid, int revision)
	{
		FileName = filename;
		Guid = guid;
		Revision = revision;
	}

	public override int GetHashCode()
	{
		return FileName.ToLower().GetHashCode() ^ Guid.GetHashCode() ^ Revision;
	}

	public override bool Equals(object obj)
	{
		if (obj is PdbEntry)
		{
			return Equals((PdbEntry)obj);
		}
		return false;
	}

	public bool Equals(PdbEntry other)
	{
		if (Revision == other.Revision && FileName.Equals(other.FileName, StringComparison.OrdinalIgnoreCase))
		{
			return Guid == other.Guid;
		}
		return false;
	}
}
