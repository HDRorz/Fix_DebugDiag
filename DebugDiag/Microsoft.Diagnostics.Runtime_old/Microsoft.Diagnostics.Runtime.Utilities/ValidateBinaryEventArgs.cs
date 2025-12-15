namespace Microsoft.Diagnostics.Runtime.Utilities;

public class ValidateBinaryEventArgs
{
	public string File { get; private set; }

	public int FileSize { get; private set; }

	public int TimeStamp { get; private set; }

	public bool ValidateProperties { get; private set; }

	public bool Rejected { get; private set; }

	public bool Accepted { get; private set; }

	public void Reject()
	{
		Rejected = true;
	}

	public void Accept()
	{
		Accepted = true;
	}

	public ValidateBinaryEventArgs(string fileName, int timestamp, int fileSize, bool validate)
	{
		File = fileName;
		FileSize = fileSize;
		TimeStamp = timestamp;
		ValidateProperties = validate;
	}
}
