using System;

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
}
