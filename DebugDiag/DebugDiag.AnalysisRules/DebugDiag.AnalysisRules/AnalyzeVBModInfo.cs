using System.Collections.Generic;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using DebugDiag.DotNet.Reports;

namespace DebugDiag.AnalysisRules;

public static class AnalyzeVBModInfo
{
	public static string GetVBRTVer()
	{
		string text = "";
		int Major = 0;
		int Minor = 0;
		int Build = 0;
		int Priv = 0;
		CacheFunctions.ScriptModuleClass moduleByName = Globals.g_ModuleCache.GetModuleByName("msvbvm60");
		if (moduleByName == null)
		{
			return "";
		}
		moduleByName.GetFileVersion(ref Major, ref Minor, ref Build, ref Priv);
		text = Major + "." + Minor + "." + Build + "." + Priv;
		if (Globals.g_OSVER < Globals.OS_VER_WIN2K3)
		{
			if (text.CompareTo("6.0.96.32") < 0)
			{
				return text;
			}
			return "";
		}
		if (text.CompareTo("6.0.97.82") < 0)
		{
			return text;
		}
		return "";
	}

	public static void ReportVBDllInfo()
	{
		Dictionary<double, IDbgModule> dictionary = new Dictionary<double, IDbgModule>();
		IModuleInfo modules = Globals.g_Debugger.Modules;
		if (modules != null)
		{
			for (int i = 0; i < modules.Count; i++)
			{
				IDbgModule val = modules[i];
				if (val.IsVBModule && (!val.RetainedInMemory || !val.UnattendedExecution))
				{
					dictionary.Add(val.Base, val);
				}
			}
		}
		if (dictionary.Count <= 0)
		{
			return;
		}
		string text = ((Globals.g_OSVER >= Globals.OS_VER_WIN2K3) ? "or install <b><a target='_blank' href='http://support.microsoft.com/kb/290887'>Visual Studio SP 6</a></b>, which contains an updated version of MSVBVM60.DLL that will automatically set these options for dlls loaded in IIS worker processes." : "or apply the hotfix associated with KB Article: <b><a target='_blank' href='http://support.microsoft.com/?id=241896'>Q307211</a></b>");
		ReportSection val2 = Globals.Manager.CurrentSection.AddChildSection("RIMUE", (SectionType)0);
		val2.Title = "Improperly Compiled VB Modules";
		val2.Write("<b>MSVBVM60.DLL Version: " + Globals.g_oldVBRuntime + "</b><br><br>");
		val2.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText>");
		foreach (double key in dictionary.Keys)
		{
			IDbgModule val = dictionary[key];
			val2.Write("<tr><td><b>Module Name</b> = " + val.VSInternalName + "</td>");
			val2.Write("<td>&nbsp;&nbsp;&nbsp;&nbsp;<b>RIM</b> = " + val.RetainedInMemory + "</td>");
			val2.Write("<td>&nbsp;&nbsp;&nbsp;&nbsp;<b>UE</b> = " + val.UnattendedExecution + "</td></tr>");
		}
		val2.Write("</table>");
		Globals.Manager.ReportWarning("One or more <b><a href='#RIMUE" + Globals.g_UniqueReference + "'>VB Modules</a></b> in <b>" + Globals.g_ShortDumpFileName + "</b> are not compiled correctly, which can lead to a hang condition with IIS.<br><br>More information can be found in the following KB article: <br><br><b><a target='_blank' href='http://support.microsoft.com/?id=241896'>241896 - Threading Issues with Visual Basic 6.0 ActiveX Components </a></b>", "Please recompile the listed modules with both 'Retain in Memory' and 'Unattended Execution' set, " + text, 0, "{ef26099b-131d-4f08-a8e1-d1d0119d09fa}");
	}

	public static string AnalyzeRimUeIssue(int lSystemID)
	{
		string text = null;
		CacheFunctions.ScriptThreadClass scriptThreadClass = Globals.g_ThreadInfoCache.ItemBySystemID(lSystemID);
		if (scriptThreadClass == null)
		{
			return null;
		}
		NetDbgStackFrames nativeStackFrames = scriptThreadClass.m_dbgThread.NativeStackFrames;
		for (int i = 0; i < ((List<NetDbgStackFrame>)(object)nativeStackFrames).Count; i++)
		{
			NetDbgStackFrame val = ((List<NetDbgStackFrame>)(object)nativeStackFrames)[i];
			text = val.GetFrameText(true, Globals.Manager.SourceInfoEnabled);
			if (Globals.HelperFunctions.InStr(1, text, "MSVBVM60!VBDLLCANUNLOADNOW") != 0 || Globals.HelperFunctions.InStr(1, text, "MSVBVM60!VBDLLGETCLASSOBJECT") != 0)
			{
				return CacheFunctions.GetModuleFromAddress(val.GetArg(0)).ImageName;
			}
		}
		return null;
	}
}
