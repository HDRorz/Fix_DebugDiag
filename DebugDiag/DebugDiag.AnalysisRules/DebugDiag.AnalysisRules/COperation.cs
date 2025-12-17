using System;
using System.Collections.Generic;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public class COperation : IHasTopUserFunctions
{
	private int m_opType;

	private string m_key;

	private Dictionary<int, CacheFunctions.ScriptThreadClass> m_colThreadsByDumpNumber;

	private Dictionary<int, Dictionary<int, CRelevantStackFrame>> m_colRelevantStackFramessByTime;

	private Dictionary<int, object> m_colvDatasByTime;

	private double m_durationMin;

	private double m_durationMax;

	private int m_firstDumpNum;

	private int m_lastDumpNum;

	private Dictionary<string, int[]> m_FunctionsByHitCount;

	private double m_AvgCPU;

	private double m_MaxCPU;

	private int m_TopUserFunctionWatermark;

	private bool m_bSpansThreads;

	private bool m_bDurationInited;

	public int FirstDumpNumber => m_firstDumpNum;

	public int LastDumpNumber => m_lastDumpNum;

	public bool SpansThreads => m_bSpansThreads;

	public string TopFunctionName
	{
		get
		{
			using (Dictionary<string, int[]>.KeyCollection.Enumerator enumerator = FunctionsByHitCount.Keys.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			return "";
		}
	}

	public double MaxCPU
	{
		get
		{
			double num = 0.0;
			int secondDumpNumber = 0;
			if (m_MaxCPU.Equals(0.0))
			{
				if (m_colThreadsByDumpNumber.Count > 1)
				{
					foreach (int key in m_colThreadsByDumpNumber.Keys)
					{
						if (!secondDumpNumber.Equals(0))
						{
							num = GetCPUPercentage(key, secondDumpNumber);
							if (num > m_MaxCPU)
							{
								m_MaxCPU = num;
							}
						}
						secondDumpNumber = key;
					}
				}
				else
				{
					m_MaxCPU = 0.0;
				}
			}
			return m_MaxCPU;
		}
	}

	public double AvgCPU
	{
		get
		{
			if (m_AvgCPU.Equals(0.0))
			{
				if (m_colThreadsByDumpNumber.Count > 1)
				{
					m_AvgCPU = GetCPUPercentage(m_firstDumpNum, m_lastDumpNum);
				}
				else
				{
					m_AvgCPU = 0.0;
				}
			}
			return m_AvgCPU;
		}
	}

	public Dictionary<string, int[]> FunctionsByHitCount
	{
		get
		{
			if (m_FunctionsByHitCount.Count == 0 && m_colThreadsByDumpNumber.Count > 0)
			{
				BuildFunctionsByHitCount();
			}
			return m_FunctionsByHitCount;
		}
	}

	public int OpType => m_opType;

	public string Key => m_key;

	public Dictionary<int, Dictionary<int, CRelevantStackFrame>> RelevantStackFramessByTime => m_colRelevantStackFramessByTime;

	public Dictionary<int, object> vDatasByTime => m_colvDatasByTime;

	public Dictionary<int, CacheFunctions.ScriptThreadClass> ThreadsByDumpNumber => m_colThreadsByDumpNumber;

	public double DurationMin
	{
		get
		{
			if (!m_bDurationInited)
			{
				InitDurationMinMax();
			}
			return m_durationMin;
		}
	}

	public double DurationMax
	{
		get
		{
			if (!m_bDurationInited)
			{
				InitDurationMinMax();
			}
			return m_durationMax;
		}
	}

	public string TypeString => Globals.g_AllOperations.TypeStrings[m_opType];

	public string Name
	{
		get
		{
			string text = "";
			IASPRequest iASPRequest = null;
			CacheFunctions.ScriptThreadClass scriptThreadClass = null;
			using (Dictionary<int, CacheFunctions.ScriptThreadClass>.ValueCollection.Enumerator enumerator = m_colThreadsByDumpNumber.Values.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					scriptThreadClass = enumerator.Current;
				}
			}
			text = "Thread " + Convert.ToString(scriptThreadClass.SystemID);
			switch (m_opType)
			{
			case 2:
				if (m_colvDatasByTime[m_lastDumpNum] != null && m_colvDatasByTime[m_lastDumpNum] != null)
				{
					iASPRequest = (IASPRequest)m_colvDatasByTime[m_lastDumpNum];
					text = AspPageFromAspRequest(iASPRequest) + " - " + text;
				}
				break;
			case 3:
				text = "ASP.NET Request - " + text;
				break;
			}
			return text;
		}
	}

	public string DumpsPresent
	{
		get
		{
			int num = 0;
			string text = "";
			if (m_lastDumpNum == 0)
			{
				InitFirstLastDumpNums();
			}
			num = m_colThreadsByDumpNumber.Count;
			if (num == Globals.g_dumps.Count)
			{
				return "All dumps";
			}
			return num switch
			{
				1 => "Dump " + m_firstDumpNum, 
				2 => "Dumps " + m_firstDumpNum + " and " + m_lastDumpNum, 
				_ => "Dumps " + m_firstDumpNum + " through " + m_lastDumpNum, 
			};
		}
	}

	public COperation()
	{
		m_colThreadsByDumpNumber = new Dictionary<int, CacheFunctions.ScriptThreadClass>();
		m_colRelevantStackFramessByTime = new Dictionary<int, Dictionary<int, CRelevantStackFrame>>();
		m_colvDatasByTime = new Dictionary<int, object>();
		m_FunctionsByHitCount = new Dictionary<string, int[]>();
		m_opType = 0;
	}

	public double ValueByValueType(int valueType)
	{
		double result = 0.0;
		switch (valueType)
		{
		case 2:
			result = MaxCPU;
			break;
		case 1:
			result = AvgCPU;
			break;
		case 4:
			result = Convert.ToDouble(DurationMax);
			break;
		}
		return result;
	}

	private string AspPageFromAspRequest(IASPRequest aspRequest)
	{
		int num = 0;
		string text = "";
		text = aspRequest.VirtualPath;
		num = text.IndexOf("/");
		if (num >= 0)
		{
			text = text.Substring(num + 1);
		}
		return text;
	}

	private string AspxPageFromAspxRequest(object aspxRequest)
	{
		int num = 0;
		string text = "";
		num = text.IndexOf("/");
		if (num >= 0)
		{
			text = text.Substring(num + 1);
		}
		return text;
	}

	private double GetCPUPercentage(int firstDumpNumber, int secondDumpNumber)
	{
		double num = 0.0;
		num = GetTimeDelta(firstDumpNumber, secondDumpNumber) * 1000.0;
		if (num == 0.0)
		{
			return 0.0;
		}
		return (double)GetCPUDelta(firstDumpNumber, secondDumpNumber) / num * 100.0;
	}

	private double GetTimeDelta(int firstDumpNumber, int secondDumpNumber)
	{
		return Convert.ToDouble(Globals.g_dumps.DumpBySortedDumpNumber(secondDumpNumber).ProcessUpTime) - Convert.ToDouble(Globals.g_dumps.DumpBySortedDumpNumber(firstDumpNumber).ProcessUpTime);
	}

	private int GetCPUDelta(int firstDumpNumber, int secondDumpNumber)
	{
		int num = 0;
		int Days = 0;
		int Hours = 0;
		int Minutes = 0;
		int Seconds = 0;
		int MilliSeconds = 0;
		m_colThreadsByDumpNumber[secondDumpNumber].GetUserTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
		if (Convert.ToBoolean(Days))
		{
			num = Convert.ToInt32(Days) * 24 * 60 * 60 * 1000;
		}
		if (Convert.ToBoolean(Hours))
		{
			num += Convert.ToInt32(Hours) * 60 * 60 * 1000;
		}
		if (Convert.ToBoolean(Minutes))
		{
			num += Convert.ToInt32(Minutes) * 60 * 1000;
		}
		if (Convert.ToBoolean(Seconds))
		{
			num += Convert.ToInt32(Seconds) * 1000;
		}
		num += Convert.ToInt32(MilliSeconds);
		m_colThreadsByDumpNumber[firstDumpNumber].GetUserTime(out Days, out Hours, out Minutes, out Seconds, out MilliSeconds);
		if (Convert.ToBoolean(Days))
		{
			num -= Convert.ToInt32(Days) * 24 * 60 * 60 * 1000;
		}
		if (Convert.ToBoolean(Hours))
		{
			num -= Convert.ToInt32(Hours) * 60 * 60 * 1000;
		}
		if (Convert.ToBoolean(Minutes))
		{
			num -= Convert.ToInt32(Minutes) * 60 * 1000;
		}
		if (Convert.ToBoolean(Seconds))
		{
			num -= Convert.ToInt32(Seconds) * 1000;
		}
		return num - Convert.ToInt32(MilliSeconds);
	}

	public void GetvDataByTime(int dumpNumber, ref object vData)
	{
		if (m_colvDatasByTime[dumpNumber] != null)
		{
			vData = m_colvDatasByTime[dumpNumber];
		}
		else
		{
			vData = m_colvDatasByTime[dumpNumber];
		}
	}

	private void BuildFunctionsByHitCount()
	{
		CRelevantStackFrame cRelevantStackFrame = null;
		Dictionary<int, CRelevantStackFrame> dictionary = null;
		HashSet<string> hashSet = null;
		string text = null;
		int num = 0;
		int num2 = 0;
		hashSet = new HashSet<string>();
		foreach (int key in m_colRelevantStackFramessByTime.Keys)
		{
			dictionary = m_colRelevantStackFramessByTime[key];
			foreach (int key2 in dictionary.Keys)
			{
				cRelevantStackFrame = dictionary[key2];
				if (cRelevantStackFrame.Priority <= 1)
				{
					continue;
				}
				text = cRelevantStackFrame.FunctionNameNoOffset;
				if (!hashSet.Contains(text))
				{
					hashSet.Add(text);
					if (m_FunctionsByHitCount.ContainsKey(text))
					{
						int[] array = m_FunctionsByHitCount[text];
						num = array[0];
						num2 = array[1];
						m_FunctionsByHitCount[text] = new int[2]
						{
							num + 1,
							num2
						};
					}
					else
					{
						m_FunctionsByHitCount.Add(text, new int[2] { 1, cRelevantStackFrame.Priority });
					}
				}
			}
			hashSet.Clear();
		}
		SortFunctionsByHitCountAndSetWatermark();
	}

	private void SortFunctionsByHitCountAndSetWatermark()
	{
		Dictionary<string, int[]> dictionary = null;
		Dictionary<string, int[]> dictionary2 = null;
		string text = "";
		int num = 0;
		int num2 = 0;
		bool flag = false;
		int num3 = 0;
		int num4 = 0;
		int[] array = null;
		int num5 = 0;
		if (m_FunctionsByHitCount.Count <= 1)
		{
			return;
		}
		dictionary2 = m_FunctionsByHitCount;
		dictionary = new Dictionary<string, int[]>();
		while (dictionary2.Count > 0)
		{
			text = "-1";
			foreach (string key in dictionary2.Keys)
			{
				array = dictionary2[key];
				num3 = array[0];
				num4 = array[1];
				flag = false;
				if (text == "-1")
				{
					flag = true;
				}
				else if (num3 > num)
				{
					flag = true;
				}
				else if (num3 == num && num4 > num2)
				{
					flag = true;
				}
				if (flag)
				{
					num = num3;
					text = key;
					num2 = num4;
				}
			}
			array = dictionary2[text];
			dictionary.Add(text, array);
			if (array[1] == 3 && num5 < 10)
			{
				num5++;
				m_TopUserFunctionWatermark = num;
			}
			dictionary2.Remove(text);
		}
		m_FunctionsByHitCount = dictionary;
	}

	public void BeginInit(int opType, string key, CacheFunctions.ScriptThreadClass thread, IASPRequest vData, int dumpNumber)
	{
		m_opType = opType;
		m_key = key;
		m_colThreadsByDumpNumber.Add(dumpNumber, thread);
		m_colRelevantStackFramessByTime.Add(dumpNumber, GetRelevantStackFrames(thread, vData));
		m_colvDatasByTime.Add(dumpNumber, vData);
	}

	public void ShowStats(string title, string tip)
	{
		COperations cOperations = new COperations();
		cOperations.AddOperation(this, -1);
		cOperations.ShowStatsEx(title, tip, collapsed: true, Key + "-stats");
	}

	private void InitFirstLastDumpNums()
	{
		foreach (int key in m_colThreadsByDumpNumber.Keys)
		{
			if (m_firstDumpNum.Equals(0))
			{
				m_firstDumpNum = key;
			}
			m_lastDumpNum = key;
		}
	}

	public void GetCustomInfoReport()
	{
		IASPRequest iASPRequest = null;
		int opType = m_opType;
		if (opType == 2)
		{
			iASPRequest = (IASPRequest)m_colvDatasByTime[m_lastDumpNum];
			Globals.Manager.Write("<tr><td><b>" + Convert.ToString(iASPRequest.Method) + "</b> request for</td><td>&nbsp;&nbsp;<b>" + iASPRequest.VirtualPath + "</b></td></tr>");
			Globals.Manager.Write("<tr><td>QueryString</td><td>&nbsp;&nbsp;" + iASPRequest.QueryString + "</td></tr>");
			Globals.Manager.Write("<tr><td>Request mapped to</td><td>&nbsp;&nbsp;" + iASPRequest.PhysicalPath + "</td></tr>");
			if (iASPRequest.Application != null)
			{
				Globals.Manager.Write("<tr><td>ASP Application</td><td>&nbsp;&nbsp;" + iASPRequest.Application.MetabaseKey + "</td></tr>");
			}
		}
	}

	public void Uninit(ref CacheFunctions.ScriptThreadClass firstThread, ref Dictionary<int, CRelevantStackFrame> firstStackFrames, ref object firstvData)
	{
		int key = 0;
		using (Dictionary<int, CacheFunctions.ScriptThreadClass>.KeyCollection.Enumerator enumerator = m_colThreadsByDumpNumber.Keys.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				key = enumerator.Current;
			}
		}
		firstThread = m_colThreadsByDumpNumber[key];
		firstStackFrames = m_colRelevantStackFramessByTime[key];
		firstvData = m_colvDatasByTime[key];
		m_opType = -1;
		m_colThreadsByDumpNumber = null;
		m_colRelevantStackFramessByTime = null;
		m_colThreadsByDumpNumber = null;
	}

	public COperation WasFoundAgain(COperation operation, int dumpNumber)
	{
		CacheFunctions.ScriptThreadClass firstThread = null;
		Dictionary<int, CRelevantStackFrame> firstStackFrames = null;
		object firstvData = null;
		operation.Uninit(ref firstThread, ref firstStackFrames, ref firstvData);
		m_colThreadsByDumpNumber.Add(dumpNumber, firstThread);
		m_colRelevantStackFramessByTime.Add(dumpNumber, firstStackFrames);
		m_colvDatasByTime.Add(dumpNumber, firstvData);
		return this;
	}

	private void InitDurationMinMax()
	{
		m_bDurationInited = true;
		IASPRequest iASPRequest = null;
		if (m_firstDumpNum.Equals(0))
		{
			InitFirstLastDumpNums();
		}
		switch (m_opType)
		{
		case 2:
			iASPRequest = (IASPRequest)m_colvDatasByTime[m_lastDumpNum];
			m_durationMin = Convert.ToDouble(iASPRequest.SecondsAlive);
			m_durationMax = m_durationMin;
			return;
		case 3:
			return;
		}
		m_durationMin = 0.0;
		m_durationMax = 0.0;
		if (m_colThreadsByDumpNumber.Count > 1)
		{
			m_durationMin = Globals.g_dumps.DumpBySortedDumpNumber(m_lastDumpNum).ProcessUpTime - Globals.g_dumps.DumpBySortedDumpNumber(m_firstDumpNum).ProcessUpTime;
			if (m_firstDumpNum > 1 && m_lastDumpNum < Globals.g_dumps.Count)
			{
				m_durationMax = Globals.g_dumps.DumpBySortedDumpNumber(m_lastDumpNum + 1).ProcessUpTime - Globals.g_dumps.DumpBySortedDumpNumber(m_firstDumpNum - 1).ProcessUpTime;
			}
		}
	}

	private Dictionary<int, CRelevantStackFrame> GetRelevantStackFrames(CacheFunctions.ScriptThreadClass thread, IASPRequest vData)
	{
		int num = 0;
		Dictionary<int, CRelevantStackFrame> dictionary = null;
		IASPRequest iASPRequest = null;
		int num2 = 0;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary2 = null;
		Dictionary<int, CacheFunctions.ScriptStackFrameClass> dictionary3 = null;
		int num3 = 0;
		dictionary = new Dictionary<int, CRelevantStackFrame>();
		switch (m_opType)
		{
		case 2:
			iASPRequest = vData;
			if (iASPRequest.Count > 0)
			{
				dictionary2 = new Dictionary<int, CacheFunctions.ScriptStackFrameClass>();
				dictionary3 = thread.StackFrames;
				num2 = -1;
				do
				{
					num2 = thread.FindFrameInStackStartFrom("VBSCRIPT!CSCRIPTRUNTIME::RUN", num2 + 1);
					if (num2 == -1)
					{
						break;
					}
					dictionary2.Add(num2, dictionary3[num2]);
				}
				while (dictionary2.Count <= Convert.ToInt32(iASPRequest.Count));
				if (dictionary2.Count == Convert.ToInt32(iASPRequest.Count))
				{
					for (num = 0; num <= dictionary3.Count - 1; num++)
					{
						if (dictionary2.ContainsKey(num))
						{
							dictionary.Add(dictionary.Count, GetRelevantStackFrameObjFromScriptFrame(iASPRequest[num3], AspPageFromAspRequest(iASPRequest)));
							num3++;
						}
						dictionary.Add(dictionary.Count, GetCRelevantStackFrameFromDbgStackFrame(dictionary3[num]));
					}
					break;
				}
				num2 = thread.FindFrameInStack("VBSCRIPT");
				if (num2 != -1)
				{
					dictionary = null;
					dictionary = new Dictionary<int, CRelevantStackFrame>();
					for (num = 0; num <= Convert.ToInt32(num2) - 1; num++)
					{
						dictionary.Add(dictionary.Count, GetCRelevantStackFrameFromDbgStackFrame(dictionary3[num]));
					}
				}
				foreach (IScriptFrame item in iASPRequest)
				{
					dictionary.Add(dictionary.Count, GetRelevantStackFrameObjFromScriptFrame(item, AspPageFromAspRequest(iASPRequest)));
				}
				if (num2 != -1)
				{
					for (num = num2; num <= Convert.ToInt32(dictionary3.Count) - 1; num++)
					{
						dictionary.Add(dictionary.Count, GetCRelevantStackFrameFromDbgStackFrame(dictionary3[num]));
					}
				}
			}
			else
			{
				for (num = 0; num <= Convert.ToInt32(dictionary3.Count) - 1; num++)
				{
					dictionary.Add(dictionary.Count, GetCRelevantStackFrameFromDbgStackFrame(dictionary3[num]));
				}
			}
			break;
		case 1:
		case 3:
			if (Convert.ToBoolean(Globals.AnalyzeManaged.IsClrExtensionExecuting()))
			{
				AddClrFramesToRelevantStackFrames(thread, vData, dictionary);
			}
			if (!Globals.AnalyzeManaged.IsClrExtensionExecuting() || Globals.g_DoCombinedNativeMangedPerfAnalysis)
			{
				dictionary3 = thread.StackFrames;
				for (num = 0; num <= Convert.ToInt32(dictionary3.Count) - 1; num++)
				{
					dictionary.Add(dictionary.Count, GetCRelevantStackFrameFromDbgStackFrame(dictionary3[num]));
				}
			}
			break;
		}
		return dictionary;
	}

	private void AddClrFramesToRelevantStackFrames(CacheFunctions.ScriptThreadClass thread, IASPRequest vData, Dictionary<int, CRelevantStackFrame> relevantStackFrames)
	{
		string[] array = null;
		int num = 0;
		int num2 = 0;
		CRelevantStackFrame cRelevantStackFrame = null;
		string text = "";
		int num3 = 0;
		bool flag = false;
		string text2 = "";
		text2 = Convert.ToString(thread.ClrStackReportNoArgsNoColor);
		if (!(text2 != ""))
		{
			return;
		}
		text2 = Globals.HelperFunctions.Replace(text2, "</th></tr>", "</th></tr>" + Convert.ToString('\n'));
		array = Globals.HelperFunctions.Split(text2, Convert.ToString('\n'));
		for (num = 0; num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num++)
		{
			if (array[num].GetSafeLength() <= 15 || array[num].IndexOf("<tr><td nowrap>") != 0)
			{
				continue;
			}
			num2 = array[num].IndexOf("</td>", 16);
			if (num2 < 17)
			{
				continue;
			}
			text = array[num].Substring(15, num2 - 16);
			cRelevantStackFrame = new CRelevantStackFrame();
			num3 = 3;
			if (IsClrSystemFunction(text))
			{
				num3 = 2;
				if (Globals.g_AllOperations.BoilerPlateFunctionsByOpType.ContainsKey(m_opType) && Globals.g_AllOperations.BoilerPlateFunctionsByOpType[m_opType].ContainsKey(text.ToUpper()))
				{
					num3 = 1;
				}
			}
			flag = true;
			cRelevantStackFrame.Init(text, num3, flag);
			relevantStackFrames.Add(relevantStackFrames.Count, cRelevantStackFrame);
		}
	}

	private bool IsClrSystemFunction(string functionName)
	{
		if (functionName.StartsWith("[["))
		{
			return true;
		}
		if (functionName.IndexOf("System.") == 0 || functionName.Contains("!System."))
		{
			return true;
		}
		if (functionName.IndexOf("Microsoft.") == 0 || functionName.Contains("!Microsoft."))
		{
			return true;
		}
		if (functionName.IndexOf("DomainNeutralILStubClass.") == 0 || functionName.Contains("!DomainNeutralILStubClass."))
		{
			return true;
		}
		return false;
	}

	private CRelevantStackFrame GetCRelevantStackFrameFromDbgStackFrame(CacheFunctions.ScriptStackFrameClass dbgStackFrame)
	{
		int num = 0;
		string text = null;
		string text2 = "";
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		bool hasSymbols = false;
		num = 3;
		CRelevantStackFrame cRelevantStackFrame = new CRelevantStackFrame();
		text = CacheFunctions.GetSymbolFromAddressIEReplaced(dbgStackFrame);
		if (IsSystemFrame(dbgStackFrame))
		{
			num = 2;
			if (Globals.g_AllOperations.BoilerPlateFunctionsByOpType.ContainsKey(m_opType))
			{
				text2 = Globals.HelperFunctions.Split(Convert.ToString(text), "+", -1)[0];
				if (Globals.g_AllOperations.BoilerPlateFunctionsByOpType[m_opType].ContainsKey(text2.ToUpper()))
				{
					num = 1;
				}
				else if (text2.ToUpper().IndexOf("WAITFOR") > -1)
				{
					num = 1;
				}
			}
		}
		scriptModuleClass = Globals.g_ModuleCache.ItemByAddress(dbgStackFrame.InstructionAddress);
		if (scriptModuleClass != null)
		{
			hasSymbols = scriptModuleClass.HasGoodSymbols;
		}
		cRelevantStackFrame.Init(text, num, hasSymbols);
		return cRelevantStackFrame;
	}

	private bool IsSystemFrame(CacheFunctions.ScriptStackFrameClass dbgStackFrame)
	{
		CacheFunctions.ScriptModuleClass scriptModuleClass = null;
		string text = "";
		scriptModuleClass = Globals.g_ModuleCache.ItemByAddress(dbgStackFrame.InstructionAddress);
		if (scriptModuleClass != null)
		{
			text = scriptModuleClass.VSCompanyName.ToUpper();
			if (!(text == "") && text.Length >= 10 && text.Substring(0, 10) == "MICROSOFT " && text.Substring(text.GetSafeLength() - 12) == " CORPORATION")
			{
				return true;
			}
		}
		return false;
	}

	private CRelevantStackFrame GetRelevantStackFrameObjFromScriptFrame(IScriptFrame scriptFrame, string pageName)
	{
		CRelevantStackFrame cRelevantStackFrame = new CRelevantStackFrame();
		cRelevantStackFrame.Init(GetScriptfunctionName(scriptFrame, pageName), 3, hasSymbols: true);
		return cRelevantStackFrame;
	}

	private string GetScriptfunctionName(IScriptFrame scriptFrame, string pageName)
	{
		string text = Convert.ToString(scriptFrame.FunctionName);
		if (text.GetSafeLength() == 0)
		{
			if (pageName != "")
			{
				pageName += " - ";
			}
			text = pageName + "Global/Page Scope";
		}
		return "<i>[VBScript]</i>&nbsp;&nbsp;&nbsp;" + Truncate(text, 30);
	}

	public bool IsTopUserFunction(string fnName)
	{
		int num = 0;
		bool result = false;
		if (m_FunctionsByHitCount.ContainsKey(fnName))
		{
			int[] array = m_FunctionsByHitCount[fnName];
			num = array[0];
			if (array[1] == 3 && num >= m_TopUserFunctionWatermark)
			{
				result = true;
			}
		}
		return result;
	}

	private string Truncate(string str, int size)
	{
		string text = "";
		if (str.GetSafeLength() > size)
		{
			return str.Substring(0, size);
		}
		return str;
	}
}
