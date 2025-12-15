namespace DebugDiag.DotNet.x86Analysis;

internal class AnalysisServiceProgress : IAnalysisServiceProgress
{
	private NetProgress _progress;

	public AnalysisServiceProgress(NetProgress progress)
	{
		_progress = progress;
	}

	public void SetOverallRange(int Low, int High)
	{
		if (_progress != null)
		{
			_progress.SetOverallRange(Low, High);
		}
	}

	public void SetCurrentRange(int Low, int High)
	{
		if (_progress != null)
		{
			_progress.SetCurrentRange(Low, High);
		}
	}

	public void SetOverallPosition(int value)
	{
		if (_progress != null)
		{
			_progress.OverallPosition = value;
		}
	}

	public void SetCurrentPosition(int value)
	{
		if (_progress != null)
		{
			_progress.CurrentPosition = value;
		}
	}

	public void SetOverallStatus(string value)
	{
		if (_progress != null)
		{
			_progress.OverallStatus = value;
		}
	}

	public void SetCurrentStatus(string value)
	{
		if (_progress != null)
		{
			_progress.CurrentStatus = value;
		}
	}

	public void SetDebuggerStatus(string value)
	{
		if (_progress != null)
		{
			_progress.DebuggerStatus = value;
		}
	}
}
