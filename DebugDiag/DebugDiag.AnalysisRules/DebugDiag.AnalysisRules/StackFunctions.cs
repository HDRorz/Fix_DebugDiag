using System;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

public class StackFunctions
{
	internal class CallFrame
	{
		public double RetAddr;

		public double DestAddr;

		public double RetSymbol;

		public double DestSymbol;

		public double ESP;

		public double EBP;
	}

	private const int FUNCOFFSET_MAX = 8000;

	private static double ESP;

	private static double EBP;

	private static double EIP;

	private static int LastCheckPoint;

	private static int CurrentFrame;

	private static CallFrame OneFrame;

	private static CallFrame[] CallFrames;

	public static string AnalyzeCorruptStack(CacheFunctions.ScriptThreadClass Thread)
	{
		string result = "";
		string text = "";
		string text2 = "";
		long num = 0L;
		CacheFunctions.ScriptStackFrameClass scriptStackFrameClass = null;
		long num2 = 0L;
		int num3 = 0;
		if (Convert.ToString(Globals.g_OSPlatformVersion) == "X64")
		{
			return result;
		}
		ESP = (long)Thread.Register("esp");
		EBP = (long)Thread.Register("ebp");
		EIP = (long)Thread.InstructionAddress;
		if (Convert.ToString(Globals.g_Debugger.GetAs32BitHexString(EBP)).ToUpper() == "0XDEACB6B6")
		{
			return "Symptoms of a known SSL issue were detected in <b>" + Convert.ToString(Globals.g_ShortDumpFileName) + "</b>. Please make sure that the following critical update has been applied to this server: <br><br><a href='http://www.microsoft.com/technet/security/bulletin/ms04-011.mspx'><b>Microsoft Security Bulletin MS04-011</b></a>";
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("SCAnalysis", (SectionType)0);
		val.Title = "Detailed stack corruption analysis for thread " + Convert.ToString(Thread.ThreadID);
		val.Write("<font color=Red><p><b>Call stack with StackWalk</b></p></font>");
		val.Write("<table class=myCustomText><tr><th>Index</th><th>Return Address</th></tr>");
		int num4 = 1;
		foreach (int key in Thread.StackFrames.Keys)
		{
			scriptStackFrameClass = Thread.StackFrames[key];
			val.Write("<tr><td>" + Convert.ToString(num4) + "</td><td>");
			val.Write(scriptStackFrameClass.GetFrameText(includeOffset: true, Globals.Manager.SourceInfoEnabled) + " (" + Convert.ToString(Globals.g_Debugger.GetAs32BitHexString(scriptStackFrameClass.InstructionAddress)) + ")</td></tr>");
			num4++;
		}
		val.Write("</table>");
		LastCheckPoint = 0;
		text = Convert.ToString(Globals.g_Debugger.Execute("?poi(@$teb+4)")).Split('=')[0];
		if (text.Split(':').GetUpperBound(0) >= 1)
		{
			num2 = Convert.ToInt64(text[1]);
			CallFrames = new CallFrame[101];
			CurrentFrame = 0;
			OneFrame = new CallFrame();
			OneFrame.RetAddr = EIP;
			OneFrame.DestAddr = 0.0;
			OneFrame.EBP = EBP;
			CallFrames[CurrentFrame] = OneFrame;
			CurrentFrame++;
			val.Write("<br><br><font color=Red><p><b>Call stack - Heuristic</b></p></font><table class=myCustomText><tr><th>Index</th><th>Stack Address</th><th>Child EBP</th><th>Return Address</th><th>Destination</th></tr>");
			EIP = Globals.g_Debugger.ReadDWord(ESP);
			while (ESP < (double)num2 - 56.0 && CurrentFrame < 100)
			{
				if (!((double)Convert.ToInt32(EIP) >= ESP) || Convert.ToInt64(EIP) > num2)
				{
					CheckForCall();
				}
				ESP += 4.0;
				EIP = Globals.g_Debugger.ReadDWord(ESP);
			}
			num4 = 1;
			for (int i = 0; i <= CurrentFrame - 1; i++)
			{
				OneFrame = CallFrames[i];
				val.Write("<tr><td>" + Convert.ToString(num4) + "</td><td>");
				val.Write(Convert.ToString(Globals.g_Debugger.GetAs64BitHexString(OneFrame.ESP)) + "</td><td>");
				val.Write(Convert.ToString(Globals.g_Debugger.GetAs64BitHexString(OneFrame.EBP)) + "</td><td>");
				val.Write(Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(OneFrame.RetAddr)) + "</td><td>");
				val.Write(Convert.ToString(Globals.g_Debugger.GetSymbolFromAddress(OneFrame.DestAddr)) + "</td></tr>");
				num4++;
			}
			val.Write("</table><br>");
			val.Write("<pre>");
			foreach (int key2 in Thread.StackFrames.Keys)
			{
				scriptStackFrameClass = Thread.StackFrames[key2];
				num3 = Convert.ToInt32(scriptStackFrameClass.ReturnAddress);
				if (num3 == 0)
				{
					continue;
				}
				for (int i = 2; i <= 10; i++)
				{
					num = Globals.g_Debugger.ReadByte((double)(Convert.ToInt32(scriptStackFrameClass.ReturnAddress) - i));
					if (Convert.ToInt32(num) == 232)
					{
						num3 = Convert.ToInt32(scriptStackFrameClass.ReturnAddress) - i;
						break;
					}
					if (Convert.ToInt32(num) == 255 && Convert.ToInt32((long)Globals.g_Debugger.ReadByte((double)(Convert.ToInt32(scriptStackFrameClass.ReturnAddress) - (i - 1)))) != 255)
					{
						num3 = Convert.ToInt32(scriptStackFrameClass.ReturnAddress) - i;
						break;
					}
				}
				text2 = Convert.ToString(Globals.g_Debugger.Execute("u " + Convert.ToString(Globals.g_Debugger.GetAs32BitHexString((double)num3)) + "  l1")).Split('\n')[1];
				val.Write(text2 + "\r\n");
			}
			val.Write("</pre><br>");
		}
		return "If no further information can be obtained from the <a href='#SCAnalysis" + Globals.g_UniqueReference + "'>detailed stack analysis below</a>, please contact Microsoft Corporation for troubleshooting steps on stack corruption<br>";
	}

	private static void CheckForCall()
	{
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		long num4 = 0L;
		bool flag = false;
		long value = Convert.ToInt64(EIP);
		EBP = Globals.g_Debugger.ReadDWord(ESP - 4.0);
		bool flag2 = false;
		if (Convert.ToInt32(value) > 4095)
		{
			for (int i = 2; i <= 10; i++)
			{
				num = 0L;
				num2 = Globals.g_Debugger.ReadByte((double)(Convert.ToInt32(value) - i));
				if (Convert.ToInt32(num2) == 232 && i == 5)
				{
					num4 = Convert.ToInt32(Globals.g_Debugger.ReadDWord((double)(Convert.ToInt32(value) - (i - 1))));
					if (num4 > int.MaxValue)
					{
						num4 = uint.MaxValue - num4;
						num4++;
						num4 *= -1;
					}
					num = Convert.ToInt32(value) + num4;
					if (Convert.ToInt32((long)Globals.g_Debugger.ReadWord((double)num)) == 9727)
					{
						num = Convert.ToInt32(Globals.g_Debugger.ReadDWord((double)(num + 2)));
						num = Convert.ToInt32(Globals.g_Debugger.ReadDWord((double)num));
					}
					flag2 = true;
					break;
				}
				if (Convert.ToInt32(num2) != 255)
				{
					continue;
				}
				num3 = Convert.ToInt32(Globals.g_Debugger.ReadByte((double)(Convert.ToInt32(value) - (i - 1))));
				flag2 = true;
				if (num3 <= 87)
				{
					long num5 = num3 - 16;
					if ((ulong)num5 <= 7uL)
					{
						switch (num5)
						{
						case 0L:
						case 1L:
						case 2L:
						case 3L:
						case 6L:
						case 7L:
							if (i != 2)
							{
								flag2 = false;
							}
							goto IL_0266;
						case 5L:
							goto IL_0209;
						case 4L:
							goto IL_0266;
						}
					}
					long num6 = num3 - 80;
					if ((ulong)num6 <= 7uL)
					{
						switch (num6)
						{
						case 0L:
						case 1L:
						case 2L:
						case 3L:
						case 5L:
						case 6L:
						case 7L:
							if (i != 3)
							{
								flag2 = false;
							}
							goto IL_0266;
						case 4L:
							goto IL_0266;
						}
					}
				}
				else
				{
					long num7 = num3 - 144;
					if ((ulong)num7 <= 7uL)
					{
						switch (num7)
						{
						case 0L:
						case 1L:
						case 2L:
						case 3L:
						case 5L:
						case 6L:
						case 7L:
							if (i != 6)
							{
								flag2 = false;
							}
							goto IL_0266;
						case 4L:
							goto IL_0266;
						}
					}
					if ((ulong)(num3 - 208) <= 7uL)
					{
						if (i != 2)
						{
							flag2 = false;
						}
						goto IL_0266;
					}
				}
				flag2 = false;
				goto IL_0266;
				IL_0209:
				if (i == 6)
				{
					num = Convert.ToInt32(Globals.g_Debugger.ReadDWord((double)(Convert.ToInt32(value) - (i - 2))));
					num = Convert.ToInt32(Globals.g_Debugger.ReadDWord((double)num));
				}
				else
				{
					flag2 = false;
				}
				goto IL_0266;
				IL_0266:
				if (flag2)
				{
					break;
				}
			}
		}
		if (!flag2)
		{
			return;
		}
		OneFrame = new CallFrame();
		OneFrame.RetAddr = EIP;
		OneFrame.DestAddr = num;
		OneFrame.EBP = EBP;
		OneFrame.ESP = ESP;
		if (num == 0L)
		{
			CallFrames[CurrentFrame] = OneFrame;
			CurrentFrame++;
			return;
		}
		flag = false;
		for (int i = CurrentFrame - 1; i >= LastCheckPoint; i += -1)
		{
			if (CallFrames[i].RetAddr > OneFrame.DestAddr && Convert.ToInt64(CallFrames[i].RetAddr) - Convert.ToInt64(OneFrame.DestAddr) < 8000)
			{
				flag = true;
				CurrentFrame = i + 1;
				break;
			}
		}
		if (flag)
		{
			if (CurrentFrame == 1)
			{
				LastCheckPoint = CurrentFrame;
			}
			else if (Convert.ToInt32(CallFrames[CurrentFrame - 1].DestAddr) != 0)
			{
				LastCheckPoint = CurrentFrame;
			}
			CallFrames[CurrentFrame] = OneFrame;
			CurrentFrame++;
		}
	}
}
