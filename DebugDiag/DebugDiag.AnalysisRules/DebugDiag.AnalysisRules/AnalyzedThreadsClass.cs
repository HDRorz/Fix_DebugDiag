using System.Collections.Generic;

namespace DebugDiag.AnalysisRules;

public class AnalyzedThreadsClass
{
	private Dictionary<int, AnalyzedThreadClass> m_AnalyzedThreadCollection = new Dictionary<int, AnalyzedThreadClass>();

	private Dictionary<string, AnalyzedThreadClass> m_AnalyzedThreadSummaries = new Dictionary<string, AnalyzedThreadClass>();

	public List<AnalyzedThreadClass> Items
	{
		get
		{
			List<AnalyzedThreadClass> list = new List<AnalyzedThreadClass>();
			foreach (AnalyzedThreadClass value in m_AnalyzedThreadCollection.Values)
			{
				list.Add(value);
			}
			return list;
		}
	}

	public int ItemsCount => m_AnalyzedThreadCollection.Count;

	public Dictionary<string, AnalyzedThreadClass> Summaries => m_AnalyzedThreadSummaries;

	public int SummaryCount => m_AnalyzedThreadSummaries.Count;

	public void Add(AnalyzedThreadClass newAnalyzedThread)
	{
		AnalyzedThreadClass analyzedThreadClass = null;
		string text = null;
		if (!m_AnalyzedThreadCollection.ContainsKey(newAnalyzedThread.Thread.ThreadID))
		{
			m_AnalyzedThreadCollection.Add(newAnalyzedThread.Thread.ThreadID, newAnalyzedThread);
		}
		if (!(newAnalyzedThread.Category != "OK"))
		{
			return;
		}
		text = newAnalyzedThread.Category + "::" + newAnalyzedThread.KeyPartOne + "::" + newAnalyzedThread.KeyPartTwo;
		if (!m_AnalyzedThreadSummaries.ContainsKey(text))
		{
			m_AnalyzedThreadSummaries.Add(text, newAnalyzedThread);
			analyzedThreadClass = newAnalyzedThread;
		}
		else
		{
			analyzedThreadClass = m_AnalyzedThreadSummaries[text];
			analyzedThreadClass.AddCount(newAnalyzedThread.Weight, 1);
			analyzedThreadClass.AddCount(newAnalyzedThread.BlockedASPCount, 2);
			analyzedThreadClass.AddCount(newAnalyzedThread.BlockedClientConnCount, 3);
		}
		if (!analyzedThreadClass.BlockedThreads.ContainsKey(newAnalyzedThread.Thread.ThreadID))
		{
			analyzedThreadClass.BlockedThreads.Add(newAnalyzedThread.Thread.ThreadID, newAnalyzedThread.Thread);
			if (analyzedThreadClass.BlockedThreads.Count > 5 && !analyzedThreadClass.IsWarning)
			{
				analyzedThreadClass.IsWarning = true;
			}
		}
	}

	public AnalyzedThreadClass Item(int ThreadNum)
	{
		return m_AnalyzedThreadCollection[ThreadNum];
	}

	public bool Exists(int ThreadNum)
	{
		return m_AnalyzedThreadCollection.ContainsKey(ThreadNum);
	}
}
