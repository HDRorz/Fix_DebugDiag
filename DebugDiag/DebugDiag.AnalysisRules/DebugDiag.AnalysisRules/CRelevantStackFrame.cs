namespace DebugDiag.AnalysisRules;

public class CRelevantStackFrame
{
	private string m_functionName;

	private string m_FunctionNameNoOffset = "";

	private int m_priority;

	public int Priority
	{
		get
		{
			return m_priority;
		}
		set
		{
			m_priority = value;
		}
	}

	public string FunctionName => m_functionName;

	public string FunctionNameNoOffset => m_FunctionNameNoOffset;

	public void Init(string functionName, int priority, bool hasSymbols)
	{
		m_functionName = functionName;
		if (hasSymbols)
		{
			m_FunctionNameNoOffset = Globals.HelperFunctions.Split(functionName, "+", -1, 0)[0];
		}
		else
		{
			m_FunctionNameNoOffset = functionName;
		}
		m_priority = priority;
	}
}
