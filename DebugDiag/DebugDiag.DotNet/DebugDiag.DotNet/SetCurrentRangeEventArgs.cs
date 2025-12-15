using System;

namespace DebugDiag.DotNet;

public class SetCurrentRangeEventArgs : EventArgs
{
	public int NewLow;

	public int NewHigh;

	public SetCurrentRangeEventArgs(int newLow, int newHigh)
	{
		NewLow = newLow;
		NewHigh = newHigh;
	}
}
