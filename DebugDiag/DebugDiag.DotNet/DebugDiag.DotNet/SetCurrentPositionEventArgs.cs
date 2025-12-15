using System;

namespace DebugDiag.DotNet;

public class SetCurrentPositionEventArgs : EventArgs
{
	public int NewPosition;

	public SetCurrentPositionEventArgs(int newPosition)
	{
		NewPosition = newPosition;
	}
}
