using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class PdbInfo
{
	public Guid Guid { get; set; }

	public int Revision { get; set; }

	public string FileName { get; set; }

	public PdbInfo()
	{
	}

	public PdbInfo(string fileName, Guid guid, int rev)
	{
		FileName = fileName;
		Guid = guid;
		Revision = rev;
	}

	public override int GetHashCode()
	{
		return Guid.GetHashCode() ^ Revision;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj is PdbInfo pdbInfo && Revision == pdbInfo.Revision && Guid == pdbInfo.Guid)
		{
			string fileName = Path.GetFileName(FileName);
			string fileName2 = Path.GetFileName(pdbInfo.FileName);
			return fileName.Equals(fileName2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public override string ToString()
	{
		return $"{Guid} {Revision} {FileName}";
	}
}
