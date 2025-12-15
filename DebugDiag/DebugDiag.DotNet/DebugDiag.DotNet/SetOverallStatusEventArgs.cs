using System;

namespace DebugDiag.DotNet;

public class SetOverallStatusEventArgs : EventArgs
{
	public string NewStatus;

	public SetOverallStatusEventArgs(string newStatus)
	{
		NewStatus = newStatus;
	}
}
