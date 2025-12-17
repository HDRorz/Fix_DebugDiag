using System;
using System.Collections.Generic;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public interface IHelperFunctions
{
	IDbgCritSec OwnsCritSec(double threadId);

	void AddAnalysisError(int errorNumber, string errorSource, string errDescription, string strFunctionName);

	string LongNameFromShortName(string FileName);

	string Mid(string str, int start);

	string Mid(string str, int start, int length);

	int InStrRev(int StartIndex, string FindIn, string ToFind);

	int InStrRev(string FindIn, string ToFind, int StartIndex);

	int InStrRev(string FindIn, string ToFind);

	int InStr(int StartIndex, string FindIn, string ToFind);

	int InStr(string FindIn, string ToFind);

	string MaskPwd(string ConnectionString);

	string EndToggleSectionString();

	void EndToggleSection();

	void StartToggleSectionWithImage(string linkText, string key, string image, bool startCollapsed);

	string GetCanonacolizedLinkKey(string key);

	string StartToggleSectionWithImageString(string linkText, string key, string image, bool startCollapsed);

	string StartToggleSectionString(string linkText, string key, bool startCollapsed);

	string StartToggleSectionWithHeaderString(string linkText, string key, bool startCollapsed, string headerLevel);

	void StartToggleSectionWithHeader(string linkText, string key, bool startCollapsed, string headerLevel);

	void StartToggleSectionWithHeader(string linkText, string key, bool startCollapsed, int headerLevel);

	void StartToggleSection(string linkText, string key, bool startCollapsed);

	string EndIndentedSectionString();

	void EndIndentedSection();

	string StartIndentedSectionString(string width);

	void StartIndentedSection(string width);

	void ShowTraceData();

	void TRACE(string s);

	double Max(double a, double b);

	bool Not(bool condition);

	bool IsObject(object obj);

	string GetUniqueReference(NetDbgObj Debugger);

	void CloseDebuggerForPid(int pid);

	NetDbgObj OpenDebuggerForPid(int pid);

	void LoadProcessesInThisReport();

	string Right(string str, int index);

	bool ProcessIsIncludedInThisReport(int pid);

	string GetPercentageString(double percentageFraction, bool bBold, bool bCapital);

	string GetTrimmedThreadDescription(int ThreadNum);

	string IsAre(int count);

	bool IsModuleLoaded(string moduleName);

	void DbgShowArray(string[] a);

	void DbgShowDebuggerExecute(string cmd);

	string DebuggerExecuteReplaceLF(string cmd, string replaceWith);

	double FromHex(string theHexStr);

	double HexToDec(string strHex);

	CacheFunctions.ScriptThreadClass GetThreadObjFromHexSystemID(double hexSystemID);

	string GetThreadIDWithLinkFromHexSystemID(double hexSystemTID);

	string GetThreadIDWithLinkFromDecSystemID(double decSystemTID);

	string Spaces(int count);

	void TraceLine(string s);

	bool FolderExists(string path);

	string GetAnalysisProcessEnvVar(string envVarName);

	void SetOSVersion();

	int GetLogicalThreadNumFromSystemTID(double systemTID);

	string EvaluateExpressionRaw(string Expression);

	string EvaluateExpressionNoErrors(string Expression);

	string EvaluateExpressionAndReportErrors(string Expression);

	string GetDwordAtSymbolAndReportErrors(string symbol);

	string GetDwordAtSymbolNoErrors(string symbol);

	string GetDwordAtSymbolRaw(string symbol);

	string GetQwordAtSymbolAndReportErrors(string symbol);

	string GetQwordAtSymbolNoErrors(string symbol);

	string GetQwordAtSymbolRaw(string symbol);

	int UBound(object[] sa);

	int UBound(string[] sa);

	int UBound_HACK_DO_NOT_USE(Array array, int dimension);

	int UBound(Array array);

	int LBound(object[] sa);

	int LBound(string[] sa);

	int LBound_HACK_DO_NOT_USE(Array array, int dimension);

	int LBound(Array array);

	int Len(string str);

	string Left(string str, int count);

	string UCase(string str);

	string EvaluateSymbolNoErrors(string symbol);

	string GetAs32BitHexString(double address);

	string GetAs64BitHexString(double address);

	double ReadULongPtr(double address);

	string GetArgAsHexString(CacheFunctions.ScriptStackFrameClass StackFrame, int nZeroBasedArgNum);

	void ReportModuleInfo();

	string CheckSymbolType(double ModuleBase);

	void ClearSubStatus();

	void IncrementSubStatus();

	void ResetStatusNoIncrement(string caption);

	void ResetStatus(string caption, int maxProgress, string subStatusTitle);

	void UpdateOverallProgress();

	void GenerateReportHeader(string DataFile, string DumpType);

	string ReturnProcessBitness();

	string GetASPTemplateWithLink(IASPTemplate ASPTemplate);

	string GetASPAppWithLink(IASPApplication ASPApp);

	string GetCritSecWithLink(string CritSecAddress);

	string GetCritSecWithLink(double CritSecAddress);

	string GetThreadAndProcessIDWithLinkOOP(NetDbgObj Debugger, int SystemThreadID);

	string GetThreadIDWithLink(int ThreadID);

	string PrintMemory(double Memory);

	string PrintTime3(double MilliSeconds);

	string PrintTime2(double Days, double Hours, double Minutes, double Seconds, double MiliSeconds);

	string PrintTime(double Seconds);

	string GetGUIDString(string pGUID);

	double Min(double a, double b);

	string PadZero(int a);

	string PadZero2(int a);

	string GetAsHexString(double address);

	string[] Split(string Expression, string Delimiter);

	string[] Split(string Expression, string Delimiter, int Count);

	string[] Split(string Expression, string Delimiter, int Count, int CompareMode);

	string Replace(string stringToPass, string find, string replaceWith);

	string LCase(string stringToCovert);

	void ModuleInfo(IDbgModule Module);

	void ModuleInfo(CacheFunctions.ScriptModuleClass Module);

	string GetVendorMessage(double p);

	double GetDirectCaller(Dictionary<int, CacheFunctions.ScriptStackFrameClass> iStackFrames, string p, int p_2);

	string GetFunctionNameNoUpper(double p);

	bool IsIISIntrinsicsStack(CacheFunctions.ScriptThreadClass ExceptionThread, out bool bIsUnmarshaling);

	string Trim(string str);

	string Join(string[] arr, string delimeter);

	string Join(string[] arr);

	double FormatNumber(double Number, int NumDigAfterDec);

	double FormatNumber(double Number);

	string TypeName(object obj);

	long CLng(string num);

	long CLng(int num);

	long CLng(double num);

	int Round(double numToRound);

	DateTime CDateTypeSafe(string DateExpression);

	string CDate(string DateExpression);

	bool IsDate(string DateExpression);

	DateTime DateAdd(string interval, int number, DateTime date);

	DateTime DateAdd(string interval, int number, string date);

	double Fix(double Number);

	int Day(DateTime Date);

	int Day(string Date);

	string Now();

	DateTime NowTypeSafe();

	double CDbl(string num);

	double CDbl(int num);

	char Chr(int charCode);

	int CInt(string num);

	int CInt(double num);

	int CInt(long num);

	string CStr(bool expression);

	string CStr(DateTime date);

	string CStr(int num);

	string CStr(double num);

	string CStr(long num);

	string Hex(int num);

	bool IsNullOrEmpty(object o);

	string GetSpecialSTABlurb(string sSTAType);
}
