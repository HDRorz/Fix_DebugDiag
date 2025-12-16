using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DebugDiag.DbgLib;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.HtmlHelpers;
using DebugDiag.DotNet.Reports;
using DebugDiag.DotNet.Util;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet;

/// <summary>
/// This object is the main object for analyzer. When analyzer runs a rule an instance of this object is made available to the rule object automatically.  
/// The NetScriptManager object is used to obtain an instance of the <see cref="T:DebugDiag.DotNet.NetDbgObj" /> object, to report errors, warnings and other information, 
/// as well as manage the data files that will be analyzed.
/// </summary>
/// <remarks>
/// There are 2 ways to obtain an instance of a NetScriptManager, the first example shows how to obtain a reference by calling the Manager property of the NetAnalyzer object.
/// The second example shows how to use the NetScriptManger when writting a custom Rule.
/// <example>
/// <code language="cs">
/// //Create an instance of the NetAnalyzer object to access the NetScriptManager
/// using (NetAnalyzer analyzer = new NetAnalyzer())
/// {
///     //Create a list of dumps that will be analyzed
///     List&lt;string&gt; dumpFiles = new List&lt;string&gt;();
///     dumpFiles.Add(@"c:\user.dmp");
///     dumpFiles.Add(@"c:\test.dmp");
///
///     //Add Dump list to Analizer object so we can analyze them with the debugger
///     analyzer.AddDumpFiles(dumpFiles, @"srv*c:\symsrv*http://msdl.microsoft.com/download/symbols");
///
///     //Obtain the NetScriptManager object from the analyzer object to be able to customize the report output
///     NetScriptManager manager = analyzer.Manager;
///
///     //Write on the report Output using the Write function
///     manager.Write("There are " + dumpFiles.Count.ToString() + " files in this DataFiles collection.&lt;/BR&gt;");
///     manager.Write("The DataFiles collection contains the following files:&lt;/BR&gt;&lt;/BR&gt;");
///
///     foreach (string dump in dumpFiles)
///     {
///         manager.Write(dump + "&lt;/BR&gt;");
///     }
///
///     //Write on the report Summary
///     manager.ReportError("This is the &lt;b&gt;error&lt;/b&gt; description text.", "This is the error recommendation text.");
///     manager.ReportWarning("This is the warning description text.", "This is the warning recommendation text.");
///     manager.ReportInformation("This is the information text. There is no recommendation text, since this is informational only.");
///
///     //Since we are not really executing a rule in this example I just write the report file to see the results
///     //manager.WriteReportFile(manager);
///
/// } 
/// </code>
/// <code language="cs">
/// public class ImplementedRule : IMultiDumpRule, IAnalysisRuleMetadata
///             {
///    public void RunAnalysisRule(DebugDiag.DotNet.NetScriptManager manager, DebugDiag.DotNet.NetProgress Progress)
///    {
///
///
///        //Create a list of dumps that will be analyzed
///        List&lt;string&gt; dumpFiles = new List&lt;string&gt;();
///
///        dumpFiles = manager.GetDumpFiles();
///
///        //Write on the report Output using the Write function
///        manager.Write("There are " + dumpFiles.Count.ToString() + " files in this DataFiles collection.&lt;/BR&gt;");
///        manager.Write("The DataFiles collection contains the following files:&lt;/BR&gt;&lt;/BR&gt;");
///
///        foreach (string dump in dumpFiles)
///        {
///            using (NetDbgObj debugger = manager.GetDebugger(dump))
///            {
///                manager.Write(dump + " : " + (debugger.Is32Bit ? "x86" : "x64") + "&lt;/BR&gt;");
///            }
///        }
///
///        //Write on the report Summary
///        manager.ReportError("This is the &lt;b&gt;error&lt;/b&gt; description text.", "This is the error recommendation text.");
///        manager.ReportWarning("This is the warning description text.", "This is the warning recommendation text.");
///        manager.ReportInformation("This is the information text. There is no recommendation text, since this is informational only.");   
///    }
///
///    public string Category
///    {
///      get { return "Samples"; }
///    }
///
///    public string Description
///    {
///      get { return "Show me the Bitness"; }
///    }
///
///             }
/// </code>
/// </example>
/// </remarks>
public class NetScriptManager : IDisposable
{
	private ScriptManager _legacyManager;

	private bool _disposed;

	private NetAnalyzer _analyzer;

	private string _reportFileFullPath = "";

	private NetProgress _defaultProgress = new NetProgress();

	private NetResults _x86Results;

	private List<object> _facts = new List<object>();

	private string _currentAnalysisRule;

	private List<object> _x86facts;

	private NetResults _results = new NetResults();

	private HashSet<string> _resultsSet = new HashSet<string>();

	internal ReportSection _details = new ReportSection("Default");

	private ReportSection _currentSection;

	private ReportJsManager jsManager;

	private string _currentDumpName = "";

	private static bool reportFacts;

	private Dictionary<string, NetDbgObj> _debuggersWithclrmdInitExceptions = new Dictionary<string, NetDbgObj>();

	private Dictionary<string, NetDbgObj> _debuggersWithMultipleClrRuntimes = new Dictionary<string, NetDbgObj>();

	internal bool BitnessErrorReported;

	private static ConcurrentDictionary<Type, bool> _isSerializeableCache;

	public NetAnalyzer Analyzer => _analyzer;

	internal bool IncludeHttpHeadersInClientConns { get; set; }

	/// <summary>
	///
	/// </summary>
	public bool SetContextOnCrashDumps { get; set; }

	/// <summary>
	/// This property allows you to enable or disable source information on the callstacks printed on the analysis results
	/// </summary>
	public bool SourceInfoEnabled { get; set; }

	/// <summary>
	/// This property allows you to enable or disable showing the Instruction Pointer Address on the callstacks printed on the analysis results
	/// </summary>
	public bool InstructionAddressEnabled { get; set; }

	/// <summary>
	/// If set to true, this property enables to perform a Hang Analysis along with a Crash Analysis for a Crash dump
	/// </summary>
	public bool DoHangAnalysisOnCrashDumps { get; set; }

	/// <summary>
	/// If set to true, this property causes CrashHangAnalysis reprt files to group all threads together wich have identicall call stacks
	/// </summary>
	public bool GroupIdenticalStacks { get; set; }

	/// <summary>
	/// This property returns a reference to the ReportSection where report details are being written for the rule is used for internal
	/// rules to avoid changing too much code while implementing the new object model for the reports.
	/// </summary>
	public ReportSection CurrentSection
	{
		get
		{
			if (_currentSection == null)
			{
				_currentSection = _details;
			}
			return _currentSection;
		}
		set
		{
			_currentSection = value;
		}
	}

	/// <summary>
	/// This property returns a reference to the ReportSection where report details are being written for the rule.
	/// </summary>
	public ReportSection DefaultSection
	{
		get
		{
			if (_currentSection == null)
			{
				_currentSection = _details;
			}
			return _currentSection;
		}
	}

	/// <summary>
	/// Returns an instance of the <c>NetProgress</c> object which is used to report the script progress to DebugDiag.
	/// </summary>
	public NetProgress Progress
	{
		get
		{
			NetProgress netProgress = _legacyManager.Progress as NetProgress;
			if (netProgress == null)
			{
				netProgress = _defaultProgress;
			}
			return netProgress;
		}
	}

	/// <summary>
	/// Returns the name of the current Analysis Rule
	/// </summary>           
	public string CurrentAnalysisRule
	{
		get
		{
			return _currentAnalysisRule;
		}
		set
		{
			_currentAnalysisRule = value;
		}
	}

	static NetScriptManager()
	{
		reportFacts = true;
		_isSerializeableCache = new ConcurrentDictionary<Type, bool>();
		if (bool.TryParse(ConfigurationManager.AppSettings["ReportFacts"], out var result))
		{
			reportFacts = result;
		}
	}

	internal NetScriptManager(ScriptManager legacyManager, NetAnalyzer analyzer)
	{
		_legacyManager = legacyManager;
		_analyzer = analyzer;
		_analyzer.PreExecuteRule += Analisis_PreExecuteRule;
		_analyzer.PostExecuteRule += Analisis_PostExecuteRule;
		jsManager = new ReportJsManager(this);
	}

	/// <summary>
	/// This method returns the full path for the report.
	/// </summary>
	/// <returns>String with the full file path</returns>
	public string GetReportFileFullPath()
	{
		return _reportFileFullPath;
	}

	/// <summary>
	/// This method returns a string using the ResourceManager and the resourceKey associated with the string 
	/// </summary>
	/// <param name="resourceKey">Resource key associated with the string</param>
	/// <returns>String stored on the resources</returns>
	public string GetResource(string resourceKey)
	{
		string text = "";
		if (Path.GetExtension(resourceKey).Length == 0)
		{
			throw new ArgumentException("resourceKey must include file extension (i.e. 'up.gif'");
		}
		bool returnBase = ResourceNeedsEncoding(resourceKey);
		using (MemoryStream memoryStream = GetResourceFromAssembly(resourceKey, returnBase))
		{
			if (memoryStream.Length > 0)
			{
				using StreamReader streamReader = new StreamReader(memoryStream);
				text = streamReader.ReadToEnd();
			}
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		throw new Exception($"resourceKey could not be found: {resourceKey}");
	}

	private bool ResourceNeedsEncoding(string resourceKey)
	{
		string text = Path.GetExtension(resourceKey).ToUpper();
		if (text.Substring(0, 1) != ".")
		{
			throw new ArgumentException("resourceKey must include file extension (i.e. 'up.gif'");
		}
		switch (text)
		{
		case ".HTM":
		case ".JS":
		case ".CSS":
			return false;
		default:
			return true;
		}
	}

	internal MemoryStream GetResourceFromAssembly(string ResourceKey, bool ReturnBase64 = false)
	{
		Assembly assembly = Assembly.GetAssembly(typeof(NetScriptManager));
		MemoryStream memoryStream = new MemoryStream();
		using (Stream stream = assembly.GetManifestResourceStream("DebugDiag.DotNet.Resources." + ResourceKey))
		{
			if (stream != null)
			{
				if (!ReturnBase64)
				{
					if (stream.ReadByte() == 239)
					{
						stream.ReadByte();
						stream.ReadByte();
					}
					else
					{
						stream.Position = 0L;
					}
					stream.CopyTo(memoryStream);
				}
				else
				{
					ToBase64Transform toBase64Transform = new ToBase64Transform();
					byte[] array = new byte[toBase64Transform.OutputBlockSize];
					byte[] array2 = new byte[stream.Length];
					stream.Read(array2, 0, array2.Length);
					byte[] buffer = new byte[2] { 13, 10 };
					if (!toBase64Transform.CanTransformMultipleBlocks)
					{
						int num = 0;
						int num2 = 0;
						int inputBlockSize = toBase64Transform.InputBlockSize;
						while (array2.Length - num > inputBlockSize)
						{
							toBase64Transform.TransformBlock(array2, num, array2.Length - num, array, 0);
							num += toBase64Transform.InputBlockSize;
							memoryStream.Write(array, 0, toBase64Transform.OutputBlockSize);
							num2 += toBase64Transform.OutputBlockSize;
							if (num2 >= 76)
							{
								memoryStream.Write(buffer, 0, 2);
								num2 = 0;
							}
						}
						array = toBase64Transform.TransformFinalBlock(array2, num, array2.Length - num);
						memoryStream.Write(array, 0, array.Length);
					}
					if (!toBase64Transform.CanReuseTransform)
					{
						toBase64Transform.Clear();
					}
				}
			}
		}
		memoryStream.Position = 0L;
		return memoryStream;
	}

	private string GetTableOfContent()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<div id=\"tableOfContents\" class=\"fixedTOCTopDiv\"> <table class=\"fixedTOCTable\">  <tbody>   <tr>    <td class=\"fixedTOCLabel\"><img src=\"res/toc.png\" /></td>    <td>");
		stringBuilder.Append(_details.GenerateTOC(10));
		stringBuilder.Append("    </td>   </tr>  </tbody> </table></div>");
		return stringBuilder.ToString();
	}

	/// <summary>
	/// This function returns the Summary Table of Contents of the report in html format
	/// </summary>
	/// <returns>html table with summary of the report</returns>
	public string GetScriptsSummaryTableContents()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (AnalysisRuleInfo analysisRuleInfo in _analyzer.AnalysisRuleInfos)
		{
			string arg = string.Empty;
			if (analysisRuleInfo is CodeAnalysisRuleInfo codeAnalysisRuleInfo)
			{
				AssemblyName name = codeAnalysisRuleInfo._analysisRuleType.Assembly.GetName();
				arg = $"- <i>v ({name.Version.ToString()})</i>";
			}
			stringBuilder.Append($"<tr class=\"gridrowspacing\" data-is-row=\"true\" data-debug-rule=\"{analysisRuleInfo.DisplayName}\">");
			stringBuilder.Append($"<td class=\"gridrowspacing\">{analysisRuleInfo.DisplayName} {arg}</td>");
			stringBuilder.Append("<td class=\"gridrowspacing\">");
			if (analysisRuleInfo.Exception != null)
			{
				Exception exception = analysisRuleInfo.Exception;
				string recentDumpFilePath = analysisRuleInfo.RecentDumpFilePath;
				stringBuilder.Append("<font color='red'><b>Failed</b></font></td><td class=\"gridrowspacing\">");
				stringBuilder.Append(FormatExceptionForReport(exception, recentDumpFilePath));
				stringBuilder.Append("</td></tr>\n");
			}
			else if (analysisRuleInfo.WasFiltered)
			{
				string text = (string.IsNullOrEmpty(analysisRuleInfo.FilterReason) ? "This rule did not apply to any of the dumps selected for analysis." : analysisRuleInfo.FilterReason);
				stringBuilder.Append("<font color='darkgray'><b>Skipped</b></font></td><td class=\"gridrowspacing\">" + text + "</td></tr>\n");
			}
			else if (analysisRuleInfo.Status == AnalysisRuleStatus.Complete)
			{
				string text2 = (string.IsNullOrEmpty(analysisRuleInfo.FilterReason) ? "&nbsp;" : analysisRuleInfo.FilterReason);
				stringBuilder.Append("<font color='darkgreen'><b>Completed</b></font></td><td class=\"gridrowspacing\">" + text2 + "</td></tr>\n");
			}
			else if (analysisRuleInfo.Status == AnalysisRuleStatus.NotStarted)
			{
				if (BitnessErrorReported)
				{
					stringBuilder.Append("<font color='darkgray'><b>Skipped</b></font></td><td class=\"gridrowspacing\">Bitness mismatch.  See the 'Notification' section at the top of this report.</td></tr>\n");
				}
				else if (_analyzer.DataFilesIncludesCrashDumps)
				{
					stringBuilder.Append("<font color='darkgray'><b>Skipped</b></font></td><td class=\"gridrowspacing\">This rule is a <b>hang</b> rule.  It only applies to <b>hang</b> dumps (dumps without an \"exception of interest\").  Only <b>crash</b> dumps (dumps containing an \"exception of interest\") were selected for analysis, so this rule was not executed. ");
					stringBuilder.Append("To run both <b>hang</b> rules and <b>crash</b> rules on <b>crash</b> dumps, select the following option in the <i>'Preferences'</i> tab of the <i>'Settings'</i> page in DebugDiag.Analysis.exe:");
					stringBuilder.Append("<br><br>&nbsp;&nbsp;&nbsp;&nbsp;<i>'For crash dumps, run hang rules and crash rules'</i></td></tr>\n");
				}
				else
				{
					stringBuilder.Append("<font color='darkgray'><b>Skipped</b></font></td><td class=\"gridrowspacing\">This rule is a <b>crash</b> rule.  It only applies to <b>crash</b> dumps (dumps containing an \"exception of interest\").  Only <b>hang</b> dumps (dumps without an \"exception of interest\") were selected for analysis, so this rule was not executed.</td></tr>\n");
				}
			}
			else
			{
				stringBuilder.Append($"<font color='orange'><b>Unexpected: {analysisRuleInfo.Status}</b></font></td><td class=\"gridrowspacing\">This rule did not complete succcessfully.  Try running the analysis again.</td></tr>\n");
			}
		}
		return stringBuilder.ToString();
	}

	internal static string FormatExceptionForReport(Exception ex, string dumpShortName)
	{
		Type type = ex.GetType();
		string text = "<b>Dump File:</b> &nbsp{0}<BR><br><b>Type: &nbsp<font color='red'>{1}.{2}</b></font><BR><br><b>Message:  &nbsp</b><font color='black'>{3}</font><BR><br>";
		if (ex.StackTrace != null)
		{
			text += "<b>Stack Trace:</b><BR>{4}";
		}
		string text2 = string.Format(text, dumpShortName, type.Namespace, type.Name, ex.Message, FormatCallStackForReport(ex.StackTrace));
		if (ex.InnerException != null)
		{
			text2 = text2 + "<br><br><b>Inner Exception:</b><br><br><div style=margin-left:50px>" + FormatExceptionForReport(ex.InnerException, dumpShortName) + "</div>";
		}
		return text2;
	}

	private static string FormatCallStackForReport(string stackTrace)
	{
		if (string.IsNullOrEmpty(stackTrace))
		{
			return string.Empty;
		}
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = stackTrace.Split("\r\n".ToArray());
			foreach (string text in array)
			{
				if (text.Length <= 0)
				{
					continue;
				}
				string text2 = null;
				string text3 = null;
				string text4 = null;
				if (text.IndexOf("at ") > -1)
				{
					int num = text.IndexOf("(");
					if (num > -1)
					{
						if (text.IndexOf("DebugDiag.DotNet") == -1)
						{
							text3 = "<b>";
							text4 = "</b>";
						}
						string text5 = text.Substring(6, num - 6);
						int num2 = text5.LastIndexOf(".");
						text2 = ((num2 <= -1) ? $"{text5}<font color='red'>{text.Substring(num)}</font>" : $"{text5.Substring(0, num2 + 1)}{text3}<font color='red'>{text5.Substring(num2 + 1)}{text4}</font>{text.Substring(num)}");
					}
				}
				int num3 = text.IndexOf(":line ");
				if (num3 > -1)
				{
					num3 = text2.LastIndexOf('\\');
					text2 = $"{text2.Substring(0, num3 + 1)}{text3}<font color='blue'>{text2.Substring(num3 + 1)}</font>{text4}";
				}
				stringBuilder.Append((text2 != null) ? text2 : text);
				stringBuilder.Append("<BR>");
			}
			return stringBuilder.Replace("\r\n", "<BR>").ToString();
		}
		catch (Exception)
		{
			return stackTrace;
		}
	}

	private MemoryStream FormatReport()
	{
		MemoryStream memoryStream = new MemoryStream();
		MemoryStream resourceFromAssembly;
		using (resourceFromAssembly = GetResourceFromAssembly("NewReportText1.htm"))
		{
			resourceFromAssembly.WriteTo(memoryStream);
		}
		byte[] encodedBytes = MIMEHelperFunctions.GetEncodedBytes(GetResultsSummary());
		memoryStream.Write(encodedBytes, 0, encodedBytes.Length);
		using (resourceFromAssembly = GetResourceFromAssembly("NewReportText2.htm"))
		{
			resourceFromAssembly.WriteTo(memoryStream);
		}
		using (resourceFromAssembly = _details.RenderHTML())
		{
			resourceFromAssembly.WriteTo(memoryStream);
		}
		using (resourceFromAssembly = GetResourceFromAssembly("NewReportText3.htm"))
		{
			resourceFromAssembly.WriteTo(memoryStream);
		}
		encodedBytes = MIMEHelperFunctions.GetEncodedBytes(GetScriptsSummaryTableContents());
		memoryStream.Write(encodedBytes, 0, encodedBytes.Length);
		using (resourceFromAssembly = GetResourceFromAssembly("NewReportText4.htm"))
		{
			resourceFromAssembly.WriteTo(memoryStream);
		}
		encodedBytes = MIMEHelperFunctions.GetEncodedBytes(BuildReportFilters());
		memoryStream.Write(encodedBytes, 0, encodedBytes.Length);
		encodedBytes = MIMEHelperFunctions.GetEncodedBytes(GetTableOfContent());
		memoryStream.Write(encodedBytes, 0, encodedBytes.Length);
		using (resourceFromAssembly = GetResourceFromAssembly("NewReportText5.htm"))
		{
			resourceFromAssembly.WriteTo(memoryStream);
			return memoryStream;
		}
	}

	private string BuildReportFilters()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, ReportSection> innerSection in _details.InnerSections)
		{
			if (innerSection.Value.SectionType != SectionType.Rule)
			{
				continue;
			}
			list.Add(innerSection.Key);
			foreach (KeyValuePair<string, ReportSection> kpdump in innerSection.Value.InnerSections)
			{
				if (kpdump.Value.SectionType == SectionType.Dump && !list2.Exists((string x) => x == kpdump.Key))
				{
					list2.Add(kpdump.Key);
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(ReportHelper.BuildFilter(list2, "memoryDump", "data-debug-dump"));
		stringBuilder.Append(ReportHelper.BuildFilter(list, "rule", "data-debug-rule"));
		return stringBuilder.ToString();
	}

	private string GetResultsSummary()
	{
		Dictionary<string, string> resultGroups = GetResultGroups();
		StringBuilder stringBuilder = new StringBuilder();
		List<NetResult> list = new List<NetResult>();
		foreach (KeyValuePair<string, string> group in resultGroups)
		{
			stringBuilder.Append(ReportHelper.BuildReportSummaryTable(results: (!(group.Key == "Warning") && !(group.Key == "Error") && !(group.Key == "Information")) ? _results.FindAll((NetResult result) => result.Type.ToUpper() == "OTHER" && result.TypeLabel.ToUpper() == group.Key.ToUpper()) : _results.FindAll((NetResult result) => result.Type.ToUpper() == group.Key.ToUpper()), summaryLabel: group.Key.Replace(" ", ""), iconFile: group.Value));
		}
		return stringBuilder.ToString();
	}

	private Dictionary<string, string> GetResultGroups()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (_results.Count == 0)
		{
			return dictionary;
		}
		if (_results.Count > 1)
		{
			IComparer<NetResult> comparer = NetResult.SortResult();
			_results.Sort(comparer);
		}
		string text = "";
		string text2 = "";
		foreach (NetResult result in _results)
		{
			if (text != result.Type)
			{
				if (!dictionary.ContainsKey(result.Type))
				{
					if (result.Type == "Other")
					{
						dictionary.Add(result.TypeLabel, result.IconFileName);
					}
					else
					{
						dictionary.Add(result.Type, result.IconFileName);
					}
				}
				text = result.Type;
				text2 = result.TypeLabel;
			}
			else if (result.Type == "Other" && text2 != result.TypeLabel)
			{
				if (!dictionary.ContainsKey(result.TypeLabel))
				{
					dictionary.Add(result.TypeLabel, result.IconFileName);
				}
				text2 = result.TypeLabel;
			}
		}
		return dictionary;
	}

	private void CreateRealReport(MemoryStream fullReport)
	{
		if (fullReport == null)
		{
			throw new ArgumentException("The HTML content for the report is missing, the memorystream is null", "fullReport");
		}
		if (fullReport.Length == 0L)
		{
			fullReport.Close();
			throw new ArgumentException("The HTML content for the report is missing, the memorystream is empty", "fullReport");
		}
		using FileStream output = File.Open(GetReportFileFullPath(), FileMode.Create);
		Encoding encoding = Encoding.GetEncoding("utf-8");
		using BinaryWriter binaryWriter = new BinaryWriter(output, encoding);
		binaryWriter.Write(MIMEHelperFunctions.FileHeader());
		using (fullReport)
		{
			MIMEHelperFunctions.QPEncode(fullReport, binaryWriter);
		}
		string[] array = new string[28]
		{
			"error_52x52.png", "collapsedButton.png", "expandedButton.png", "warning_57x57.png", "up.png", "down.png", "notificationicon.png", "settings_97x52.png", "verticalLine_2x117.png", "notification_52x52.png",
			"information_52x52.png", "filter_30x30.png", "closeButton.png", "tocCollapsed.png", "tocExpanded.png", "collapsedButton_21x21.png", "expandedButton_21x21.png", "toc.png", "ui-bg_glass_65_ffffff_1x400.png", "ui-bg_glass_75_dadada_1x400.png",
			"ui-bg_glass_75_e6e6e6_1x400.png", "erroricon.png", "warningicon.png", "notificationicon.png", "informationicon.png", "vkbicon.png", "IISDiag-Analysis.png", "debugdiag-analysis.png"
		};
		foreach (string text in array)
		{
			binaryWriter.Write(MIMEHelperFunctions.ImageHeader(text));
			using MemoryStream memoryStream2 = GetResourceFromAssembly(text, ReturnBase64: true);
			memoryStream2.CopyTo(binaryWriter.BaseStream);
		}
		array = new string[3] { "main.css", "jquery-1.11.0.min.js", "jquery-ui-1.10.4.custom.min.js" };
		foreach (string text2 in array)
		{
			string contentType;
			string container;
			if (text2.EndsWith("css"))
			{
				contentType = "text/css";
				container = "res";
			}
			else
			{
				if (!text2.EndsWith("js"))
				{
					throw new FileFormatException("The file to be included on the MHT file was not recognized as a valid text file (*.js, *.css)");
				}
				contentType = "text/javascript";
				container = "scripts/jquery";
			}
			binaryWriter.Write(MIMEHelperFunctions.TextHeader(text2, container, contentType));
			using MemoryStream source = GetResourceFromAssembly(text2);
			MIMEHelperFunctions.QPEncode(source, binaryWriter);
		}
		using (MemoryStream source2 = jsManager.CreateReportFunctions())
		{
			binaryWriter.Write(MIMEHelperFunctions.TextHeader("ReportFunctions.js", "scripts", "text/javascript"));
			MIMEHelperFunctions.QPEncode(source2, binaryWriter);
		}
		binaryWriter.Write(MIMEHelperFunctions.FileFooter());
	}

	/// <summary>
	/// Release all the native resources, the NetAnalyzer.Dispose function also calls the NetScriptManager.Dispose function.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Release all the native resources, the NetAnalyzer.Dispose function also calls the NetScriptManager.Dispose function.
	/// </summary>
	/// <param name="disposing">Disposing</param>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			if (_legacyManager != null)
			{
				Marshal.FinalReleaseComObject(_legacyManager);
			}
			_currentSection = null;
			if (_details != null)
			{
				_details.Dispose();
			}
		}
		_legacyManager = null;
		_disposed = true;
	}

	internal void Initialize(bool includeSourceAndLineInformationInAnalysisReports, bool setContextOnCrashDumps, bool doHangAnalysisOnCrashDumps, bool includeHttpHeadersInClientConns, bool groupIdenticalStacks, bool includeInstructionPointerInAnalysisReports)
	{
		InstructionAddressEnabled = includeInstructionPointerInAnalysisReports;
		SourceInfoEnabled = includeSourceAndLineInformationInAnalysisReports;
		IncludeHttpHeadersInClientConns = includeHttpHeadersInClientConns;
		SetContextOnCrashDumps = setContextOnCrashDumps;
		DoHangAnalysisOnCrashDumps = doHangAnalysisOnCrashDumps;
		GroupIdenticalStacks = groupIdenticalStacks;
	}

	/// <summary>
	/// This method returns a <c>NetDbgObj</c> object. The string passed into the method should contain the full path to the filename of a *.dmp file 
	/// that is to be analyzed.
	/// </summary>
	/// <param name="DumpFile">Dump File Name including the path information</param>
	/// <returns>NetDbgObj instance reference</returns>
	public NetDbgObj GetDebugger(string DumpFile)
	{
		return GetDebugger(DumpFile, throwOnBitnessMismatch: false, loadClrRuntime: true, loadClrHeap: true);
	}

	/// <summary>
	/// This method returns a <c>NetDbgObj</c> object. The string passed into the method should contain the full path to the filename of a *.dmp file 
	/// that is to be analyzed.
	/// </summary>
	/// <param name="DumpFile">Dump File Name including the path information</param>
	/// <param name="throwOnBitnessMismatch">Throw an exception if the bitness of the target process is different from that of the calling process (default: false)</param>
	/// <param name="loadClrRuntime">Initialize the CLR runtime-related properties if the CLR is loaded in the target process (default: true)</param>
	/// <param name="loadClrHeap">Initialize the CLR heap-related properties if the CLR is loaded in the target process (default: true)</param>
	/// <returns>NetDbgObj instance reference</returns>
	internal NetDbgObj GetDebugger(string DumpFile, bool throwOnBitnessMismatch = false, bool loadClrRuntime = true, bool loadClrHeap = true)
	{
		NetDbgObj netDbgObj = NetDbgObj.OpenDump(DumpFile, _analyzer.SymbolPath, _analyzer.ImagePath, _analyzer.Progress, throwOnBitnessMismatch, loadClrRuntime, loadClrHeap);
		if (netDbgObj.ClrInitException != null)
		{
			AddClrmdInitException(netDbgObj);
		}
		if (netDbgObj.HasMultipleClrRuntimesWithThreads)
		{
			AddMultipleClrRuntimeDump(netDbgObj);
		}
		if (SetContextOnCrashDumps)
		{
			netDbgObj.SetContextFromExceptionRecord();
		}
		_currentDumpName = Path.GetFileName(DumpFile);
		string shadowCopyDir = NetAnalyzer.ShadowCopyDir;
		netDbgObj.PrependExtPath(shadowCopyDir);
		netDbgObj.PrependExtPath(Path.Combine(shadowCopyDir, "EXTS"));
		netDbgObj.PrependExtPath(Path.Combine(shadowCopyDir, "EXTS", Environment.Is64BitProcess ? "x64" : "x86"));
		return netDbgObj;
	}

	/// <summary>
	/// Method to write a Name-Value pair on the report file using some basic formating
	/// </summary>
	/// <param name="name">String representing the Name of the pair</param>
	/// <param name="value">String representing the Value of the pair</param>
	/// <param name="bold">Optional Boolean value indicating if the text should be formated as bold, the default is true</param>
	/// <param name="color">Optional string indicating the color for the text, default value is null</param>
	/// <param name="size">Optional text size</param>
	public void WriteNameValuePair(string name, string value, bool bold = true, string color = null, int size = 0)
	{
		CurrentSection.WriteNameValuePair(name, value, bold, color, size);
	}

	/// <summary>
	/// Method to write an Empty line on the Report
	/// </summary>
	/// <overloads>This method has two more overloads</overloads>
	public void WriteLine()
	{
		CurrentSection.WriteLine();
	}

	/// <summary>
	/// Method to write string with a libe break on the report
	/// </summary>
	/// <param name="output">String value to write on the report</param>
	/// <param name="bold">Boolean to indicate if the string should be formated as Bold</param>
	/// <param name="color">String value to apply a color to the text</param>
	/// <param name="size">Integer value to indicate the size for the text</param>
	public void WriteLine(string output, bool bold, string color = null, int size = 0)
	{
		CurrentSection.WriteLine(output, bold, color, size);
	}

	/// <summary>
	/// Method to write string with a libe break on the report
	/// </summary>
	/// <param name="output">String value to write on the report</param>
	public void WriteLine(string output)
	{
		CurrentSection.WriteLine(output);
	}

	/// <summary>
	/// Method to write data with a line break on the report after formatting via String.Format.
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="bold">Boolean value indicating if the text should be formated as bold</param>
	/// <param name="color">String indicating the color to apply on the text</param>
	/// <param name="size">Integer indicating the size of the text</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void WriteLine(string format, bool bold, string color, int size = 0, params object[] args)
	{
		CurrentSection.WriteLine(format, bold, color, size, args);
	}

	/// <summary>
	/// Method to write string with a line break on the report after formatting via String.Format.
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void WriteLine(string format, params object[] args)
	{
		CurrentSection.WriteLine(format, args);
	}

	/// <summary>
	/// Function that will create the report file for code that is not implemented as a rule
	/// </summary>
	/// <param name="reportFileFullPath">Path where the report file should be generated</param>
	public void WriteReportFile(string reportFileFullPath)
	{
		_reportFileFullPath = reportFileFullPath;
		WriteReportFile();
	}

	/// <summary>
	/// This method returns a List of strings containing the  file names to be analyzed.
	/// </summary>
	/// <returns>String list with the dump file names</returns>
	public List<string> GetDumpFiles()
	{
		return _analyzer.GetDumpFiles();
	}

	internal void AddClrmdInitException(NetDbgObj debugger)
	{
		string key = debugger.DumpFileFullPath.ToUpper();
		if (!_debuggersWithclrmdInitExceptions.ContainsKey(key))
		{
			_debuggersWithclrmdInitExceptions.Add(key, debugger);
		}
	}

	internal void AddMultipleClrRuntimeDump(NetDbgObj debugger)
	{
		string key = debugger.DumpFileFullPath.ToUpper();
		if (!_debuggersWithMultipleClrRuntimes.ContainsKey(key))
		{
			_debuggersWithMultipleClrRuntimes.Add(key, debugger);
		}
	}

	internal void ReportClrmdInitExceptions()
	{
		if (_debuggersWithclrmdInitExceptions.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder("<b><font color='red'>Analysis results may be incomplete</font></b> because an error occurred while initializing the CLR diagnostic runtime for ");
		StringBuilder stringBuilder2 = new StringBuilder();
		if (_debuggersWithclrmdInitExceptions.Count == 1)
		{
			foreach (NetDbgObj value in _debuggersWithclrmdInitExceptions.Values)
			{
				stringBuilder.AppendFormat("<b>{0}</b>.<br>", value.DumpFileShortName);
			}
		}
		else
		{
			stringBuilder.Append("the following dump files:<br><br>");
			foreach (NetDbgObj value2 in _debuggersWithclrmdInitExceptions.Values)
			{
				stringBuilder.AppendFormat("&nbsp;&nbsp;&nbsp;{0}<br>", value2.DumpFileShortName);
			}
			stringBuilder.Append("<br>Exception information for the first failure is provided below:<br>");
		}
		using (Dictionary<string, NetDbgObj>.ValueCollection.Enumerator enumerator = _debuggersWithclrmdInitExceptions.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				NetDbgObj current3 = enumerator.Current;
				stringBuilder.Append("<br><br>");
				stringBuilder.Append(FormatExceptionForReport(current3.ClrInitException, current3.DumpFileShortName));
				if (current3.ClrInitException is DacNotFoundException)
				{
					string dacFileName = current3.DacFileName;
					stringBuilder2.Append("To fix this problem, you can copy mscordacwks.dll from the server where the dump was taken and rename it to <b> " + dacFileName + "</b> and add the path of the folder to the <b>Symbol server path </b> by going to <b>Tools-> Options and Settings </b> <br/> <br/> For more details on this issue, please refer to <a href='http://blogs.msdn.com/b/dougste/archive/2009/02/18/failed-to-load-data-access-dll-0x80004005-or-what-is-mscordacwks-dll.aspx'>this</a> blog.");
					if (current3.ClrInitException is DacNotFoundException ex && !string.IsNullOrEmpty(ex.SymbolProbeInfo))
					{
						string newValue = "<br>&nbsp;&nbsp;&nbsp;";
						stringBuilder2.AppendFormat("<br/><br/>The following symbol probe information may be helpful:<br/>{0}{1}", "&nbsp;&nbsp;&nbsp;", ex.SymbolProbeInfo.Replace("\r\n", newValue));
					}
				}
				if (current3.ClrInitException is ClrDiagnosticsException)
				{
					stringBuilder.AppendFormat("<br><b>HResult:  </b>0x{0:X8}", ((ClrDiagnosticsException)current3.ClrInitException).HResult);
					stringBuilder2.Append("This message means that the CLR Runtime is loaded but the ThreadStore or GC Heap information is not initialized.");
				}
			}
		}
		ReportOther(stringBuilder.ToString(), stringBuilder2.ToString(), "Notification", "notificationicon.png", 0, "{b0c0cfb1-3ac6-4000-a9e5-8ffc6d1cdfff}");
	}

	private void ReportMultipleClrRuntimes()
	{
		if (_debuggersWithMultipleClrRuntimes.Count == 0)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder("<b><font color='red'>Analysis results may be incomplete</font></b> because multiple CLR Runtimes were detected in ");
		string text = "each dump";
		if (_debuggersWithMultipleClrRuntimes.Count == 1)
		{
			foreach (NetDbgObj value in _debuggersWithMultipleClrRuntimes.Values)
			{
				stringBuilder.AppendFormat("<b>{0}</b>.<br>", value.DumpFileShortName);
			}
		}
		else
		{
			text = "these dumps";
			stringBuilder.Append("the following dump files:<br><br>");
			foreach (NetDbgObj value2 in _debuggersWithMultipleClrRuntimes.Values)
			{
				stringBuilder.AppendFormat("&nbsp;&nbsp;&nbsp;{0}<br>", value2.DumpFileShortName);
			}
		}
		stringBuilder.Append("<br>Analysis of multiple CLR Runtimes in a single process is only partially supported in this version of DebugDiag.  Call stacks for all runtimes will be shown, but only a single CLR Runtime has been selected for other types of analysis (i.e. searching for previous exceptions or HTTP requests).");
		string text2 = "In most cases the most relevant CLR runtime will be selected automatically.  Manual dump analysis may be required if the automated analysis of the selected runtime is not sufficient.";
		if (_analyzer.AnalysisRuleInfos.Where((AnalysisRuleInfo ruleInfo) => ruleInfo.DisplayName.ToUpper() == "CRASHHANGANALYSIS").FirstOrDefault() != null)
		{
			text2 = text2 + "<br><br>See the <b>CLR Information</b> section of the CrashHanagAnalysis report for " + text + " for details on which runtimes are present and which were selected.";
		}
		ReportOther(stringBuilder.ToString(), text2, "Notification", "notificationicon.png", 0, "{33CB6BF8-B3F5-4EE2-BFF3-3C189568A06D}");
	}

	private void ReportFacts()
	{
		try
		{
			if (_facts.Count <= 0)
			{
				return;
			}
			string arg = ((_facts.Count == 1) ? "" : "s");
			string arg2 = ((_facts.Count == 1) ? "was" : "were");
			ReportSection reportSection = _details.AddChildSection("Facts");
			reportSection.Collapsible = true;
			reportSection.Collapsed = true;
			reportSection.WriteLine("<div id='FactsSection'>");
			reportSection.WriteLine($"<b>{_facts.Count} fact{arg}</b> {arg2} extracted during the analysis.");
			HTMLTable hTMLTable = new HTMLTable("FactsTable");
			hTMLTable.ToggleRowBackgrounds = true;
			hTMLTable.AddColumns("ID", "TypeID", "ParentID", "Name", "Data[0]", "Data[1]", "Data[2]");
			foreach (object fact2 in _facts)
			{
				Fact fact = fact2 as Fact;
				if (fact2 != null)
				{
					hTMLTable.AddRow(fact.FactID, fact.FactTypeID, fact.ParentFactID, fact.Name, fact.Data.GetSafely(0), fact.Data.GetSafely(1), fact.Data.GetSafely(2));
				}
				else
				{
					hTMLTable.AddRow(fact.ToString());
				}
			}
			reportSection.WriteLine(hTMLTable.ToString());
			reportSection.WriteLine("</div>");
			string getUID = reportSection.GetUID;
			ReportOther($"<b>{_facts.Count} fact{arg}</b> {arg2} extracted during the analysis.", "The details are available in the <a href='#" + getUID + "'><u>Facts Details</u></a> at the bottom of the report.", "Notification", "notificationicon.png", 0, "{C3351925-D758-42CE-8729-619281075286}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred while reporting the facts.\r\n\tMessage:  {ex.Message}\r\nStack Trace:\r\n{ex.StackTrace}");
		}
	}

	/// <summary>
	/// Method to write data on the report after formatting via String.Format.
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void Write(string format, params object[] args)
	{
		CurrentSection.Write(format, args);
	}

	/// <summary>
	/// Method to write data on the report
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="bold">Boolean value indicating if the text should be formated as bold</param>
	/// <param name="color">String indicating the color to apply on the text</param>
	/// <param name="size">Integer indicating the size of the text</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void Write(string format, bool bold, string color, int size = 0, params object[] args)
	{
		CurrentSection.Write(format, bold, color, size, args);
	}

	/// <summary>
	/// Method to write data on the report
	/// </summary>
	/// <param name="output">String value to add on the report</param>
	/// <param name="bold">Boolean value indicating if the text should be formated as bold</param>
	/// <param name="color">String indicating the color to apply on the text</param>
	/// <param name="size">Integer indicating the size of the text</param>
	public void Write(string output, bool bold, string color, int size = 0)
	{
		CurrentSection.Write(output, bold, color, size);
	}

	private void Analisis_PreExecuteRule(object sender, RuleExecutionEventArgs args)
	{
		if (string.IsNullOrEmpty(args.AnalysisRuleName))
		{
			_currentSection = _details;
			return;
		}
		ReportSection innerSection = _details.GetInnerSection(args.AnalysisRuleName, SectionType.Rule);
		if (!string.IsNullOrEmpty(args.DumpName))
		{
			innerSection = innerSection.GetInnerSection(args.DumpName, SectionType.Dump);
			if (!string.IsNullOrEmpty(args.ThreadID))
			{
				innerSection = innerSection.GetInnerSection("ThreadSummary", SectionType.ThreadSummary);
				innerSection = innerSection.GetInnerSection("Thread " + args.ThreadID, SectionType.Thread);
			}
		}
		_currentDumpName = args.DumpName;
		_currentSection = innerSection;
	}

	private void Analisis_PostExecuteRule(object sender, RuleExecutionEventArgs args)
	{
		_currentSection = _details;
	}

	/// <summary>
	/// Function to create a inner Report Section on the report
	/// </summary>
	/// <param name="SectionID">Unique name for the new section, whenever a sectionID that already exists is used, the existing
	/// report section will be returned</param>
	/// <param name="ReportSectionType">Type of Section to be added, use custom type as the default section</param>
	/// <returns>New created inner section or existing section that matches with the ID</returns>
	public ReportSection AddReportSection(string SectionID, SectionType ReportSectionType)
	{
		return CurrentSection.AddChildSection(SectionID, ReportSectionType);
	}

	/// <summary>
	/// This method is used to report detailed information to the user about the analysis.   
	/// </summary>
	/// <param name="Output">Content to write on the report</param>
	public void Write(string Output)
	{
		CurrentSection.Write(Output);
	}

	private void SolutionSourceIdIsMissing()
	{
	}

	/// <summary>
	/// This method is used to report general information to DebugDiag.  
	/// </summary>
	/// <param name="Information">The data passed to this method will appear in the analysis report as information.</param>
	/// <overloads>There are two overloads for this method</overloads>
	public void ReportInformation(string Information)
	{
		ReportInformation(Information, 0);
	}

	/// <summary>
	/// This method is used to report general information to DebugDiag. The parameter Weight helps to order the summary based on the most relevant results.
	/// </summary>
	/// <param name="Information">The data passed to this method will appear in the analysis report as information.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	public void ReportInformation(string Information, int Weight)
	{
		SolutionSourceIdIsMissing();
		ReportInformation(Information, 0, Guid.Empty.ToString());
	}

	/// <summary>
	/// This method is used to report general information to DebugDiag. The parameter Weight helps to order the summary based on the most relevant results.
	/// </summary>
	/// <param name="Information">The data passed to this method will appear in the analysis report as information.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	/// <param name="SolutionSourceID">reserved</param>
	public void ReportInformation(string Information, int Weight, string SolutionSourceID)
	{
		if (Information == null)
		{
			throw new ArgumentNullException("Information");
		}
		string item = string.Format("{0}|{1}|{2}|{3}|{4}", "Information", Information, "", SolutionSourceID, _currentDumpName);
		if (!_resultsSet.Contains(item))
		{
			_resultsSet.Add(item);
			_results.Add(new NetResult(_currentAnalysisRule, "Information", Information, "", Weight, SolutionSourceID, _currentDumpName));
		}
	}

	/// <summary>
	/// This method is used to report a warning to DebugDiag. The data passed to this method will appear in the analysis report as a warning.
	/// </summary>
	/// <param name="Warning">The first string passed to this method is the warning to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the warning.</param>
	/// <overloads>This method has two overloads</overloads>
	public void ReportWarning(string Warning, string Recommendation)
	{
		ReportWarning(Warning, Recommendation, 0);
	}

	/// <summary>
	/// This method is used to report a warning to DebugDiag. The data passed to this method will appear in the analysis report as a warning.
	/// </summary>
	/// <param name="Warning">The first string passed to this method is the warning to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the warning.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	public void ReportWarning(string Warning, string Recommendation, int Weight)
	{
		SolutionSourceIdIsMissing();
		ReportWarning(Warning, Recommendation, 0, Guid.Empty.ToString());
	}

	/// <summary>
	/// This method is used to report a warning to DebugDiag. The data passed to this method will appear in the analysis report as a warning.
	/// </summary>
	/// <param name="Warning">The first string passed to this method is the warning to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the warning.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	/// <param name="SolutionSourceID">reserved</param>
	public void ReportWarning(string Warning, string Recommendation, int Weight, string SolutionSourceID)
	{
		if (Warning == null)
		{
			throw new ArgumentNullException("Warning");
		}
		if (Recommendation == null)
		{
			throw new ArgumentNullException("Recommendation");
		}
		string item = string.Format("{0}|{1}|{2}|{3}|{4}", "Warning", Warning, Recommendation, SolutionSourceID, _currentDumpName);
		if (!_resultsSet.Contains(item))
		{
			_resultsSet.Add(item);
			_results.Add(new NetResult(_currentAnalysisRule, "Warning", Warning, Recommendation, Weight, SolutionSourceID, _currentDumpName));
		}
	}

	/// <summary>
	/// This method is used to report an error to DebugDiag. The data passed to this method will appear in the analysis report as an error.
	/// </summary> 
	/// <param name="Error">The first string passed to this method is the error to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the error.</param>
	/// <overloads>This merthod has two overloads.</overloads>
	public void ReportError(string Error, string Recommendation)
	{
		ReportError(Error, Recommendation, 0);
	}

	/// <summary>
	/// This method is used to report an error to DebugDiag. The data passed to this method will appear in the analysis report as an error.
	/// </summary> 
	/// <param name="Error">The first string passed to this method is the error to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the error.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	public void ReportError(string Error, string Recommendation, int Weight)
	{
		SolutionSourceIdIsMissing();
		ReportError(Error, Recommendation, 0, Guid.Empty.ToString());
	}

	/// <summary>
	/// This method is used to report an error to DebugDiag. The data passed to this method will appear in the analysis report as an error.
	/// </summary> 
	/// <param name="Error">The first string passed to this method is the error to be reported.</param>
	/// <param name="Recommendation">The second string is the recommendation suggested to resolve the error.</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	/// <param name="SolutionSourceID">reserved</param>
	public void ReportError(string Error, string Recommendation, int Weight, string SolutionSourceID)
	{
		if (SolutionSourceID == Guid.Empty.ToString())
		{
			SolutionSourceIdIsMissing();
		}
		if (Error == null)
		{
			throw new ArgumentNullException("Error");
		}
		if (Recommendation == null)
		{
			throw new ArgumentNullException("Recommendation");
		}
		if (!SolutionSourceID.StartsWith("{"))
		{
			SolutionSourceID = $"{{{SolutionSourceID}}}";
		}
		string item = string.Format("{0}|{1}|{2}|{3}|{4}", "Error", Error, Recommendation, SolutionSourceID, _currentDumpName);
		if (!_resultsSet.Contains(item))
		{
			_resultsSet.Add(item);
			_results.Add(new NetResult(_currentAnalysisRule, "Error", Error, Recommendation, Weight, SolutionSourceID, _currentDumpName));
		}
	}

	/// <summary>
	/// This method is used by unattended processors to add arbitrary data from the dump into a fact model.  This method is not intended for use from within your code.
	/// </summary>
	/// <param name="fact">The fact to be stored.  Must be [Serializable].  Any references between objects must also be serializable (i.e. using object IDs).</param>
	public void AddFact(object fact)
	{
		if (!IsSerializeable(fact))
		{
			throw new Exception("Facts Must be [Serializable].  Any references between objects must also be serializable (i.e. using object IDs).");
		}
		_facts.Add(fact);
	}

	private static bool IsSerializeable(object fact)
	{
		bool value = false;
		if (fact is Fact)
		{
			return true;
		}
		Type type = fact.GetType();
		if (!_isSerializeableCache.TryGetValue(type, out value))
		{
			value = fact.GetType().GetCustomAttributes(typeof(SerializableAttribute), inherit: true).Any();
			_isSerializeableCache.TryAdd(type, value);
		}
		return value;
	}

	/// <summary>
	/// This method is used by unattended processors to add arbitrary data from the dump into a fact model.  This method is not intended for use from within your code.
	/// </summary>
	/// <param name="FactData">The fact data to be stored.</param>
	/// <param name="factTypeID">The type of fact data to be stored.</param>
	/// <param name="ParentFactID">The ID of the parent fact.</param>
	public void AddFact(ref Fact fact)
	{
		if (string.IsNullOrEmpty(fact.FactTypeID))
		{
			throw new ArgumentNullException("FactTypeID must not be null or empty");
		}
		if (!string.IsNullOrEmpty(fact.FactID))
		{
			throw new ArgumentException("FactID must be null or empty.  It is set to a new ID during this method.");
		}
		Fact fact2 = fact;
		if (!string.IsNullOrEmpty(fact.ParentFactID) && _facts.Where((object x) => ((Fact)x).FactID == fact2.ParentFactID).Count() == 0)
		{
			throw new ArgumentException("The specified ParentFactID does not match the FactID of any existing fact in the list.  The parent must exist before adding the child.");
		}
		fact.FactID = Guid.NewGuid().ToString();
		_facts.Add(fact);
	}

	internal void SetX86Results(NetResults results)
	{
		_x86Results = results;
	}

	internal void SetX86Facts(List<object> facts)
	{
		_x86facts = facts;
	}

	/// <summary>
	/// Returns the Collection of IResults based on the index specified
	/// </summary>
	/// <param name="index">indicate which set of IResults to return</param>
	/// <returns>Reference object that represents the COM collection of IResults</returns>
	public IResults GetResults(int index)
	{
		if (index == 0)
		{
			return _results;
		}
		return _x86Results;
	}

	/// <summary>
	/// Returns the Collection of Facts based on the index specified.  This method is not intended for use from within your code.
	/// </summary>
	/// <param name="index">indicate which set of IResults to return</param>
	/// <returns>Reference object that represents the COM collection of IResults</returns>
	public List<object> GetFacts(int index)
	{
		if (index == 0)
		{
			return _facts;
		}
		return _x86facts;
	}

	/// <summary>
	/// Adds a comment to the report
	/// </summary>
	/// <param name="Description">String containing what you want to report</param>
	/// <param name="Recommendation">String that will appear on the recommendation column of the report</param>
	/// <param name="TypeLabel">Name for the custom type of information reported</param>
	/// <param name="IconFileName">Icon file name to attach on the report</param>
	/// <param name="Weight">An integer value representing the relevance of the result.</param>
	/// <param name="SolutionSourceID">reserved</param>
	public void ReportOther(string Description, string Recommendation, string TypeLabel, string IconFileName, int Weight = 0, string SolutionSourceID = "")
	{
		string item = string.Format("{0}|{1}|{2}|{3}|{4}", "Other", Description, Recommendation, SolutionSourceID, _currentDumpName);
		if (!_resultsSet.Contains(item))
		{
			_resultsSet.Add(item);
			_results.Add(new NetResult(_currentAnalysisRule, TypeLabel, Description, Recommendation, Weight, SolutionSourceID, _currentDumpName, IconFileName));
		}
	}

	/// <summary>
	/// Writes the Report File when you are creating a custom report not using Rules Interfaces.
	/// </summary>
	/// <exception cref="!:System.Excpetion">This method may throw an exption if the report file name is empty.</exception>
	/// <remarks>You should not call this method when developing a custom Rule that will run inside the DebugDiag.Analysis.exe analyzer, doing so will cause the report
	/// to have layout issues</remarks>
	private void WriteReportFile()
	{
		ReportClrmdInitExceptions();
		ReportMultipleClrRuntimes();
		if (reportFacts)
		{
			ReportFacts();
		}
		if (string.IsNullOrEmpty(_reportFileFullPath))
		{
			string reportFileDirectoryOrFullPath = Directory.GetCurrentDirectory() + "\\Reports";
			_reportFileFullPath = _analyzer.BuildReportFileFullPath(reportFileDirectoryOrFullPath);
			if (string.IsNullOrEmpty(_reportFileFullPath))
			{
				throw new Exception("The report file name cannot be null");
			}
		}
		CreateRealReport(FormatReport());
	}

	/// <summary>
	/// This function appends the code sent as parameter allowing anlaysis rule developers to add custom Javascript functions to the report
	/// </summary>
	/// <param name="JSCode">string containing the Javascript function code</param>
	public void RegisterJSFunction(string JSCode)
	{
		jsManager.AppendJSFunction(JSCode);
	}

	/// <summary>
	/// Since the report includes Jquery 1.11.0 handlers for events on dynamic html object can be added on the $(document).ready(function () {
	/// through this method
	/// </summary>
	/// <param name="handlerJSCode">string containing the Javascript handler</param>
	public void RegisterJSDocReadyHandler(string handlerJSCode)
	{
		jsManager.AppendCallOnDocumentReady(handlerJSCode);
	}

	internal void ReplaceAnchorReference(string CurrentHrefValue, string NewHrefValue)
	{
		jsManager.ReplaceAnchorReference(CurrentHrefValue, NewHrefValue);
	}
}
