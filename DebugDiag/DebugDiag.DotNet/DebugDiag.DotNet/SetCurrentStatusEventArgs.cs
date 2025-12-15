using System;

namespace DebugDiag.DotNet;

public class SetCurrentStatusEventArgs : EventArgs
{
	public string NewStatus;

	public SetCurrentStatusEventArgs(string newStatus)
	{
		NewStatus = newStatus;
	}
}
