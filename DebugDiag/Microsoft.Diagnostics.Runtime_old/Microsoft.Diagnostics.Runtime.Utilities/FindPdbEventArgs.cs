using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class FindPdbEventArgs
{
	public string FileName { get; private set; }

	public Guid Guid { get; private set; }

	public int Revision { get; private set; }

	public string Result { get; set; }

	internal FindPdbEventArgs(string filename, Guid guid, int revision)
	{
		FileName = filename;
		Guid = guid;
		Revision = revision;
	}
}
