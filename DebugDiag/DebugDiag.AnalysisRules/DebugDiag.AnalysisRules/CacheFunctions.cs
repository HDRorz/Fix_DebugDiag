using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CrashHangExtLib;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;

namespace DebugDiag.AnalysisRules;

public static class CacheFunctions
{
	public class ScriptThreadClass
	{
		public NetDbgThread m_dbgThread;

		private Guid m_ThreadHash = Guid.Empty;

		private int? m_ThreadID;

		private int? m_SystemID;

		private double? m_StartAddress;

		private DateTime? m_CreateTime;

		private Dictionary<int, ScriptStackFrameClass> m_StackFrames;

		private bool? m_bHasAllGoodSymbols;

		private ScriptModuleClass m_FirstISAPIDLL;

		private bool? m_bHasValidTeb;

		private double? m_InstructionAddress;

		private double? m_StackAddress;

		private double? m_FrameAddress;

		private TimeSpan? m_kernelTime;

		private TimeSpan? m_userTime;

		private Dictionary<string, double> m_Register;

		private double? m_WaitingOnCritSecAddr;

		private int? m_COMDestinationProcessID;

		private int? m_COMDestinationThreadID;

		private string m_SocketSourceAddress = string.Empty;

		private string m_SocketDestinationAddress = string.Empty;

		private string m_RpcSourceBindings = string.Empty;

		private string m_RpcDestinationBindings = string.Empty;

		private string m_StackReportNoArgs = string.Empty;

		private string m_StackReportWithArgs = string.Empty;

		private string m_ClrStackReportNoArgs = string.Empty;

		private string m_ClrStackReportNoArgsNoColor = string.Empty;

		private string m_ClrStackDoubleQuoteSeparated = string.Empty;

		private string m_NativeStackDoubleQuoteSeparatedNoDupes = string.Empty;

		private string m_ClrStackSearch = string.Empty;

		private string m_SearchString = string.Empty;

		private string m_StartAddressSymbol = string.Empty;

		internal string m_ClrSearchString = string.Empty;

		internal bool HashStack;

		public int ThreadID
		{
			get
			{
				if (!m_ThreadID.HasValue)
				{
					m_ThreadID = m_dbgThread.ThreadID;
				}
				return m_ThreadID.Value;
			}
		}

		public int SystemID
		{
			get
			{
				if (!m_SystemID.HasValue)
				{
					m_SystemID = (int)m_dbgThread.SystemID;
				}
				return m_SystemID.Value;
			}
		}

		public double StartAddress
		{
			get
			{
				if (!m_StartAddress.HasValue)
				{
					m_StartAddress = m_dbgThread.StartAddress;
				}
				return m_StartAddress.Value;
			}
		}

		public string StartAddressSymbol
		{
			get
			{
				if (string.IsNullOrEmpty(m_StartAddressSymbol) && StartAddress != 0.0)
				{
					m_StartAddressSymbol = Globals.g_Debugger.GetSymbolFromAddress(StartAddress);
				}
				return m_StartAddressSymbol;
			}
		}

		public DateTime CreateTime
		{
			get
			{
				if (!m_CreateTime.HasValue)
				{
					m_CreateTime = m_dbgThread.CreateTime;
				}
				return m_CreateTime.Value;
			}
		}

		public Dictionary<int, ScriptStackFrameClass> StackFrames
		{
			get
			{
				if (m_StackFrames == null)
				{
					LoadCommonStackItems();
				}
				return m_StackFrames;
			}
		}

		public double InstructionAddress
		{
			get
			{
				if (!m_InstructionAddress.HasValue)
				{
					m_InstructionAddress = m_dbgThread.InstructionAddress;
				}
				return m_InstructionAddress.Value;
			}
		}

		public double StackAddress
		{
			get
			{
				if (!m_StackAddress.HasValue)
				{
					m_StackAddress = m_dbgThread.StackAddress;
				}
				return m_StackAddress.Value;
			}
		}

		public double FrameAddress
		{
			get
			{
				if (!m_FrameAddress.HasValue)
				{
					m_FrameAddress = m_dbgThread.FrameAddress;
				}
				return m_FrameAddress.Value;
			}
		}

		public double WaitingOnCritSecAddr
		{
			get
			{
				if (!m_WaitingOnCritSecAddr.HasValue)
				{
					m_WaitingOnCritSecAddr = m_dbgThread.WaitingOnCritSecAddr;
				}
				return m_WaitingOnCritSecAddr.Value;
			}
		}

		public int COMDestinationProcessID
		{
			get
			{
				if (!m_COMDestinationProcessID.HasValue)
				{
					m_COMDestinationProcessID = m_dbgThread.COMDestinationProcessID;
				}
				return m_COMDestinationProcessID.Value;
			}
		}

		public int COMDestinationThreadID
		{
			get
			{
				if (!m_COMDestinationThreadID.HasValue)
				{
					m_COMDestinationThreadID = m_dbgThread.COMDestinationThreadID;
				}
				return m_COMDestinationThreadID.Value;
			}
		}

		public string SocketDestinationAddress
		{
			get
			{
				if (string.IsNullOrEmpty(m_SocketDestinationAddress))
				{
					m_SocketDestinationAddress = m_dbgThread.SocketDestinationAddress;
				}
				return m_SocketDestinationAddress;
			}
		}

		public string SocketSourceAddress
		{
			get
			{
				if (string.IsNullOrEmpty(m_SocketSourceAddress))
				{
					m_SocketSourceAddress = m_dbgThread.SocketSourceAddress;
				}
				return m_SocketSourceAddress;
			}
		}

		public string RpcDestinationBindings
		{
			get
			{
				if (string.IsNullOrEmpty(m_RpcDestinationBindings))
				{
					m_RpcDestinationBindings = m_dbgThread.RpcDestinationBindings;
				}
				return m_RpcDestinationBindings;
			}
		}

		public string RpcSourceBindings
		{
			get
			{
				if (string.IsNullOrEmpty(m_RpcSourceBindings))
				{
					m_RpcSourceBindings = m_dbgThread.RpcSourceBindings;
				}
				return m_RpcSourceBindings;
			}
		}

		public bool HasAllGoodSymbols
		{
			get
			{
				if (!(!m_bHasAllGoodSymbols).HasValue)
				{
					LoadCommonStackItems();
				}
				return m_bHasAllGoodSymbols.Value;
			}
		}

		public ScriptModuleClass FirstISAPIDLL
		{
			get
			{
				if (m_FirstISAPIDLL == null)
				{
					LoadCommonStackItems();
				}
				return m_FirstISAPIDLL;
			}
		}

		public bool HasValidTeb
		{
			get
			{
				if (!m_bHasValidTeb.HasValue)
				{
					m_bHasValidTeb = ((IUtils)Globals.g_UtilExt).get_IsTEBValid((uint)SystemID);
				}
				return m_bHasValidTeb.Value;
			}
		}

		public Guid ThreadHash => m_ThreadHash;

		public string StackReportNoArgs
		{
			get
			{
				if (string.IsNullOrEmpty(m_StackReportNoArgs))
				{
					LoadCommonStackItems();
				}
				return m_StackReportNoArgs;
			}
		}

		public string NativeStackDoubleQuoteSeparatedNoDupes
		{
			get
			{
				if (string.IsNullOrEmpty(m_NativeStackDoubleQuoteSeparatedNoDupes))
				{
					LoadStackReportWithArgs();
				}
				return m_NativeStackDoubleQuoteSeparatedNoDupes;
			}
		}

		public string StackReportWithArgs
		{
			get
			{
				if (string.IsNullOrEmpty(m_StackReportWithArgs))
				{
					LoadStackReportWithArgs();
				}
				return m_StackReportWithArgs;
			}
		}

		public string ClrStackReportNoArgs
		{
			get
			{
				if (string.IsNullOrEmpty(m_ClrStackReportNoArgs))
				{
					LoadClrStackReportNoArgs();
				}
				return m_ClrStackReportNoArgs;
			}
		}

		public string ClrStackReportNoArgsNoColor
		{
			get
			{
				if (string.IsNullOrEmpty(m_ClrStackReportNoArgsNoColor))
				{
					LoadClrStackReportNoArgs();
				}
				return m_ClrStackReportNoArgsNoColor;
			}
		}

		public string ClrStackDoubleQuoteSeparated
		{
			get
			{
				if (string.IsNullOrEmpty(m_ClrStackDoubleQuoteSeparated))
				{
					LoadClrStackReportNoArgs();
				}
				return m_ClrStackDoubleQuoteSeparated;
			}
		}

		public string ClrStackSearch
		{
			get
			{
				if (string.IsNullOrEmpty(m_ClrStackSearch))
				{
					LoadClrStackReportNoArgs();
				}
				return m_ClrStackSearch;
			}
		}

		public bool Init(int nThreadID)
		{
			try
			{
				m_dbgThread = Globals.g_Debugger.Threads[nThreadID];
				if (m_dbgThread != null)
				{
					m_ThreadID = nThreadID;
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
			return false;
		}

		public bool InitBySystemID(int nSystemId)
		{
			try
			{
				m_dbgThread = Globals.g_Debugger.GetThreadBySystemID(nSystemId);
				if (m_dbgThread != null)
				{
					m_SystemID = nSystemId;
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
			return false;
		}

		public void GetUserTime(out int Days, out int Hours, out int Minutes, out int Seconds, out int MilliSeconds)
		{
			if (m_userTime.HasValue)
			{
				Days = m_userTime.Value.Days;
				Hours = m_userTime.Value.Hours;
				Minutes = m_userTime.Value.Minutes;
				Seconds = m_userTime.Value.Seconds;
				MilliSeconds = m_userTime.Value.Milliseconds;
			}
			else
			{
				object obj = default(object);
				object obj2 = default(object);
				object obj3 = default(object);
				object obj4 = default(object);
				object obj5 = default(object);
				m_dbgThread.GetUserTime(ref obj, ref obj2, ref obj3, ref obj4, ref obj5);
				Days = (int)obj;
				Hours = (int)obj2;
				Minutes = (int)obj3;
				Seconds = (int)obj4;
				MilliSeconds = (int)obj5;
				m_userTime = new TimeSpan(Days, Hours, Minutes, Seconds, MilliSeconds);
			}
		}

		public void GetKernelTime(out int Days, out int Hours, out int Minutes, out int Seconds, out int MilliSeconds)
		{
			if (m_kernelTime.HasValue)
			{
				Days = m_kernelTime.Value.Days;
				Hours = m_kernelTime.Value.Hours;
				Minutes = m_kernelTime.Value.Minutes;
				Seconds = m_kernelTime.Value.Seconds;
				MilliSeconds = m_kernelTime.Value.Milliseconds;
			}
			else
			{
				object obj = default(object);
				object obj2 = default(object);
				object obj3 = default(object);
				object obj4 = default(object);
				object obj5 = default(object);
				m_dbgThread.GetKernelTime(ref obj, ref obj2, ref obj3, ref obj4, ref obj5);
				Days = (int)obj;
				Hours = (int)obj2;
				Minutes = (int)obj3;
				Seconds = (int)obj4;
				MilliSeconds = (int)obj5;
				m_kernelTime = new TimeSpan(Days, Hours, Minutes, Seconds, MilliSeconds);
			}
		}

		public bool ChangeThreadContext(ref double fContextAdress)
		{
			return m_dbgThread.ChangeThreadContext(fContextAdress);
		}

		public void FlushStackFrames()
		{
			m_dbgThread.FlushStackFrames();
			m_StackReportNoArgs = null;
			m_StackReportWithArgs = null;
			m_SearchString = null;
			m_ClrSearchString = null;
			m_StackFrames = null;
		}

		public double Register(string sRegisterName)
		{
			if (m_Register == null)
			{
				m_Register = new Dictionary<string, double>();
			}
			if (!m_Register.ContainsKey(sRegisterName))
			{
				double num = m_dbgThread[sRegisterName];
				m_Register.Add(sRegisterName, num);
				return num;
			}
			return m_Register[sRegisterName];
		}

		private void Class_Initialize()
		{
			m_ThreadID = -1;
			m_SystemID = 0;
		}

		private void LoadCommonStackItems()
		{
			if (!string.IsNullOrEmpty(m_SearchString))
			{
				return;
			}
			string text = "";
			string text2 = "";
			byte[] array = new byte[16];
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			Dictionary<int, ScriptStackFrameClass> dictionary = new Dictionary<int, ScriptStackFrameClass>();
			Dictionary<string, ScriptModuleClass> dictionary2 = new Dictionary<string, ScriptModuleClass>();
			m_bHasAllGoodSymbols = true;
			m_FirstISAPIDLL = null;
			bool sourceInfoEnabled = Globals.Manager.SourceInfoEnabled;
			if (sourceInfoEnabled)
			{
				text = "<th>&nbsp;&nbsp;Source</th>";
			}
			bool instructionAddressEnabled = Globals.Manager.InstructionAddressEnabled;
			if (instructionAddressEnabled)
			{
				text2 = "<th nowrap>Instruction Address</th>";
			}
			stringBuilder.Append("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr>" + text2 + "<th></th>" + text + "</tr>");
			int num = 0;
			using (SHA1Managed sHA1Managed = new SHA1Managed())
			{
				sHA1Managed.Initialize();
				int num2 = 0;
				foreach (NetDbgStackFrame item in (List<NetDbgStackFrame>)(object)m_dbgThread.StackFrames)
				{
					num2++;
					ScriptStackFrameClass scriptStackFrameClass = new ScriptStackFrameClass();
					if (scriptStackFrameClass.Init(item, num))
					{
						double instructionAddress = scriptStackFrameClass.InstructionAddress;
						if (instructionAddress == 0.0)
						{
							continue;
						}
						dictionary.Add(num, scriptStackFrameClass);
						string symbolFromAddressIEReplaced = GetSymbolFromAddressIEReplaced(scriptStackFrameClass);
						byte[] bytes = BitConverter.GetBytes(instructionAddress);
						if (num2 != ((List<NetDbgStackFrame>)(object)m_dbgThread.StackFrames).Count)
						{
							sHA1Managed.TransformBlock(bytes, 0, 8, null, 0);
						}
						else
						{
							sHA1Managed.TransformFinalBlock(bytes, 0, 8);
						}
						if (sourceInfoEnabled)
						{
							text = "<td nowrap>&nbsp;&nbsp;" + GetSourceInfoFromAddress(scriptStackFrameClass.InstructionAddress) + "</td>";
						}
						if (instructionAddressEnabled && instructionAddress > 0.0)
						{
							text2 = $"<td nowrap>[0x{(long)instructionAddress:x8}]" + "</td>";
						}
						stringBuilder.Append("<tr>" + text2 + "<td nowrap>" + symbolFromAddressIEReplaced + "</td>" + text + "</tr>");
						symbolFromAddressIEReplaced = symbolFromAddressIEReplaced.ToUpper();
						if (m_bHasAllGoodSymbols.Value || m_FirstISAPIDLL == null)
						{
							ScriptModuleClass moduleFromAddress = GetModuleFromAddress(instructionAddress);
							if (moduleFromAddress == null)
							{
								if (Globals.g_OSVER > Globals.OS_VER_WIN2K && Convert.ToString(GetFunctionName(instructionAddress)).IndexOf("SHAREDUSERDATA") == -1)
								{
									m_bHasAllGoodSymbols = false;
								}
							}
							else
							{
								string key = Convert.ToString(moduleFromAddress.ModuleName).ToUpper();
								if (!Convert.ToBoolean(dictionary2.ContainsKey(key)))
								{
									dictionary2.Add(key, moduleFromAddress);
									if (m_FirstISAPIDLL == null && Convert.ToBoolean(moduleFromAddress.IsISAPIDLL))
									{
										m_FirstISAPIDLL = moduleFromAddress;
									}
									if (m_bHasAllGoodSymbols == true && !Convert.ToBoolean(moduleFromAddress.HasGoodSymbols))
									{
										m_bHasAllGoodSymbols = false;
									}
								}
							}
						}
						stringBuilder2.Append(MakeFrameNumMarker(num) + symbolFromAddressIEReplaced.ToUpper());
					}
					num++;
				}
				if (num2 > 0 && HashStack)
				{
					try
					{
						Array.Copy(sHA1Managed.Hash, array, 16);
						m_ThreadHash = new Guid(array);
					}
					catch
					{
						m_ThreadHash = Guid.NewGuid();
					}
				}
			}
			stringBuilder.Append("</table>");
			m_StackReportNoArgs = stringBuilder.ToString();
			m_SearchString = stringBuilder2.ToString();
			m_StackFrames = dictionary;
		}

		private void LoadStackReportWithArgs()
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder.AppendLine("    <table border=0 cellpadding=0 cellspacing=0 class=myCustomText>");
			stringBuilder.AppendLine("        <tr>");
			stringBuilder.AppendLine("            <th></th>");
			stringBuilder.AppendLine("             <th>&nbsp;&nbsp;&nbsp;&nbsp;Arg 1</th>");
			stringBuilder.AppendLine("             <th>&nbsp;&nbsp;&nbsp;&nbsp;Arg 2</th>");
			stringBuilder.AppendLine("             <th>&nbsp;&nbsp;&nbsp;&nbsp;Arg 3</th>");
			stringBuilder.AppendLine("             <th>&nbsp;&nbsp;&nbsp;&nbsp;Arg 4</th>");
			stringBuilder.AppendLine("             <th>&nbsp;&nbsp;Source</th>");
			stringBuilder.AppendLine("         </tr>");
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < StackFrames.Count; i++)
			{
				ScriptStackFrameClass scriptStackFrameClass = StackFrames[i];
				string functionNameNoUpper = Globals.HelperFunctions.GetFunctionNameNoUpper(scriptStackFrameClass.InstructionAddress);
				if (!hashSet.Contains(functionNameNoUpper))
				{
					hashSet.Add(functionNameNoUpper);
					stringBuilder2.Append("\"\"" + functionNameNoUpper + "\"\" ");
				}
				string stringToPass = Globals.HelperFunctions.Replace(scriptStackFrameClass.GetFrameText(includeOffset: true, Globals.Manager.SourceInfoEnabled), "<", "&lt");
				stringToPass = Globals.HelperFunctions.Replace(stringToPass, ">", "&gt");
				stringBuilder.AppendLine("        <tr>");
				stringBuilder.AppendLine("            <td nowrap>" + stringToPass + "</td>");
				stringBuilder.AppendLine("               " + GetArgHTMLOutput(scriptStackFrameClass, 0));
				stringBuilder.AppendLine("               " + GetArgHTMLOutput(scriptStackFrameClass, 1));
				stringBuilder.AppendLine("               " + GetArgHTMLOutput(scriptStackFrameClass, 2));
				stringBuilder.AppendLine("               " + GetArgHTMLOutput(scriptStackFrameClass, 3));
				stringBuilder.AppendLine("            <td nowrap>&nbsp;&nbsp;" + GetSourceInfoFromAddress(scriptStackFrameClass.InstructionAddress) + "</td>");
				stringBuilder.AppendLine("        </tr>");
			}
			stringBuilder.AppendLine("    </table>");
			stringBuilder.AppendLine("    <br><br>");
			m_StackReportWithArgs = stringBuilder.ToString();
			m_NativeStackDoubleQuoteSeparatedNoDupes = stringBuilder2.ToString();
		}

		private void LoadClrStackReportNoArgs()
		{
			bool flag = false;
			string text = "";
			if (!Globals.AnalyzeManaged.IsClrExtensionExecuting())
			{
				m_ClrStackReportNoArgs = "";
				m_ClrStackReportNoArgsNoColor = "";
				m_ClrStackDoubleQuoteSeparated = "";
				m_ClrStackSearch = "";
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			StringBuilder stringBuilder3 = new StringBuilder();
			StringBuilder stringBuilder4 = new StringBuilder();
			stringBuilder.Append("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><th></th></tr>");
			stringBuilder2.Append("<table border=0 cellpadding=0 cellspacing=0 class=myCustomText><tr><th></th></tr>");
			NetDbgThread dbgThread = m_dbgThread;
			new List<string>();
			NetDbgStackFrame val = null;
			for (int i = 0; i < ((List<NetDbgStackFrame>)(object)dbgThread.StackFrames).Count; i++)
			{
				val = ((List<NetDbgStackFrame>)(object)dbgThread.StackFrames)[i];
				if (val.IsManaged)
				{
					flag = true;
					text = val.GetFrameText(true, Globals.Manager.SourceInfoEnabled);
					stringBuilder3.Append("\"\"" + val.GetFrameText(false, false) + "\"\" ");
					stringBuilder2.AppendLine("<tr><td nowrap>" + text + "</td></tr>");
					stringBuilder.AppendLine("<tr><td nowrap>" + DotNetColorCode(text) + "</td></tr>");
					stringBuilder4.AppendLine(MakeFrameNumMarker(i) + text.ToUpper());
				}
			}
			stringBuilder.Append("</table>");
			stringBuilder2.Append("</table>");
			if (flag)
			{
				m_ClrStackReportNoArgs = stringBuilder.ToString();
				m_ClrStackReportNoArgsNoColor = stringBuilder2.ToString();
				m_ClrStackDoubleQuoteSeparated = stringBuilder3.ToString();
				m_ClrSearchString = stringBuilder4.ToString();
			}
		}

		private string GetFunctionPrototypeWithoutOffsetNorSrc(string FunctionPrototype)
		{
			string text = "";
			if (Globals.g_FunctionPrototypeWithoutOffsetCache.ContainsKey(FunctionPrototype))
			{
				return Globals.g_FunctionPrototypeWithoutOffsetCache[FunctionPrototype].ToString();
			}
			int num = Globals.HelperFunctions.InStrRev(FunctionPrototype, "+");
			if (num > 0)
			{
				text = Globals.HelperFunctions.Left(FunctionPrototype, num - 1);
			}
			num = Globals.HelperFunctions.InStrRev(FunctionPrototype, ")");
			if (num > 0)
			{
				text = Globals.HelperFunctions.Left(FunctionPrototype, num);
			}
			Globals.g_FunctionPrototypeWithoutOffsetCache.Add(FunctionPrototype, text);
			return text;
		}

		private string DotNetColorCode(string fnPrototype)
		{
			int num = Globals.HelperFunctions.InStr(fnPrototype, "(");
			if (num > 0)
			{
				return "<font color='darkblue'>" + Globals.HelperFunctions.Left(fnPrototype, num - 1) + "</font>" + Globals.HelperFunctions.Mid(fnPrototype, num);
			}
			return fnPrototype;
		}

		private string[] StripSymbolWarnings(ref string[] Lines)
		{
			string[] array = new string[Lines.Length];
			int num = 0;
			for (int i = 0; i <= Globals.HelperFunctions.UBound(Lines); i++)
			{
				if (Globals.HelperFunctions.InStr(Lines[i], "*** WARNING:") == 0)
				{
					array[num] = Lines[i];
					num++;
				}
				else if (Globals.HelperFunctions.InStr(Lines[i], "*** WARNING:") > 1)
				{
					int num2 = Globals.HelperFunctions.InStr(Lines[i], "*** WARNING:");
					array[num] = Globals.HelperFunctions.Mid(Lines[i], 1, num2 - 1);
					num++;
				}
			}
			return array;
		}

		private bool CheckClrStackOutput(ref string[] Lines)
		{
			if (Globals.HelperFunctions.UBound(Lines) >= 3)
			{
				for (int i = 0; i <= Globals.HelperFunctions.UBound(Lines); i++)
				{
					string str = Globals.HelperFunctions.Trim(Lines[i]);
					if (Globals.HelperFunctions.Left(str, 6) == "ESP   " && Globals.HelperFunctions.Right(str, 6) == "   EIP")
					{
						return true;
					}
					if ((Globals.HelperFunctions.Left(str, 8) == "Child-SP" || Globals.HelperFunctions.Left(str, 8) == "Child SP") && Globals.HelperFunctions.Right(str, 9) == "Call Site")
					{
						return true;
					}
				}
			}
			return false;
		}

		private void SplitClrStackFrameLine(ref string Line, ref double ESP, ref double EIP, ref string FunctionPrototype, ref bool IgnoreFrame)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			int num = 0;
			int num2 = 0;
			ESP = 0.0;
			EIP = 0.0;
			FunctionPrototype = "";
			IgnoreFrame = true;
			int num3 = Globals.HelperFunctions.Len(Line);
			if (Globals.g_Debugger.ClrVersionInfo.Version.Major < 2)
			{
				if (num3 > 1 && Globals.HelperFunctions.Left(Line, 2).ToUpper() == "0X")
				{
					num = 2;
				}
				num2 = 1;
			}
			int num4;
			int num5;
			if (Globals.g_SizeOfULongPtr == 8)
			{
				num4 = 17 + num;
				num5 = 34 + num2 + 2 * num;
			}
			else
			{
				num4 = 9 + num;
				num5 = 18 + num2 + 2 * num;
			}
			if (num3 <= num5 || Globals.HelperFunctions.InStr(Line, " ") != num4 || Globals.HelperFunctions.InStr(num4 + num2 + 1, Line, " ") != num5)
			{
				return;
			}
			IgnoreFrame = false;
			if (Globals.HelperFunctions.InStr(Line.ToUpper(), "DEBUGGER EXTENSION") > 0)
			{
				IgnoreFrame = true;
				return;
			}
			ESP = Convert.ToDouble("&H" + Globals.HelperFunctions.Mid(Line, num4 - 8, 8));
			EIP = Convert.ToDouble("&H" + Globals.HelperFunctions.Mid(Line, num5 - 8, 8));
			FunctionPrototype = Globals.HelperFunctions.Mid(Line, num5 + 1);
			if (Globals.HelperFunctions.InStr(FunctionPrototype, "(") == 0)
			{
				IgnoreFrame = true;
			}
			if (Globals.HelperFunctions.Left(FunctionPrototype, 1) == "[")
			{
				int num6 = Globals.HelperFunctions.InStr(FunctionPrototype, "]");
				if (num6 > 1 && Globals.HelperFunctions.Len(FunctionPrototype) > num6 + 1)
				{
					FunctionPrototype = Globals.HelperFunctions.Mid(FunctionPrototype, num6 + 2);
				}
			}
		}

		private void CheckInit()
		{
			if (m_dbgThread != null)
			{
				return;
			}
			throw new Exception("ScriptThreadClass not initialized");
		}

		public int FindFrameInClrStack(string sFrameText)
		{
			int num = -1;
			if (string.IsNullOrEmpty(m_StackReportNoArgs))
			{
				LoadCommonStackItems();
			}
			string cacheKey = "FindFrameInClrStack:" + m_ClrSearchString + ":" + sFrameText;
			num = GetFromClrStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				num = GetClrFrameNumFromHitPos(Globals.HelperFunctions.InStr(1, m_ClrSearchString, sFrameText));
				AddToClrStackSearchCache(cacheKey, num);
			}
			return num;
		}

		public int FindFrameInStack(string sFrameText)
		{
			int num = -1;
			if (string.IsNullOrEmpty(m_StackReportNoArgs))
			{
				LoadCommonStackItems();
			}
			string cacheKey = "FindFrameInStack:" + m_SearchString + ":" + sFrameText;
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				num = GetFrameNumFromHitPos(Globals.HelperFunctions.InStr(1, m_SearchString, sFrameText));
				AddToStackSearchCache(cacheKey, num);
			}
			return num;
		}

		public int FindFrameInStackBottomUp(string sFrameText)
		{
			int num = -1;
			if (string.IsNullOrEmpty(m_StackReportNoArgs))
			{
				LoadCommonStackItems();
			}
			string cacheKey = "FindFrameInStackBottomUp:" + m_SearchString + ":" + sFrameText;
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				num = GetFrameNumFromHitPos(Globals.HelperFunctions.InStrRev(1, m_SearchString, sFrameText));
				AddToStackSearchCache(cacheKey, num);
			}
			return num;
		}

		public int FindFrameInStackStartFrom(string sFrameText, int nStartFrameNum)
		{
			int num = -1;
			if (string.IsNullOrEmpty(m_StackReportNoArgs))
			{
				LoadCommonStackItems();
			}
			if (nStartFrameNum <= 0)
			{
				return FindFrameInStack(sFrameText);
			}
			string cacheKey = "FindFrameInStackStartFrom:" + m_SearchString + ":" + sFrameText + ":" + nStartFrameNum;
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				int num2 = Globals.HelperFunctions.InStr(1, m_SearchString, MakeFrameNumMarker(nStartFrameNum));
				if (num2 > 0)
				{
					num = GetFrameNumFromHitPos(Globals.HelperFunctions.InStr(num2, m_SearchString, sFrameText));
				}
				AddToStackSearchCache(cacheKey, num);
			}
			return num;
		}

		public int FindFrameFragmentsInStack(string[] saFrameFragments)
		{
			int num = -1;
			string cacheKey = "FindFrameFragmentsInStack:" + m_SearchString + ":" + Globals.HelperFunctions.Join(saFrameFragments, ":");
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = FindFrameFragmentsInStackInternal(saFrameFragments, 0);
				AddToStackSearchCache(cacheKey, num);
			}
			return num;
		}

		private int FindFrameFragmentsInStackInternal(string[] saFrameFragments, int nStartFrameNum)
		{
			int num = -1;
			int num2 = 0;
			num = -1;
			if (!Globals.HelperFunctions.IsNullOrEmpty(saFrameFragments))
			{
				int num3 = Globals.HelperFunctions.LBound(saFrameFragments);
				int num4 = Globals.HelperFunctions.UBound(saFrameFragments);
				int num5 = FindFrameInStackStartFrom(saFrameFragments[num3], nStartFrameNum);
				if (num5 > -1)
				{
					string findIn = StackFrames[num5].GetFrameText(includeOffset: true, Globals.Manager.SourceInfoEnabled).ToUpper();
					for (int i = num3 + 1; i <= num4; i++)
					{
						num2 = Globals.HelperFunctions.InStr(num2 + 1, findIn, saFrameFragments[i]);
						if (num2 == 0)
						{
							if (num5 < StackFrames.Count)
							{
								num = FindFrameFragmentsInStackInternal(saFrameFragments, num5 + 1);
							}
							return num;
						}
					}
					return num5;
				}
			}
			return num;
		}

		public int CountFrameHitsInStack(string sFrameText, int nStopAt)
		{
			int num = 0;
			if (string.IsNullOrEmpty(m_StackReportNoArgs))
			{
				LoadCommonStackItems();
			}
			string cacheKey = "CountFrameHitsInStack:" + m_SearchString + ":" + sFrameText + ":" + nStopAt;
			int num2 = GetFromStackSearchCache(cacheKey);
			if (num2 == -2)
			{
				num2 = 0;
				do
				{
					num = Globals.HelperFunctions.InStr(num + 1, m_SearchString, sFrameText);
					if (num == 0)
					{
						break;
					}
					num2++;
				}
				while (nStopAt <= 0 || num2 < nStopAt);
				AddToStackSearchCache(cacheKey, num2);
			}
			return num2;
		}

		public bool FindFramesInStackInOrder(ref Dictionary<int, string> colFrameTexts)
		{
			int num = 0;
			if (colFrameTexts.Count > 0)
			{
				foreach (int key in colFrameTexts.Keys)
				{
					string sFrameText = colFrameTexts[key];
					num = FindFrameInStackStartFrom(sFrameText, num);
					if (num == -1)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		private int GetFrameNumFromHitPos(int nHitPos)
		{
			int num = -1;
			if (nHitPos <= 0)
			{
				return -1;
			}
			string cacheKey = "GetFrameNumFromHitPos:" + m_SearchString + ":" + nHitPos;
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				int num2 = Globals.HelperFunctions.InStrRev(m_SearchString, "#FRAMENUM#", nHitPos);
				if (num2 > 0)
				{
					int num3 = num2 + Globals.HelperFunctions.Len("#FRAMENUM#");
					int num4 = Globals.HelperFunctions.InStr(num3, m_SearchString, "#");
					if (num4 > num3)
					{
						num = (int)Globals.HelperFunctions.CLng(Globals.HelperFunctions.Mid(m_SearchString, num3, num4 - num3));
						AddToStackSearchCache(cacheKey, num);
						return num;
					}
				}
				ASSERT(bCondition: false, "Unexpected condition in GetFrameNumFromHitPos");
			}
			return num;
		}

		private int GetClrFrameNumFromHitPos(int nHitPos)
		{
			int num = -1;
			if (nHitPos == 0)
			{
				return -1;
			}
			string cacheKey = "GetClrFrameNumFromHitPos:" + m_ClrSearchString + ":" + nHitPos;
			num = GetFromStackSearchCache(cacheKey);
			if (num == -2)
			{
				num = -1;
				int num2 = Globals.HelperFunctions.InStrRev(m_ClrSearchString, "#FRAMENUM#", nHitPos);
				if (num2 > 0)
				{
					int num3 = num2 + Globals.HelperFunctions.Len("#FRAMENUM#");
					int num4 = Globals.HelperFunctions.InStr(num3, m_ClrSearchString, "#");
					if (num4 > num3)
					{
						num = (int)Globals.HelperFunctions.CLng(Globals.HelperFunctions.Mid(m_ClrSearchString, num3, num4 - num3));
						AddToStackSearchCache(cacheKey, num);
						return num;
					}
				}
				ASSERT(bCondition: false, "Unexpected condition in GetFrameNumFromHitPos");
			}
			return num;
		}

		private int GetFromStackSearchCache(string cacheKey)
		{
			if (Globals.g_ThreadSearchCache.ContainsKey(cacheKey))
			{
				return Globals.g_ThreadSearchCache[cacheKey];
			}
			return -2;
		}

		private void AddToStackSearchCache(string cacheKey, int value)
		{
			if (!Globals.g_ThreadSearchCache.ContainsKey(cacheKey))
			{
				Globals.g_ThreadSearchCache.Add(cacheKey, value);
			}
		}

		private int GetFromClrStackSearchCache(string cacheKey)
		{
			if (Globals.g_ClrThreadSearchCache.ContainsKey(cacheKey))
			{
				return Globals.g_ClrThreadSearchCache[cacheKey];
			}
			return -2;
		}

		private void AddToClrStackSearchCache(string cacheKey, int value)
		{
			if (!Globals.g_ThreadSearchCache.ContainsKey(cacheKey))
			{
				Globals.g_ThreadSearchCache.Add(cacheKey, value);
			}
		}

		public List<string> StringSearch(string searchStringContentsStart, ulong startAddress = 0uL, ulong endAddress = 0uL)
		{
			return m_dbgThread.StringSearch(searchStringContentsStart, startAddress, endAddress, true);
		}
	}

	public class ScriptStackFrameClass
	{
		private NetDbgStackFrame m_dbgStackFrame;

		private string _frameText;

		private string m_FunctionName;

		private double? m_ChildEBP;

		private int? m_FrameNumber;

		private double? m_InstructionAddress;

		private double? m_StackAddress;

		private double? m_ReturnAddress;

		private double[] m_Args;

		public NetDbgStackFrame DbgStackFrame => m_dbgStackFrame;

		public string FunctionName
		{
			get
			{
				if (m_FunctionName == null)
				{
					m_FunctionName = m_dbgStackFrame.FunctionName;
				}
				return m_FunctionName;
			}
		}

		public double ChildEBP
		{
			get
			{
				if (!m_ChildEBP.HasValue)
				{
					m_ChildEBP = m_dbgStackFrame.ChildEBP;
				}
				return m_ChildEBP.Value;
			}
		}

		public int FrameNumber
		{
			get
			{
				if (!m_FrameNumber.HasValue)
				{
					m_FrameNumber = m_dbgStackFrame.FrameNumber;
				}
				return m_FrameNumber.Value;
			}
		}

		public double InstructionAddress
		{
			get
			{
				if (!m_InstructionAddress.HasValue)
				{
					m_InstructionAddress = m_dbgStackFrame.InstructionAddress;
				}
				return m_InstructionAddress.Value;
			}
		}

		public double StackAddress
		{
			get
			{
				if (!m_StackAddress.HasValue)
				{
					m_StackAddress = m_dbgStackFrame.StackAddress;
				}
				return m_StackAddress.Value;
			}
		}

		public double ReturnAddress
		{
			get
			{
				if (!m_ReturnAddress.HasValue)
				{
					m_ReturnAddress = m_dbgStackFrame.ReturnAddress;
				}
				return m_ReturnAddress.Value;
			}
		}

		public string GetFrameText(bool includeOffset = false, bool includeSourceInfo = false, bool includeIPAddress = false)
		{
			if (_frameText == null)
			{
				_frameText = m_dbgStackFrame.GetFrameText(includeOffset, includeSourceInfo);
			}
			return _frameText;
		}

		public double Args(int nZeroBasedArgNum)
		{
			if (m_Args == null || m_Args.Length == 0)
			{
				m_Args = new double[3];
				m_Args[0] = m_dbgStackFrame.GetArg(0);
				m_Args[1] = m_dbgStackFrame.GetArg(1);
				m_Args[2] = m_dbgStackFrame.GetArg(2);
			}
			if ((uint)nZeroBasedArgNum <= 2u)
			{
				return m_Args[nZeroBasedArgNum];
			}
			return m_dbgStackFrame.GetArg(nZeroBasedArgNum);
		}

		public bool Init(NetDbgStackFrame dbgStackFrame, int nFrameNumer)
		{
			if (dbgStackFrame != null)
			{
				m_dbgStackFrame = dbgStackFrame;
				m_FrameNumber = nFrameNumer;
				return true;
			}
			return false;
		}

		private void CheckInit()
		{
			if (m_dbgStackFrame != null)
			{
				return;
			}
			throw new Exception("ScriptStackFrameClass not initialized");
		}
	}

	public class ScriptModuleClass
	{
		public IDbgModule m_dbgModule;

		private double m_Base = -1.0;

		private double m_Size = -1.0;

		private string m_TimeStamp;

		private double m_CheckSum = -1.0;

		private string m_SymbolType;

		private bool? m_IsManaged;

		private string m_ImageName;

		private string m_ModuleName;

		private string m_LoadedImageName;

		private string m_SymbolFileName;

		private string m_MappedImageName;

		private int m_Index = -1;

		private string m_VSComments;

		private string m_VSInternalName;

		private string m_VSProductName;

		private string m_VSCompanyName;

		private string m_VSLegalCopyright;

		private string m_VSProductVersion;

		private string m_VSFileDescription;

		private string m_VSLegalTrademarks;

		private string m_VSPrivateBuild;

		private string m_VSFileVersion;

		private string m_VSOriginalFilename;

		private string m_VSSpecialBuild;

		private int m_FileVersionMajor = -1;

		private int m_FileVersionMinor = -1;

		private int m_FileVersionBuild = -1;

		private int m_FileVersionPrivate = -1;

		private int m_ProductVersionMajor = -1;

		private int m_ProductVersionMinor = -1;

		private int m_ProductVersionBuild = -1;

		private int m_ProductVersionPrivate = -1;

		private IExportsInfo m_ExportsInfo;

		private IImportsInfo m_ImportsInfo;

		private bool? m_IsISAPIExtension;

		private bool? m_IsISAPIFilter;

		private bool? m_IsVBModule;

		private bool? m_IsCOMDLL;

		private bool? m_RetainedInMemory;

		private bool? m_UnattendedExecution;

		private bool? m_SingleThreaded;

		private bool? m_bHasGoodSymbols;

		public double Base
		{
			get
			{
				if (m_Base == -1.0)
				{
					m_Base = m_dbgModule.Base;
				}
				return m_Base;
			}
		}

		public double Size
		{
			get
			{
				if (m_Size == -1.0)
				{
					m_Size = m_dbgModule.Size;
				}
				return m_Size;
			}
		}

		public string TimeStamp
		{
			get
			{
				if (string.IsNullOrEmpty(m_TimeStamp))
				{
					m_TimeStamp = m_dbgModule.TimeStamp;
				}
				return m_TimeStamp;
			}
		}

		public double CheckSum
		{
			get
			{
				if (m_CheckSum == -1.0)
				{
					m_CheckSum = m_dbgModule.Checksum;
				}
				return m_CheckSum;
			}
		}

		public string SymbolType
		{
			get
			{
				if (string.IsNullOrEmpty(m_SymbolType))
				{
					m_SymbolType = m_dbgModule.SymbolType;
				}
				return m_SymbolType;
			}
		}

		public bool? IsManaged
		{
			get
			{
				if (!m_IsManaged.HasValue)
				{
					m_IsManaged = m_dbgModule.IsManaged;
				}
				return m_IsManaged;
			}
		}

		public string ImageName
		{
			get
			{
				if (string.IsNullOrEmpty(m_ImageName))
				{
					m_ImageName = m_dbgModule.ImageName;
				}
				return m_ImageName;
			}
		}

		public string ModuleName
		{
			get
			{
				if (string.IsNullOrEmpty(m_ModuleName))
				{
					m_ModuleName = m_dbgModule.ModuleName;
				}
				return m_ModuleName;
			}
		}

		public string LoadedImageName
		{
			get
			{
				if (string.IsNullOrEmpty(m_LoadedImageName))
				{
					m_LoadedImageName = m_dbgModule.LoadedImageName;
				}
				return m_LoadedImageName;
			}
		}

		public string SymbolFileName
		{
			get
			{
				if (string.IsNullOrEmpty(m_SymbolFileName))
				{
					m_SymbolFileName = m_dbgModule.SymbolFileName;
				}
				return m_SymbolFileName;
			}
		}

		public string MappedImageName
		{
			get
			{
				if (string.IsNullOrEmpty(m_MappedImageName))
				{
					m_MappedImageName = m_dbgModule.MappedImageName;
				}
				return m_MappedImageName;
			}
		}

		public int Index
		{
			get
			{
				if (m_Index == -1)
				{
					m_Index = m_dbgModule.Index;
				}
				return m_Index;
			}
		}

		public string VSComments
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSComments))
				{
					m_VSComments = m_dbgModule.VSComments;
				}
				return m_VSComments;
			}
		}

		public string VSInternalName
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSInternalName))
				{
					m_VSInternalName = m_dbgModule.VSInternalName;
				}
				return m_VSInternalName;
			}
		}

		public string VSProductName
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSProductName))
				{
					m_VSProductName = m_dbgModule.VSProductName;
				}
				return m_VSProductName;
			}
		}

		public string VSCompanyName
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSCompanyName))
				{
					m_VSCompanyName = m_dbgModule.VSCompanyName;
				}
				return m_VSCompanyName;
			}
			set
			{
				if (m_VSCompanyName == string.Empty)
				{
					m_VSCompanyName = value;
				}
			}
		}

		public string VSLegalCopyright
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSLegalCopyright))
				{
					m_VSLegalCopyright = m_dbgModule.VSLegalCopyright;
				}
				return m_VSLegalCopyright;
			}
		}

		public string VSProductVersion
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSProductVersion))
				{
					m_VSProductVersion = m_dbgModule.VSProductVersion;
				}
				return m_VSProductVersion;
			}
		}

		public string VSFileDescription
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSFileDescription))
				{
					m_VSFileDescription = m_dbgModule.VSFileDescription;
				}
				return m_VSFileDescription;
			}
		}

		public string VSLegalTrademarks
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSLegalTrademarks))
				{
					m_VSLegalTrademarks = m_dbgModule.VSLegalTrademarks;
				}
				return m_VSLegalTrademarks;
			}
		}

		public string VSPrivateBuild
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSPrivateBuild))
				{
					m_VSPrivateBuild = m_dbgModule.VSPrivateBuild;
				}
				return m_VSPrivateBuild;
			}
		}

		public string VSFileVersion
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSFileVersion))
				{
					m_VSFileVersion = m_dbgModule.VSFileVersion;
				}
				return m_VSFileVersion;
			}
		}

		public string VSOriginalFilename
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSOriginalFilename))
				{
					m_VSOriginalFilename = m_dbgModule.VSOriginalFilename;
				}
				return m_VSOriginalFilename;
			}
		}

		public string VSSpecialBuild
		{
			get
			{
				if (string.IsNullOrEmpty(m_VSSpecialBuild))
				{
					m_VSSpecialBuild = m_dbgModule.VSSpecialBuild;
				}
				return m_VSSpecialBuild;
			}
		}

		public IExportsInfo ExportsInfo
		{
			get
			{
				if (m_ExportsInfo == null)
				{
					m_ExportsInfo = m_dbgModule.ExportsInfo;
				}
				return m_ExportsInfo;
			}
		}

		public IImportsInfo ImportsInfo
		{
			get
			{
				if (m_ImportsInfo == null)
				{
					m_ImportsInfo = m_dbgModule.ImportsInfo;
				}
				return m_ImportsInfo;
			}
		}

		public bool IsISAPIExtension
		{
			get
			{
				if (!m_IsISAPIExtension.HasValue)
				{
					m_IsISAPIExtension = m_dbgModule.IsISAPIExtension;
				}
				return m_IsISAPIExtension.Value;
			}
		}

		public bool IsISAPIFilter
		{
			get
			{
				if (!m_IsISAPIFilter.HasValue)
				{
					m_IsISAPIFilter = m_dbgModule.IsISAPIFilter;
				}
				return m_IsISAPIFilter.Value;
			}
		}

		public bool IsVBModule
		{
			get
			{
				if (!m_IsVBModule.HasValue)
				{
					m_IsVBModule = m_dbgModule.IsVBModule;
				}
				return m_IsVBModule.Value;
			}
		}

		public bool IsCOMDLL
		{
			get
			{
				if (!m_IsCOMDLL.HasValue)
				{
					m_IsCOMDLL = m_dbgModule.IsCOMDLL;
				}
				return m_IsCOMDLL.Value;
			}
		}

		public bool RetainedInMemory
		{
			get
			{
				if (!m_RetainedInMemory.HasValue)
				{
					m_RetainedInMemory = m_dbgModule.RetainedInMemory;
				}
				return m_RetainedInMemory.Value;
			}
		}

		public bool UnattendedExecution
		{
			get
			{
				if (!m_UnattendedExecution.HasValue)
				{
					m_UnattendedExecution = m_dbgModule.UnattendedExecution;
				}
				return m_UnattendedExecution.Value;
			}
		}

		public bool SingleThreaded
		{
			get
			{
				if (!m_SingleThreaded.HasValue)
				{
					m_SingleThreaded = m_dbgModule.SingleThreaded;
				}
				return m_SingleThreaded.Value;
			}
		}

		public bool HasGoodSymbols
		{
			get
			{
				if (!m_bHasGoodSymbols.HasValue)
				{
					switch (SymbolType.ToUpper())
					{
					case "UNKNOWN":
					case "EXPORT":
					case "NONE":
						m_bHasGoodSymbols = false;
						break;
					default:
						m_bHasGoodSymbols = true;
						break;
					}
				}
				return m_bHasGoodSymbols.Value;
			}
		}

		public bool IsISAPIDLL
		{
			get
			{
				if (IsISAPIExtension)
				{
					return true;
				}
				if (IsISAPIFilter)
				{
					return true;
				}
				return false;
			}
		}

		public bool InitByName(string sModuleName)
		{
			bool flag = false;
			if (Convert.ToBoolean(value: false))
			{
				CheckCaller(Globals.g_ModuleCache);
				ASSERT(sModuleName == Globals.HelperFunctions.UCase(sModuleName), "Module names must be UPPERCASE");
			}
			flag = false;
			m_dbgModule = null;
			try
			{
				m_dbgModule = Globals.g_Debugger.GetModuleByModuleName(sModuleName);
				if (m_dbgModule != null)
				{
					m_ModuleName = sModuleName;
					flag = true;
				}
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}

		public bool InitByAddress(double addr)
		{
			bool result = false;
			IDbgModule moduleByAddress = Globals.g_Debugger.GetModuleByAddress(addr);
			if (moduleByAddress != null)
			{
				m_dbgModule = moduleByAddress;
				result = true;
			}
			else
			{
				string symbolFromAddress = Globals.g_Debugger.GetSymbolFromAddress(addr);
				if (Globals.HelperFunctions.Len(symbolFromAddress) > 0 && Globals.HelperFunctions.InStr(symbolFromAddress, "!") == 0 && Globals.HelperFunctions.InStr(symbolFromAddress, ".") > 0)
				{
					string sModuleName = Globals.HelperFunctions.Split(symbolFromAddress, ".")[0].ToUpper();
					result = InitByName(sModuleName);
				}
			}
			return result;
		}

		public void GetFileVersion(ref int Major, ref int Minor, ref int Build, ref int Priv)
		{
			object obj = null;
			object obj2 = null;
			object obj3 = null;
			object obj4 = null;
			if (m_FileVersionMajor == -1)
			{
				m_dbgModule.GetFileVersion(ref obj, ref obj2, ref obj3, ref obj4);
				m_FileVersionMajor = (int)obj;
				m_FileVersionMinor = (int)obj2;
				m_FileVersionBuild = (int)obj3;
				m_FileVersionPrivate = (int)obj4;
			}
			Major = m_FileVersionMajor;
			Minor = m_FileVersionMinor;
			Build = m_FileVersionBuild;
			Priv = m_FileVersionPrivate;
		}

		public void GetProductVersion(ref int Major, ref int Minor, ref int Build, ref int Priv)
		{
			object obj = null;
			object obj2 = null;
			object obj3 = null;
			object obj4 = null;
			if (m_ProductVersionMajor == -1)
			{
				m_dbgModule.GetProductVersion(ref obj, ref obj2, ref obj3, ref obj4);
			}
			m_ProductVersionMajor = (int)obj;
			m_ProductVersionMinor = (int)obj2;
			m_ProductVersionBuild = (int)obj3;
			m_ProductVersionPrivate = (int)obj4;
			Major = m_ProductVersionMajor;
			Minor = m_ProductVersionMinor;
			Build = m_ProductVersionBuild;
			Priv = m_ProductVersionPrivate;
		}

		private void CheckInit()
		{
			if (m_dbgModule != null)
			{
				return;
			}
			throw new Exception("ScriptModuleClass not initialized");
		}
	}

	public class ScriptThreadsClass
	{
		private Dictionary<double, ScriptThreadClass> m_colThreadsByThreadNum;

		private Dictionary<double, ScriptThreadClass> m_colThreadsBySystemID;

		private bool m_bIsIniting;

		private int m_TrueCount = -1;

		public bool IsIniting => m_bIsIniting;

		public int Count
		{
			get
			{
				if (m_TrueCount == -1)
				{
					m_TrueCount = Globals.g_Debugger.Threads.Count;
				}
				return m_TrueCount;
			}
		}

		public ScriptThreadsClass()
		{
			m_colThreadsByThreadNum = new Dictionary<double, ScriptThreadClass>();
			m_colThreadsBySystemID = new Dictionary<double, ScriptThreadClass>();
		}

		~ScriptThreadsClass()
		{
			m_colThreadsByThreadNum = null;
			m_colThreadsBySystemID = null;
		}

		private void Add(ScriptThreadClass ScriptThread)
		{
			if (ScriptThread == null)
			{
				new Exception("In ScriptThreadsClass::Add - ScriptThread Is Nothing");
			}
			double key = ScriptThread.ThreadID;
			double key2 = ScriptThread.SystemID;
			if (m_colThreadsByThreadNum.ContainsKey(key))
			{
				new Exception("ScriptThreadsClass::Add - ThreadID : " + key + " already exists in collection");
			}
			if (m_colThreadsBySystemID.ContainsKey(key2))
			{
				new Exception("ScriptThreadsClass::Add - SystemID " + key2 + " already exists in collection");
			}
			m_colThreadsByThreadNum.Add(key, ScriptThread);
			m_colThreadsBySystemID.Add(key2, ScriptThread);
		}

		public ScriptThreadClass Item(int nThreadID)
		{
			ScriptThreadClass scriptThreadClass;
			if (m_colThreadsByThreadNum.ContainsKey(nThreadID))
			{
				scriptThreadClass = m_colThreadsByThreadNum[nThreadID];
			}
			else
			{
				scriptThreadClass = new ScriptThreadClass();
				m_bIsIniting = true;
				bool num = scriptThreadClass.Init(nThreadID);
				m_bIsIniting = false;
				if (!num)
				{
					return null;
				}
				Add(scriptThreadClass);
			}
			return scriptThreadClass;
		}

		public ScriptThreadClass ItemBySystemID(int lSystemID)
		{
			ScriptThreadClass scriptThreadClass;
			if (m_colThreadsBySystemID.ContainsKey(lSystemID))
			{
				scriptThreadClass = m_colThreadsBySystemID[lSystemID];
			}
			else
			{
				scriptThreadClass = new ScriptThreadClass();
				m_bIsIniting = true;
				bool num = scriptThreadClass.InitBySystemID(lSystemID);
				m_bIsIniting = false;
				if (!num)
				{
					return null;
				}
				Add(scriptThreadClass);
			}
			return scriptThreadClass;
		}
	}

	public class ScriptModulesClass
	{
		private Dictionary<double, ScriptModuleClass> m_colModulesByAddress;

		private Dictionary<string, ScriptModuleClass> m_colModulesByName;

		private bool m_bIsIniting;

		private int m_TrueCount = -1;

		public bool IsIniting => m_bIsIniting;

		public int Count
		{
			get
			{
				if (m_TrueCount == -1)
				{
					m_TrueCount = Globals.g_Debugger.Modules.Count;
				}
				return m_TrueCount;
			}
		}

		public ScriptModulesClass()
		{
			m_colModulesByName = new Dictionary<string, ScriptModuleClass>();
			m_colModulesByAddress = new Dictionary<double, ScriptModuleClass>();
		}

		~ScriptModulesClass()
		{
			m_colModulesByName = null;
			m_colModulesByAddress = null;
		}

		public ScriptModuleClass Item(ref int nIndex)
		{
			return GetModuleByName(Globals.g_Debugger.Modules[nIndex].ModuleName);
		}

		public ScriptModuleClass GetModuleByName(string sModuleName)
		{
			if (string.IsNullOrEmpty(sModuleName))
			{
				return null;
			}
			sModuleName = Globals.HelperFunctions.UCase(sModuleName);
			ScriptModuleClass scriptModuleClass;
			if (m_colModulesByName.ContainsKey(sModuleName))
			{
				scriptModuleClass = m_colModulesByName[sModuleName];
			}
			else
			{
				scriptModuleClass = new ScriptModuleClass();
				m_bIsIniting = true;
				bool num = scriptModuleClass.InitByName(sModuleName);
				m_bIsIniting = false;
				if (!num)
				{
					scriptModuleClass = null;
				}
				m_colModulesByName.Add(sModuleName, scriptModuleClass);
			}
			return scriptModuleClass;
		}

		public ScriptModuleClass ItemByAddress(double addr)
		{
			ScriptModuleClass scriptModuleClass = null;
			bool flag = false;
			if (m_colModulesByAddress.ContainsKey(addr))
			{
				scriptModuleClass = m_colModulesByAddress[addr];
			}
			else
			{
				if (addr != 0.0)
				{
					scriptModuleClass = new ScriptModuleClass();
					m_bIsIniting = true;
					flag = scriptModuleClass.InitByAddress(addr);
					m_bIsIniting = false;
				}
				if (!flag)
				{
					scriptModuleClass = null;
				}
				m_colModulesByAddress.Add(addr, scriptModuleClass);
				if (scriptModuleClass != null)
				{
					string moduleName = scriptModuleClass.ModuleName;
					if (!m_colModulesByName.ContainsKey(moduleName))
					{
						m_colModulesByName.Add(moduleName, scriptModuleClass);
					}
				}
			}
			return scriptModuleClass;
		}
	}

	public const bool DEBUG_ON = false;

	public const bool TRACE_ON = false;

	public const string FRAMENUM_MARKER_LEFT = "#FRAMENUM#";

	public const string FRAMENUM_MARKER_RIGHT = "#";

	public const int FINDFRAME_NONEFOUND = -1;

	public const int THREADNUM_INVALID = -1;

	private const uint E_INVALIDARG = 2147942487u;

	private const uint E_ASSERTFAILED = 2147745793u;

	private const uint E_NOTIMPL = 2147745794u;

	private const uint E_SCRIPTTHREADCLASS_NOINIT = 2147745795u;

	private const uint E_SCRIPTSTACKFRAMECLASS_NOINIT = 2147745796u;

	private const uint E_SCRIPTMODULECLASS_NOINIT = 2147745797u;

	private const string STR_NOTIMPL = "Function Not Implemented.";

	private const string STR_THREADCLASS_INIT = "ScriptThreadClass::CheckInit";

	private const string STR_THREADCLASS_NOINIT = "ScriptThreadClass::CheckInit was never called (or failed).";

	private const string STR_SCRIPTSTACKFRAMECLASS_INIT = "ScriptStackFrameClass::Init";

	private const string STR_SCRIPTSTACKFRAMECLASS_NOINIT = "ScriptStackFrameClass::Init was never called (or failed).";

	private const string STR_SCRIPTMODULECLASS_INIT = "ScriptModuleClass::Init";

	private const string STR_SCRIPTMODULECLASS_NOINIT = "ScriptModuleClass::Init was never called (or failed).";

	private const string STR_UNLOADEDMODPATTERNLEFT = "<UNLOADED_";

	private const string STR_UNLOADEDMODPATTERNRIGHT = ">";

	private const int LEN_UNLOADEDMODPATTERNLEFT = 10;

	private const int LEN_UNLOADEDMODPATTERNRIGHT = 1;

	private const int LEN_UNLOADEDMODPATTERNMIN = 14;

	private const int NOT_PRESENT_IN_CACHE = -2;

	public static void ResetCache()
	{
		Globals.g_ThreadInfoCache = new ScriptThreadsClass();
		Globals.g_ModuleCache = new ScriptModulesClass();
		Globals.g_SymbolFromAddrCache = new Dictionary<double, string>();
		Globals.g_SymbolFromAddrIEReplacedCache = new Dictionary<double, string>();
		Globals.g_SourceInfoFromAddrCache = new Dictionary<double, string>();
		Globals.g_FunctionNameFromAddrCache = new Dictionary<double, string>();
		Globals.g_ModuleNameFromAddrCache = new Dictionary<double, string>();
		Globals.g_ThreadSearchCache = new Dictionary<string, int>();
		Globals.g_ClrThreadSearchCache = new Dictionary<string, int>();
		Globals.g_FunctionPrototypeWithoutOffsetCache = new Dictionary<string, string>();
	}

	public static void ASSERT(bool bCondition, string sMessage)
	{
	}

	public static ScriptModuleClass GetModuleFromAddress(double addr)
	{
		ScriptModuleClass scriptModuleClass = null;
		string sModuleName = "";
		scriptModuleClass = Globals.g_ModuleCache.ItemByAddress(addr);
		if (scriptModuleClass == null)
		{
			Globals.g_Debugger.GetModuleByAddress(addr, ref sModuleName);
			scriptModuleClass = Globals.g_ModuleCache.GetModuleByName(sModuleName);
		}
		return scriptModuleClass;
	}

	public static string GetSymbolFromAddress(ScriptStackFrameClass stackFrame)
	{
		double instructionAddress = stackFrame.InstructionAddress;
		string text;
		if (Globals.g_SymbolFromAddrCache.ContainsKey(instructionAddress))
		{
			text = Globals.g_SymbolFromAddrCache[instructionAddress];
		}
		else
		{
			text = stackFrame.GetFrameText(includeOffset: true, Globals.Manager.SourceInfoEnabled);
			if (instructionAddress != 0.0)
			{
				Globals.g_SymbolFromAddrCache.Add(instructionAddress, text);
			}
		}
		return text;
	}

	public static string GetSymbolFromAddress(double addr)
	{
		string text;
		if (Globals.g_SymbolFromAddrCache.ContainsKey(addr))
		{
			text = Globals.g_SymbolFromAddrCache[addr];
		}
		else
		{
			text = Globals.g_Debugger.GetSymbolFromAddress(addr);
			if (addr != 0.0)
			{
				Globals.g_SymbolFromAddrCache.Add(addr, text);
			}
		}
		return text;
	}

	public static string GetSymbolFromAddressIEReplaced(ScriptStackFrameClass stackFrame)
	{
		double instructionAddress = stackFrame.InstructionAddress;
		string text;
		if (Globals.g_SymbolFromAddrIEReplacedCache.ContainsKey(instructionAddress))
		{
			text = Globals.g_SymbolFromAddrIEReplacedCache[instructionAddress];
		}
		else
		{
			text = Globals.HelperFunctions.Replace(GetSymbolFromAddress(stackFrame), "<", "&lt");
			text = Globals.HelperFunctions.Replace(text, ">", "&gt");
			if (instructionAddress != 0.0)
			{
				Globals.g_SymbolFromAddrIEReplacedCache.Add(instructionAddress, text);
			}
		}
		return text;
	}

	public static string GetSourceInfoFromAddress(double addr)
	{
		string text;
		if (Globals.g_SourceInfoFromAddrCache.ContainsKey(addr))
		{
			text = Globals.g_SourceInfoFromAddrCache[addr];
		}
		else
		{
			text = Globals.g_Debugger.GetSourceInfoFromAddress(addr);
			if (addr != 0.0)
			{
				Globals.g_SourceInfoFromAddrCache.Add(addr, text);
			}
		}
		return text;
	}

	public static string GetFunctionName(double addr)
	{
		return Globals.HelperFunctions.UCase(Globals.HelperFunctions.GetFunctionNameNoUpper(addr));
	}

	private static string MakeFrameNumMarker(int nFrameNum)
	{
		return "#FRAMENUM#" + nFrameNum + "#";
	}

	public static ScriptThreadClass ScriptThreadFromDbgThread(NetDbgThread dbgThread)
	{
		return Globals.g_ThreadInfoCache.ItemBySystemID((int)dbgThread.SystemID);
	}

	private static void CheckCaller(ScriptThreadsClass globalCacheCollection)
	{
	}

	private static void CheckCaller(ScriptModulesClass globalCacheCollection)
	{
	}

	internal static string GetArgHTMLOutput(ScriptStackFrameClass StackFrame, int p)
	{
		string text = Globals.HelperFunctions.GetArgAsHexString(StackFrame, p);
		if (text.Substring(0, 2) == "0x")
		{
			text = text.Substring(3);
		}
		return "            <td nowrap>&nbsp;&nbsp;&nbsp;&nbsp;<font face = \"courier new\">" + text + "</font></td>";
	}
}
