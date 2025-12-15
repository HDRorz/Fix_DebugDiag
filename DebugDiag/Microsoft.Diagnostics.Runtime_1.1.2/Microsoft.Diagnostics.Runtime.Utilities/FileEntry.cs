using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct FileEntry : IEquatable<FileEntry>
{
	public string FileName;

	public int TimeStamp;

	public int FileSize;

	public FileEntry(string filename, int timestamp, int filesize)
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
		if (obj is FileEntry)
		{
			return Equals((FileEntry)obj);
		}
		return false;
	}

	public bool Equals(FileEntry other)
	{
		if (FileName.Equals(other.FileName, StringComparison.OrdinalIgnoreCase) && TimeStamp == other.TimeStamp)
		{
			return FileSize == other.FileSize;
		}
		return false;
	}
}
