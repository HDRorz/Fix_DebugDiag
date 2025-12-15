using System;

namespace DebugDiag.DotNet;

public class SetDebuggerStatusEventArgs : EventArgs
{
	public string NewDebuggerStatus;

	public SetDebuggerStatusEventArgs(string newDebuggerStatus)
	{
		NewDebuggerStatus = newDebuggerStatus;
	}
}
