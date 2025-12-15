using System;

namespace DebugDiag.DotNet;

public class SetOverallRangeEventArgs : EventArgs
{
	public int NewLow;

	public int NewHigh;

	public SetOverallRangeEventArgs(int Low, int High)
	{
		NewLow = Low;
		NewHigh = High;
	}
}
