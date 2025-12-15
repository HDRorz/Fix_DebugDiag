using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct BinaryEntry : IEquatable<BinaryEntry>
{
	public string FileName;

	public int TimeStamp;

	public int FileSize;

	public BinaryEntry(string filename, int timestamp, int filesize)
	{
		FileName = filename;
		TimeStamp = timestamp;
		FileSize = filesize;
	}

	public override int GetHashCode()
	{
		return FileName.ToLower().GetHashCode() ^ TimeStamp ^ FileSize;
	}

	public override bool Equals(object obj)
	{
		if (obj is BinaryEntry)
		{
			return Equals((BinaryEntry)obj);
		}
		return false;
	}

	public bool Equals(BinaryEntry other)
	{
		if (FileName.Equals(other.FileName, StringComparison.OrdinalIgnoreCase) && TimeStamp == other.TimeStamp)
		{
			return FileSize == other.FileSize;
		}
		return false;
	}
}
