using System;

namespace DebugDiag.DotNet;

public class SetOverallPositionEventArgs : EventArgs
{
	public int NewPosition;

	public SetOverallPositionEventArgs(int newPosition)
	{
		NewPosition = newPosition;
	}
}
