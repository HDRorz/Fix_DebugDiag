#define TRACE
using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DebugDiag.DbgLib;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.Mex;
using DebugDiag.DotNet.Properties;
using DebugDiag.DotNet.Util;
using DebugDiag.DotNet.x86Analysis;
using Microsoft.Win32;

namespace DebugDiag.DotNet;

/// <summary>
/// The NetAnalyzer object is used to determine available analysis scripts, add data files, and start an analysis.  
/// This object is used internally by the DebugDiag Analysis user interface to manage analysis rules.  
/// End users can use this object to develop their own rules, batch files, or GUI's to manage data analysis.
/// </summary>
/// <remarks>
/// <example>
/// <code language="cs">
/// using (NetAnalyzer analyzer = new NetAnalyzer())
/// {
///     //In this example I'm referencing a dll module that has the prebuild rules that ship with debugdiag 
///     analyzer.AddAnalysisRulesToRunList(@"C:\Program Files\DebugDiag\AnalysisRules\DebugDiag.AnalysisRules.dll", false);
///
///     List&lt;AnalysisRuleInfo&gt; analysisRules = analyzer.AnalysisRuleInfos;
///
///     Console.WriteLine("The available rules on the analyzer are: \n\r\n\r");
///
///     foreach(AnalysisRuleInfo ruleInfo in analysisRules)
///     {
///          Console.WriteLine(ruleInfo.DisplayName);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public class NetAnalyzer : IDisposable
{
	public delegate void AssembyProbeMissed(string assemblyName, string path);

	public delegate void AssembyLoadFailed(string assemblyName, Exception ex);

	internal delegate void ShadowCopyDelgate(string srcFile, string dstFile);

	private class AnalysisExecutionQueueItem
	{
		public string[] RuleNames;

		public string[] ErrorMessages;

		public string[] DumpFiles;

		public AnalysisExecutionQueueItem(string[] ruleNames, string[] errorMessages, string[] dumpFiles)
		{
			RuleNames = ruleNames;
			ErrorMessages = errorMessages;
			DumpFiles = dumpFiles;
		}
	}

	/// <summary>
	/// Field that holds a list of <c>AnalysisRuleInfo</c> objects
	/// </summary>
	public List<AnalysisRuleInfo> AnalysisRuleInfos = new List<AnalysisRuleInfo>();

	/// <summary>
	/// Field where the symbols path information is stored
	/// </summary>
	public string SymbolPath;

	/// <summary>
	/// Field where the compiled modules are located in case a minidump is used in order to help resolving symbols
	/// </summary>
	public string ImagePath = "";

	/// <summary>
	/// Field that holds an instance of the <c>NetProgress</c> object used by the NetAnalyzer 
	/// </summary>
	public NetProgress Progress;

	internal bool DataFilesIncludesCrashDumps;

	private Process _x86AnalysisHostProcess;

	private const string X86_SUPPORT = "x86Support";

	private const string X86_ANALYSIS_HOST_PROCESS_NAME = "DebugDiag.x86AnalysisHost.exe";

	private static string[] _possibleAnalysisRuleInterfacesList;

	private static HashSet<string> AssemblyResolvePaths;

	private IAnalyzer _legacyAnalyzer;

	private DbgControl_Legacy _legacyControl;

	private NetScriptManager _manager;

	private bool _disposed;

	private bool _analysisRulesComplete;

	private DumpFileType _dumpTypeBeingAnalyzedCurrently;

	private Dictionary<DumpFileType, List<string>> _dumpFilesByType;

	private string[] _reportFileNames = new string[2];

	private StringBuilder _x86AnalysisHostErrors = new StringBuilder();

	private static string _shadowCopyDir;

	private IAnalysisService _analysisService;

	private ManualResetEvent _proxyEvent = new ManualResetEvent(initialState: false);

	private static bool _ddv2InstallDirSearchAttempted;

	private static string _ddv2InstallDir;

	private static ManualResetEvent _staticConstructorComplete;

	private const int ANALYSIS_TIMEOUT = 7200000;

	private HashSet<string> dependencies = new HashSet<string> { "DEBUGDIAG.DOTNET.DLL", "CLRMEMDIAGEXT.DLL", "MICROSOFT.DIAGNOSTICS.RUNTIME.DLL", "MEXFRAMEWORK.DLL", "MEXFRAMEWORKINTERNAL.DLL" };

	private HashSet<string> scannedPaths = new HashSet<string>();

	public static AssembyProbeMissed OnAssembyProbeMiss;

	public static AssembyLoadFailed OnAssembyLoadFailed;

	internal static ShadowCopyDelgate OnShadowCopy;

	private static TypeFilter _analysisRuleInterfaceFilter;

	private bool _ruleNotCompletedMessageWritten;

	internal static string ShadowCopyDir
	{
		get
		{
			if (string.IsNullOrEmpty(_shadowCopyDir))
			{
				_shadowCopyDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "DebugDiagShadowCopy", Guid.NewGuid().ToString());
				Directory.CreateDirectory(_shadowCopyDir);
				ThreadPool.QueueUserWorkItem(CleanupShadowCopyDirs);
			}
			return _shadowCopyDir;
		}
		set
		{
			_shadowCopyDir = value;
		}
	}

	/// <summary>
	/// This property returns a string array containing the Report File names generated by the analysis rules executed.
	/// </summary>
	public string[] ReportFileNames => _reportFileNames;

	private static string InstallDir
	{
		get
		{
			if (!_ddv2InstallDirSearchAttempted)
			{
				_ddv2InstallDirSearchAttempted = true;
				string text = "HKEY_CLASSES_ROOT\\DbgLib.DbgControl\\CLSID";
				string defaultRegVal = GetDefaultRegVal(text);
				try
				{
					if (defaultRegVal != string.Empty && defaultRegVal != string.Empty)
					{
						text = $"HKEY_CLASSES_ROOT\\CLSID\\{defaultRegVal}\\InprocServer32";
						defaultRegVal = GetDefaultRegVal(text);
						if (!string.IsNullOrEmpty(defaultRegVal))
						{
							_ddv2InstallDir = Path.GetDirectoryName(defaultRegVal);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"An exception occurred while finding the v2 analysis runtime.\r\n\tKeyPath:  {text}\r\n\tValueName:  {defaultRegVal}\r\n\tMessage:  {ex.Message}\r\nStack Trace:\r\n{ex.StackTrace}");
				}
			}
			return _ddv2InstallDir;
		}
	}

	/// <summary>
	/// This property returns True if the report has been completely generated, otherwise it returns false.
	/// </summary>
	public bool ReportReady
	{
		get
		{
			bool flag = true;
			if (AnalysisRuleInfos.Count > 0)
			{
				flag = _analysisRulesComplete;
			}
			return (byte)(1u & (flag ? 1u : 0u)) != 0;
		}
	}

	/// <summary>
	/// Returns an instance of the <see cref="T:DebugDiag.DotNet.NetScriptManager" /> object.
	/// </summary>
	public NetScriptManager Manager => _manager;

	/// <summary>
	/// On 64 bits analysis is performed on a different process called DebugDiag.x86AnalysisHost.exe, this property allows the developer
	/// to get a list of errors if any, when executing rules against 32 bit dump processes.
	/// </summary>
	public string x86AnalysisHostErrors => _x86AnalysisHostErrors.ToString();

	/// <summary>
	/// Occurs when the RunAnalysisRules method is executed and a rule is executed
	/// </summary>
	public event EventHandler<RuleFilterEventArgs> OnRuleFilter;

	/// <summary>
	/// Occurs prior to call the RunAnalysis method on the implemented Rule
	/// </summary>
	public event EventHandler<RuleExecutionEventArgs> PreExecuteRule;

	/// <summary>
	/// Occurs after the RunAnalysis method has been executed
	/// </summary>
	public event EventHandler<RuleExecutionEventArgs> PostExecuteRule;

	static NetAnalyzer()
	{
		_possibleAnalysisRuleInterfacesList = new string[4] { "DebugDiag.DotNet.AnalysisRules.IExceptionThreadRule", "DebugDiag.DotNet.AnalysisRules.IHangDumpRule", "DebugDiag.DotNet.AnalysisRules.IHangThreadRule", "DebugDiag.DotNet.AnalysisRules.IMultiDumpRule" };
		AssemblyResolvePaths = new HashSet<string>();
		_shadowCopyDir = null;
		_staticConstructorComplete = new ManualResetEvent(initialState: false);
		_analysisRuleInterfaceFilter = AnalysisRuleInterfaceFilter;
		bool isUdePluginExe = false;
		try
		{
			MethodInfo originalHandler = null;
			if (!isUdePluginExe)
			{
				Type typeFromHandle = typeof(AppDomain);
				FieldInfo field = typeFromHandle.GetField("_AssemblyResolve", BindingFlags.Instance | BindingFlags.NonPublic);
				EventInfo @event = typeFromHandle.GetEvent("AssemblyResolve");
				AppDomain currentDomain = AppDomain.CurrentDomain;
				Delegate @delegate = field.GetValue(currentDomain) as Delegate;
				Console.WriteLine("Checking for existing AssemblyResolve handler");
				if ((object)@delegate != null)
				{
					originalHandler = @delegate.Method;
					@event.RemoveEventHandler(currentDomain, @delegate);
					Console.WriteLine("Removed existing AssemblyResolve handler");
				}
			}
			AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs a)
			{
				Console.WriteLine("Looking for {0} in static static NetAnalyzer", a.Name);
				if (!isUdePluginExe)
				{
					string text = a.Name.Split(',')[0];
					if (text == "Microsoft.Diagnostics.Runtime")
					{
						string text2 = Path.Combine(Directory.GetCurrentDirectory(), text + ".dll");
						Console.WriteLine("Looking for {0} at the follwing path:\r\n\t{1}", text, text2);
						if (File.Exists(text2))
						{
							return Assembly.Load(text);
						}
					}
				}
				if (originalHandler != null)
				{
					Console.WriteLine("Invoking previous AssemblyResolve handler");
					return (Assembly)originalHandler.Invoke(null, new object[2] { s, a });
				}
				Console.WriteLine("No previous AssemblyResolve handler");
				return (Assembly)null;
			};
		}
		finally
		{
			_staticConstructorComplete.Set();
		}
	}

	private static void SetLoggingCallbacks()
	{
	}

	private static void CopyRootDirFiles()
	{
		try
		{
			string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "rootdir");
			if (Directory.Exists(text))
			{
				string directoryName = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				XCopy.DirectoryCopy(text, directoryName, copySubDirs: true);
			}
		}
		catch (Exception)
		{
		}
	}

	/// <summary>
	/// Default Constructor of the <c>NetAnalyzer</c> object, it initializes an instance of the <c>NetScriptManager</c> Object.
	/// </summary>
	public NetAnalyzer()
	{
		_staticConstructorComplete.WaitOne();
		_legacyControl = NetDbgObj.CreateNewLegacyControl();
		_legacyAnalyzer = _legacyControl.Analyzer;
		_manager = new NetScriptManager(_legacyAnalyzer.Manager, this);
		_dumpFilesByType = new Dictionary<DumpFileType, List<string>>();
		_dumpFilesByType[DumpFileType._32bitAll] = new List<string>();
		_dumpFilesByType[DumpFileType._64bitAll] = new List<string>();
		AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
	}

	private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
	{
		Console.WriteLine("Looking for {0} in NetAnalyzer.AssemblyResolve", args.Name);
		string text = args.Name.Split(',')[0];
		string requestingAssemblyFileName = null;
		if (args.RequestingAssembly != null)
		{
			requestingAssemblyFileName = Path.GetFileName(args.RequestingAssembly.CodeBase);
		}
		foreach (string assemblyResolvePath in AssemblyResolvePaths)
		{
			Console.WriteLine("Looking for {0} at the follwing path:\r\n\t{1}", text, assemblyResolvePath);
			Assembly assembly = LoadAssemblyByRequestingAssembly(text, requestingAssemblyFileName, assemblyResolvePath);
			if (assembly != null)
			{
				return assembly;
			}
			if (OnAssembyProbeMiss != null)
			{
				OnAssembyProbeMiss(text, assemblyResolvePath);
			}
		}
		return null;
	}

	private static Assembly LoadAssemblyByRequestingAssembly(string assemblyFileName, string requestingAssemblyFileName, string path)
	{
		if ((requestingAssemblyFileName == null || File.Exists(Path.Combine(path, requestingAssemblyFileName))) && File.Exists(Path.Combine(path, assemblyFileName + ".dll")))
		{
			return Assembly.LoadFrom(ShadowCopy(Path.Combine(path, assemblyFileName + ".dll")));
		}
		return null;
	}

	/// <summary>
	/// Release all the native resources used by the NetAnalyzer object.
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			if (_legacyAnalyzer != null)
			{
				Marshal.FinalReleaseComObject(_legacyAnalyzer);
			}
			if (_legacyControl != null)
			{
				Marshal.FinalReleaseComObject(_legacyControl);
			}
			if (_manager != null)
			{
				_manager.Dispose();
			}
		}
		_legacyAnalyzer = null;
		_legacyControl = null;
		_manager = null;
		_disposed = true;
		GC.SuppressFinalize(this);
	}

	~NetAnalyzer()
	{
		Dispose(disposing: false);
	}

	/// <summary>
	/// Add all possible analysis rules found in the type provided.  Analysis rules must be public classes which implement 
	/// one or more of the interfaces deriving from DebugDiag.DotNet.AnalysisRules.IAnalysisRule.  
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <typeparam name="RuleInstanceType">Type of the rule to be added</typeparam>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	public bool AddAnalysisRuleToRunList<RuleInstanceType>()
	{
		Type typeFromHandle = typeof(RuleInstanceType);
		return AddAnalysisRuleToRunList(typeFromHandle, typeFromHandle.Assembly.Location, throwIfNoAnalysisRulesFound: true);
	}

	private bool AddAnalysisRuleToRunList(Type ruleInstanceType, string originalPath, bool throwIfNoAnalysisRulesFound)
	{
		if (IsTypeAnAnalysisRule(ruleInstanceType))
		{
			AnalysisRuleInfos.Add(new CodeAnalysisRuleInfo(ruleInstanceType, originalPath));
			return true;
		}
		if (IsTypeAMexAnalysisRule(ruleInstanceType))
		{
			AnalysisRuleInfos.Add(new MexCodeAnalysisRuleInfo(ruleInstanceType, originalPath));
			return true;
		}
		if (throwIfNoAnalysisRulesFound)
		{
			throw new NoValidAnalysisRulesFoundException($"No valid Analysis Rule interfaces found on the following type:  {ruleInstanceType.FullName}");
		}
		return false;
	}

	/// <summary>
	/// Add all possible analysis rules found in the type provided.  Analysis rules must be public classes which implement 
	/// one or more of the interfaces deriving from DebugDiag.DotNet.AnalysisRules.IAnalysisRule.  
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <param name="ruleInstanceType">System.Type for the rule to be added</param>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	public bool AddAnalysisRuleToRunList(Type ruleInstanceType)
	{
		return AddAnalysisRuleToRunList(ruleInstanceType, ruleInstanceType.Assembly.Location, throwIfNoAnalysisRulesFound: true);
	}

	/// <summary>
	///
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <param name="pathOrType"></param>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	public bool AddAnalysisRuleToRunList(string pathOrType)
	{
		if (dependencies.Contains(Path.GetFileName(pathOrType).ToUpper()))
		{
			return false;
		}
		if (pathOrType.EndsWith(".filter.xaml", StringComparison.CurrentCultureIgnoreCase))
		{
			return false;
		}
		string originalPath = pathOrType;
		pathOrType = ShadowCopy(pathOrType);
		if (File.Exists(pathOrType))
		{
			FileInfo fileInfo = new FileInfo(pathOrType);
			if (fileInfo.Extension.ToUpper() == ".XAML")
			{
				AnalysisRuleInfos.Add(new XamlAnalysisRuleInfo(pathOrType));
				return true;
			}
			if (fileInfo.Extension.ToUpper() == ".DLL")
			{
				Assembly assembly = null;
				try
				{
					if (assembly == null)
					{
						assembly = Assembly.LoadFrom(pathOrType);
					}
					if (AddAnalysisRulesToRunList(assembly, originalPath, throwIfNoAnalysisRulesFound: false))
					{
						string item = Path.GetDirectoryName(pathOrType).ToUpper();
						if (!AssemblyResolvePaths.Contains(item))
						{
							AssemblyResolvePaths.Add(item);
						}
						return true;
					}
				}
				catch (BadImageFormatException)
				{
				}
				catch (FileNotFoundException)
				{
				}
			}
		}
		else
		{
			Type type = Type.GetType(pathOrType, throwOnError: false);
			if (type != null)
			{
				return AddAnalysisRuleToRunList(type);
			}
		}
		return false;
	}

	private bool IsSpecialRulesSubDir(string subDirectory)
	{
		switch (new DirectoryInfo(subDirectory).Name.ToUpper())
		{
		case "EXTS":
		case "BIN":
		case "ROOTDIR":
		case "X86SUPPORT":
			return true;
		default:
			return false;
		}
	}

	internal static string ShadowCopy(string srcFile)
	{
		if (!File.Exists(srcFile))
		{
			return srcFile;
		}
		if (Path.GetDirectoryName(srcFile).ToUpper().StartsWith(ShadowCopyDir.ToUpper()))
		{
			return srcFile;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(srcFile);
			if (directoryName.Equals(ShadowCopyDir, StringComparison.CurrentCultureIgnoreCase))
			{
				return srcFile;
			}
			if (srcFile.EndsWith(".xaml", StringComparison.CurrentCultureIgnoreCase))
			{
				return srcFile;
			}
			string text = null;
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(srcFile);
			string extension = Path.GetExtension(srcFile);
			text = Path.Combine(ShadowCopyDir, $"{fileNameWithoutExtension}{extension}");
			int num = 0;
			while (File.Exists(text))
			{
				num++;
				text = Path.Combine(ShadowCopyDir, $"{fileNameWithoutExtension} ({num}){extension}");
			}
			FileCopy(srcFile, text);
			string text2 = Path.Combine(directoryName, fileNameWithoutExtension) + ".pdb";
			if (File.Exists(text2))
			{
				try
				{
					string text3 = Path.Combine(ShadowCopyDir, fileNameWithoutExtension + ".pdb");
					if (!File.Exists(text3))
					{
						FileCopy(text2, text3);
					}
				}
				catch (Exception)
				{
				}
			}
			string path = Path.GetFileName(srcFile) + ".config";
			string text4 = Path.Combine(directoryName, path);
			if (File.Exists(text4))
			{
				try
				{
					string text5 = Path.Combine(ShadowCopyDir, path);
					if (!File.Exists(text5))
					{
						FileCopy(text4, text5);
					}
				}
				catch (Exception)
				{
				}
			}
			return text;
		}
		catch (Exception)
		{
			return srcFile;
		}
	}

	private static void FileCopy(string srcFile, string dstFile)
	{
		Console.WriteLine("FileCopy\r\n\tFrom:\t'{0}'\r\n\tTo:\t{1}", srcFile, dstFile);
		File.Copy(srcFile, dstFile);
	}

	private static void CleanupShadowCopyDirs(object state)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(ShadowCopyDir);
		if (directoryInfo == null || directoryInfo.Parent == null)
		{
			return;
		}
		foreach (DirectoryInfo item in directoryInfo.Parent.EnumerateDirectories())
		{
			if (!(DateTime.Now.Subtract(item.CreationTime) > new TimeSpan(2, 0, 0)))
			{
				continue;
			}
			foreach (FileInfo item2 in item.EnumerateFiles())
			{
				item2.Attributes &= ~FileAttributes.ReadOnly;
			}
			try
			{
				item.Delete(recursive: true);
			}
			catch (Exception)
			{
			}
		}
	}

	/// <summary>
	/// Add all possible analysis rules found at the path provided.  Analysis rules must be public classes which implement 
	/// one or more of the interfaces deriving from DebugDiag.DotNet.AnalysisRules.IAnalysisRule.  
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <param name="path">Can point to single assembly, or a to a directory containing multiple assemblies.</param>
	/// <param name="recurse">Wheter to recursively look for assemblies in subdirectories (if 'path' points to a directory)</param>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	public bool AddAnalysisRulesToRunList(string path, bool recurse = false)
	{
		return AddAnalysisRulesToRunListInternal(path, recurse);
	}

	private bool AddAnalysisRulesToRunListInternal(string path, bool recurse = false)
	{
		bool result = false;
		string text = null;
		bool flag = false;
		if (Directory.Exists(path))
		{
			flag = true;
			text = path;
		}
		else if (File.Exists(path))
		{
			flag = false;
			text = Path.GetDirectoryName(path);
		}
		if (text == null)
		{
			return false;
		}
		ShadowCopySpecialSubdirs(text);
		if (flag)
		{
			if (recurse)
			{
				foreach (string item in Directory.EnumerateDirectories(path))
				{
					if (!IsSpecialRulesSubDir(item) && AddAnalysisRulesToRunListInternal(item, recurse: true))
					{
						result = true;
					}
				}
			}
			foreach (string item2 in Directory.EnumerateFiles(path, "*.dll"))
			{
				if (AddAnalysisRulesToRunListInternal(item2, recurse: true))
				{
					result = true;
				}
			}
			foreach (string item3 in Directory.EnumerateFiles(path, "*.xaml"))
			{
				if (AddAnalysisRulesToRunListInternal(item3, recurse: true))
				{
					result = true;
				}
			}
		}
		else if (AddAnalysisRuleToRunList(path))
		{
			result = true;
		}
		return result;
	}

	private void ShadowCopySpecialSubdirs(string ruleDir)
	{
		ruleDir = ruleDir.ToUpper();
		if (scannedPaths.Contains(ruleDir) || !Directory.Exists(ruleDir))
		{
			return;
		}
		scannedPaths.Add(ruleDir);
		string[] array = new string[4] { "BIN", "EXTS", "ROOTDIR", "X86SUPPORT" };
		foreach (string text in array)
		{
			string text2 = Path.Combine(ruleDir, text);
			if (!Directory.Exists(text2))
			{
				continue;
			}
			try
			{
				string text3 = null;
				text3 = ((!(text == "ROOTDIR")) ? Path.Combine(ShadowCopyDir, text) : Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
				if (text == "BIN" || text == "X86SUPPORT")
				{
					text3 = text3.ToUpper();
					if (!AssemblyResolvePaths.Contains(text3))
					{
						AssemblyResolvePaths.Add(text3);
					}
				}
				if (OnShadowCopy != null)
				{
					OnShadowCopy(text2, text3);
				}
				XCopy.DirectoryCopy(text2, text3, copySubDirs: true);
			}
			catch (Exception)
			{
			}
		}
	}

	/// <summary>
	/// Add all possible analysis rules found in the assembly provided.  Analysis rules must be public classes which implement 
	/// one of the interfaces deriving from DebugDiag.DotNet.AnalysisRules.IAnalysisRule
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <param name="assembly">Assembly containing the Rules to be added</param>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	public bool AddAnalysisRulesToRunList(Assembly assembly)
	{
		return AddAnalysisRulesToRunList(assembly, null, throwIfNoAnalysisRulesFound: true);
	}

	/// <summary>
	/// Add all possible analysis rules found in the assembly provided.  Analysis rules must be public classes which implement 
	/// one of the interfaces deriving from DebugDiag.DotNet.AnalysisRules.IAnalysisRule
	/// </summary>
	/// <exception cref="T:DebugDiag.DotNet.AnalysisRules.NoValidAnalysisRulesFoundException"></exception>
	/// <param name="assembly">Assembly that contains the rules</param>
	/// <param name="originalPath">Path were the assembly is located</param>
	/// <param name="throwIfNoAnalysisRulesFound">If true, a NotAValidAnalysisRuleException will be thrown if the type doesn't contain any
	/// valid rules (doesn't implement any of the interfaces derived from DebugDiag.DotNet.AnalysisRules.IAnalysisRule).  
	/// Note it's common to find plenty of 'bad' types when searching through an entire assembly, so this param is set to false when called
	/// internally.  But if an external client passes in a type through the public API, it should have at least 1 rule so we will use true
	/// when it's called via the public API.</param>
	/// <returns>True if any analysis rues were found.  otherwise false</returns>
	private bool AddAnalysisRulesToRunList(Assembly assembly, string originalPath, bool throwIfNoAnalysisRulesFound)
	{
		bool atLeastOneValidRuleInAssembly = false;
		Parallel.ForEach(assembly.GetTypes(), delegate(Type ruleInstanceType)
		{
			if (AddAnalysisRuleToRunList(ruleInstanceType, originalPath, throwIfNoAnalysisRulesFound: false))
			{
				atLeastOneValidRuleInAssembly = true;
			}
		});
		if (throwIfNoAnalysisRulesFound && !atLeastOneValidRuleInAssembly)
		{
			throw new NoValidAnalysisRulesFoundException($"No valid Analysis Rule interfaces found on any type in the following assembly:  {assembly.FullName}");
		}
		return atLeastOneValidRuleInAssembly;
	}

	private bool IsTypeAMexAnalysisRule(Type ruleInstanceType)
	{
		try
		{
			return IsTypeAMexAnalysisRule_(ruleInstanceType);
		}
		catch (Exception)
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool IsTypeAMexAnalysisRule_(Type ruleInstanceType)
	{
		if (ruleInstanceType.GetCustomAttributes(inherit: true).Any((object a) => a.GetType().Name == "UdeRuleAttribute"))
		{
			MexCodeAnalysisRuleInfo.InitRuleEngine(ruleInstanceType.Assembly);
			return true;
		}
		return false;
	}

	private bool IsTypeAnAnalysisRule(Type ruleInstanceType)
	{
		for (int i = 0; i < _possibleAnalysisRuleInterfacesList.Length; i++)
		{
			if (ruleInstanceType.FindInterfaces(_analysisRuleInterfaceFilter, _possibleAnalysisRuleInterfacesList[i]).Length != 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool AnalysisRuleInterfaceFilter(Type ruleInstanceTypeObj, object criteriaObj)
	{
		if (ruleInstanceTypeObj.ToString() == criteriaObj.ToString())
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// This method will run the scripts added with <see cref="M:DebugDiag.DotNet.NetAnalyzer.AddAnalysisRulesToRunList(System.String,System.Boolean)" /> on the dump files added to be analyzed.  
	/// A <see cref="T:DebugDiag.DotNet.NetProgress" /> object can be passed to display the progress of the analysis, as well as the symbol path used during the analysis, 
	/// and the path where the report will be generated for the analysis.  
	/// </summary>
	/// <param name="progress"><c>NetProgress</c> object where the progress will be updated</param>
	/// <param name="symbolPath">String representing the symbol path to be used by the debugger to resolve the symbols</param>
	/// <param name="imagePath">String rerpresenting the path where the current modules used by the application can be located for analyzing mini dumps.</param>
	/// <param name="reportFileDirectoryOrFullPath">If reportFileDirectoryOrFullPath includes a file name, the report will be written to that file.  The file will be overwitten
	/// <param name="analysisMode">Optional parameter to point if the rule is being ran interactively (by default) or automated.</param>
	/// if it already exists.  If reportFileDirectoryOrFullPath is a directory, the file name will be constructed automatically using a combination of factors
	/// such as the process name in the target dump(s) and the name(s) of the rule(s) being executed.</param>
	public void RunAnalysisRules(NetProgress progress, string symbolPath, string imagePath, string reportFileDirectoryOrFullPath, AnalysisModes analysisMode = AnalysisModes.Interactive)
	{
		RunAnalysisRules(progress, symbolPath, imagePath, reportFileDirectoryOrFullPath, twoTabs: false, analysisMode);
	}

	internal void RunAnalysisRules(NetProgress progress, string symbolPath, string imagePath, string reportFileDirectoryOrFullPath, bool twoTabs, AnalysisModes analysisMode = AnalysisModes.Interactive)
	{
		ShowSymbolsWarningIfNeedBe();
		try
		{
			if (progress == null)
			{
				progress = _manager.Progress;
			}
			progress.SetCurrentRange(0, 2);
			progress.CurrentStatus = "Loading dump file and symbols...";
			progress.CurrentPosition = 1;
			SymbolPath = symbolPath;
			ImagePath = imagePath;
			Progress = progress;
			if (!HaveSomeDumpFiles())
			{
				throw new Exception("No dump files to analyze.  Call AddDumpFile[s] before calling this function.");
			}
			if (AnalysisRuleInfos.Count == 0)
			{
				throw new Exception("No Analysis Rules to run.  Call one of the AddAnalysisRule[s]ToRunList functions before calling this function.");
			}
			bool flag = false;
			string text2;
			if (Environment.Is64BitProcess)
			{
				twoTabs = _dumpFilesByType[DumpFileType._32bitAll].Count > 0 && _dumpFilesByType[DumpFileType._64bitAll].Count > 0;
				string text = BuildReportFileFullPath(reportFileDirectoryOrFullPath, _dumpFilesByType[DumpFileType._32bitAll]);
				text2 = BuildReportFileFullPath(reportFileDirectoryOrFullPath, _dumpFilesByType[DumpFileType._64bitAll]);
				progress.CurrentStatus = "";
				progress.CurrentPosition = 0;
				if (text2 == text)
				{
					text2 = GetUniqueFileNameIfAlreadyExists(text2.Replace(".mht", "_x64.mht"));
					text = GetUniqueFileNameIfAlreadyExists(text.Replace(".mht", "_x86.mht"));
				}
				RunAnalysisRulesInternal(DumpFileType._32bitAll, progress, symbolPath, imagePath, text, twoTabs, analysisMode);
				flag = RunAnalysisRulesInternal(DumpFileType._64bitAll, progress, symbolPath, imagePath, text2, twoTabs, analysisMode);
			}
			else
			{
				text2 = BuildReportFileFullPath(reportFileDirectoryOrFullPath, _dumpFilesByType[DumpFileType._32bitAll]);
				progress.CurrentStatus = "";
				progress.CurrentPosition = 0;
				flag = RunAnalysisRulesInternal(DumpFileType._32bitAll, progress, symbolPath, imagePath, text2, twoTabs, analysisMode);
				if (_dumpFilesByType[DumpFileType._64bitAll].Count > 0)
				{
					string format = "DebugDiag Analysis cannot be performed against a 64-bit dump file from a 32-bit analysis machine.<BR><BR><B>{0}";
					string recommendation = "Please run the analysis on a 64-bit machine to get analysis details.";
					StringBuilder stringBuilder = new StringBuilder();
					int count = _dumpFilesByType[DumpFileType._64bitAll].Count;
					for (int i = 0; i < count; i++)
					{
						stringBuilder.Append(_dumpFilesByType[DumpFileType._64bitAll][i]);
						if (i < count)
						{
							stringBuilder.Append("<BR>");
						}
					}
					_manager.ReportOther(string.Format(format, stringBuilder), recommendation, "Notification", "debugdiag-analysis.png", 0, Guid.Empty.ToString());
					_manager.BitnessErrorReported = true;
					flag = true;
					if (text2 == null)
					{
						text2 = BuildReportFileFullPath(reportFileDirectoryOrFullPath, _dumpFilesByType[DumpFileType._64bitAll]);
					}
				}
			}
			if (flag)
			{
				_manager.WriteReportFile(text2);
				_reportFileNames[0] = text2;
			}
		}
		finally
		{
			_dumpTypeBeingAnalyzedCurrently = DumpFileType.None;
			_analysisRulesComplete = true;
		}
	}

	private void ShowSymbolsWarningIfNeedBe()
	{
		if (string.IsNullOrEmpty(SymbolPath))
		{
			_manager.ReportOther("<div class='summaryItemCallout'>The symbol path is empty.</div>  Analysis will be incomplete without matching symbols available for analysis.  Please correct he symbol path and run the analysis again.", "", "Notification", "notificationicon.png");
		}
	}

	internal static string FormatExceptionForLog(Exception ex)
	{
		if (ex == null)
		{
			return null;
		}
		Type type = ex.GetType();
		return $"Type:  {type.Namespace}.{type.Name}\r\n\r\nMessage:   {ex.Message}\r\n\r\nStack Trace:\r\n{ex.StackTrace}";
	}

	internal string BuildReportFileFullPath(string reportFileDirectoryOrFullPath)
	{
		return BuildReportFileFullPath(reportFileDirectoryOrFullPath, Manager.GetDumpFiles());
	}

	private string BuildReportFileFullPath(string reportFileDirectoryOrFullPath, List<string> dumpFiles)
	{
		if (dumpFiles.Count == 0)
		{
			return null;
		}
		if (IsDirectory(reportFileDirectoryOrFullPath))
		{
			string arg = ((AnalysisRuleInfos.Count == 0) ? "Custom" : ((AnalysisRuleInfos.Count <= 1) ? AnalysisRuleInfos[0].DisplayName : "MultipleRules"));
			string arg2 = ((dumpFiles.Count <= 1) ? Path.GetFileNameWithoutExtension(dumpFiles[0]) : "MultipleDumps");
			string path = $"{arg2}_{arg}.mht";
			return GetUniqueFileNameIfAlreadyExists(Path.Combine(reportFileDirectoryOrFullPath, path));
		}
		return reportFileDirectoryOrFullPath;
	}

	private bool IsDirectory(string reportFileDirectoryOrFullPath)
	{
		return !Path.GetFileName(reportFileDirectoryOrFullPath).Contains(".");
	}

	private static string GetUniqueFileNameIfAlreadyExists(string reportFileFullPathDesired)
	{
		int num = 0;
		string text = reportFileFullPathDesired;
		while (true)
		{
			if (num > 0)
			{
				text = reportFileFullPathDesired.Replace(".mht", $" ({num}).mht");
			}
			if (!File.Exists(text))
			{
				break;
			}
			num++;
		}
		return text;
	}

	private void AddTwoTabInfoStatement()
	{
		string description = string.Format("A combination of 32bit dumps and 64bit dumps were selected together for analysis.  The 32bit dumps were analyzed separately from the 64bit dumps and 2 separate report files were generated.<BR><BR><font color='blue'>Click the {0} tab in the browser to view the analysis for the {1}bit dump files</font></BR>", Environment.Is64BitProcess ? "next" : "previous", Environment.Is64BitProcess ? "32" : "64");
		_manager.ReportOther(description, "", "Notification", "debugdiag-analysis.png", 10000, Guid.Empty.ToString());
	}

	private bool HaveSomeDumpFiles()
	{
		foreach (List<string> value in _dumpFilesByType.Values)
		{
			using List<string>.Enumerator enumerator2 = value.GetEnumerator();
			if (enumerator2.MoveNext())
			{
				_ = enumerator2.Current;
				return true;
			}
		}
		return false;
	}

	[HandleProcessCorruptedStateExceptions]
	private bool RunAnalysisRulesInternal(DumpFileType bitness, NetProgress progress, string symbolPath, string imagePath, string reportFileFullPath, bool twoTabs, AnalysisModes analysisMode = AnalysisModes.Interactive)
	{
		try
		{
			_dumpTypeBeingAnalyzedCurrently = bitness;
			List<string> list = _dumpFilesByType[bitness];
			if (list.Count == 0)
			{
				return false;
			}
			string directoryName = Path.GetDirectoryName(reportFileFullPath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			using (File.CreateText(reportFileFullPath))
			{
			}
			if (Environment.Is64BitProcess && bitness == DumpFileType._32bitAll)
			{
				_reportFileNames[1] = reportFileFullPath;
				NetResults results;
				List<object> facts;
				bool result = RunX86Analysis(progress, list, AnalysisRuleInfos, symbolPath, imagePath, reportFileFullPath, twoTabs, out results, out facts);
				Manager.SetX86Results(results);
				Manager.SetX86Facts(facts);
				return result;
			}
			if (twoTabs)
			{
				AddTwoTabInfoStatement();
			}
			int num = 0;
			bool flag = true;
			foreach (string item in list)
			{
				using NetDbgObj netDbgObj = _manager.GetDebugger(item, throwOnBitnessMismatch: false, loadClrRuntime: false, loadClrHeap: false);
				if (flag && !netDbgObj.IsKernelMode)
				{
					flag = false;
				}
			}
			foreach (AnalysisRuleInfo analysisRuleInfo in AnalysisRuleInfos)
			{
				_manager.CurrentAnalysisRule = analysisRuleInfo.DisplayName;
				CodeAnalysisRuleInfo codeAnalysisRuleInfo = analysisRuleInfo as CodeAnalysisRuleInfo;
				XamlAnalysisRuleInfo xamlAnalysisRuleInfo = analysisRuleInfo as XamlAnalysisRuleInfo;
				MexCodeAnalysisRuleInfo mexCodeAnalysisRuleInfo = analysisRuleInfo as MexCodeAnalysisRuleInfo;
				RuleFilterResults filterResult = RuleFilterResults.NoFilter;
				try
				{
					if (codeAnalysisRuleInfo == null && xamlAnalysisRuleInfo == null)
					{
						if (mexCodeAnalysisRuleInfo == null)
						{
							throw new Exception("ruleStatus is of unknown type:  " + analysisRuleInfo.GetType().FullName);
						}
						continue;
					}
					if (codeAnalysisRuleInfo != null)
					{
						IAnalysisRuleBase analysisRuleInstance = codeAnalysisRuleInfo.AnalysisRuleInstance;
						if (!(analysisRuleInstance is IMultiDumpRule))
						{
							continue;
						}
						num++;
						if (analysisRuleInstance is IMultiDumpRuleFilter)
						{
							filterResult = ((!((IMultiDumpRuleFilter)analysisRuleInstance).ShouldRunAnalysis(_manager, analysisMode, ref analysisRuleInfo.FilterReason)) ? RuleFilterResults.FilterReturnedFalse : RuleFilterResults.FilterReturnedTrue);
						}
						else if (flag)
						{
							AutoFilterKernelDumps(analysisRuleInstance, analysisRuleInfo, ref filterResult);
						}
						if (this.OnRuleFilter != null)
						{
							this.OnRuleFilter(this, new RuleFilterEventArgs(analysisRuleInfo.DisplayName, Manager.GetDumpFiles(), filterResult));
						}
						if (filterResult != RuleFilterResults.FilterReturnedFalse)
						{
							analysisRuleInfo.Status = AnalysisRuleStatus.Running;
							StringBuilder stringBuilder = new StringBuilder();
							foreach (string item2 in list)
							{
								stringBuilder.AppendFormat("{0};", item2);
							}
							analysisRuleInfo.RecentDumpFilePath = stringBuilder.ToString();
							if (this.PreExecuteRule != null)
							{
								this.PreExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo.DisplayName, "", ""));
							}
							UpdateProgress(progress, analysisRuleInfo.DisplayName);
							((IMultiDumpRule)analysisRuleInstance).RunAnalysisRule(_manager, progress);
						}
						goto IL_038f;
					}
					if (xamlAnalysisRuleInfo == null)
					{
						goto IL_038f;
					}
					if (((DynamicActivity)ActivityXamlServices.Load(xamlAnalysisRuleInfo.Location)).Properties.Count != 1)
					{
						continue;
					}
					if (this.OnRuleFilter != null)
					{
						this.OnRuleFilter(this, new RuleFilterEventArgs(xamlAnalysisRuleInfo.DisplayName, Manager.GetDumpFiles(), filterResult));
					}
					if (filterResult != RuleFilterResults.FilterReturnedFalse)
					{
						if (this.PreExecuteRule != null)
						{
							this.PreExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo.DisplayName, "", ""));
						}
						UpdateProgress(progress, analysisRuleInfo.DisplayName);
						xamlAnalysisRuleInfo.RunAnalysisRule(7200000, _manager, list);
						if (this.PostExecuteRule != null)
						{
							this.PostExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo.DisplayName, "", ""));
						}
					}
					goto IL_038f;
					IL_038f:
					if (analysisRuleInfo.Status != AnalysisRuleStatus.Complete && analysisRuleInfo.Status != AnalysisRuleStatus.Running && analysisMode == AnalysisModes.Interactive)
					{
						ShowRuleNotCompletedMessage();
					}
				}
				catch (Exception exception)
				{
					ShowRuleNotCompletedMessage();
					analysisRuleInfo.Exception = exception;
				}
				finally
				{
					if (filterResult == RuleFilterResults.FilterReturnedFalse)
					{
						analysisRuleInfo.WasFiltered = true;
					}
					else if (this.PostExecuteRule != null && analysisRuleInfo != null)
					{
						this.PostExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo.DisplayName, "", ""));
					}
					if (analysisRuleInfo.Status == AnalysisRuleStatus.Running)
					{
						analysisRuleInfo.Status = AnalysisRuleStatus.Complete;
					}
				}
			}
			if (num < AnalysisRuleInfos.Count)
			{
				foreach (string item3 in list)
				{
					using NetDbgObj netDbgObj2 = _manager.GetDebugger(item3);
					MexCodeAnalysisRuleInfo.Reset(netDbgObj2);
					foreach (AnalysisRuleInfo analysisRuleInfo2 in AnalysisRuleInfos)
					{
						analysisRuleInfo2.RecentDumpFilePath = item3;
						_manager.CurrentAnalysisRule = analysisRuleInfo2.DisplayName;
						CodeAnalysisRuleInfo codeAnalysisRuleInfo2 = analysisRuleInfo2 as CodeAnalysisRuleInfo;
						XamlAnalysisRuleInfo xamlAnalysisRuleInfo2 = analysisRuleInfo2 as XamlAnalysisRuleInfo;
						MexCodeAnalysisRuleInfo mexCodeAnalysisRuleInfo2 = analysisRuleInfo2 as MexCodeAnalysisRuleInfo;
						RuleFilterResults filterResult2 = RuleFilterResults.NoFilter;
						try
						{
							if (codeAnalysisRuleInfo2 == null && xamlAnalysisRuleInfo2 == null && mexCodeAnalysisRuleInfo2 == null)
							{
								throw new Exception("ruleStatus is of unknown type:  " + analysisRuleInfo2.GetType().FullName);
							}
							if (codeAnalysisRuleInfo2 != null)
							{
								IAnalysisRuleBase analysisRuleInstance2 = codeAnalysisRuleInfo2.AnalysisRuleInstance;
								if (analysisRuleInstance2 is IMultiDumpRule)
								{
									continue;
								}
								bool isCrashDump = netDbgObj2.IsCrashDump;
								if (isCrashDump)
								{
									DataFilesIncludesCrashDumps = true;
								}
								if (_manager.DoHangAnalysisOnCrashDumps || !isCrashDump)
								{
									if (analysisRuleInstance2 is IHangDumpRule || analysisRuleInstance2 is IHangThreadRule)
									{
										if (analysisRuleInstance2 is ISingleDumpRuleFilter)
										{
											filterResult2 = ((!((ISingleDumpRuleFilter)analysisRuleInstance2).ShouldRunAnalysis(netDbgObj2, analysisMode, ref analysisRuleInfo2.FilterReason)) ? RuleFilterResults.FilterReturnedFalse : RuleFilterResults.FilterReturnedTrue);
										}
										else if (netDbgObj2.IsKernelMode)
										{
											AutoFilterKernelDumps(analysisRuleInstance2, analysisRuleInfo2, ref filterResult2);
										}
										if (this.OnRuleFilter != null)
										{
											this.OnRuleFilter(this, new RuleFilterEventArgs(analysisRuleInfo2.DisplayName, Manager.GetDumpFiles(), filterResult2));
										}
									}
									if (filterResult2 != RuleFilterResults.FilterReturnedFalse)
									{
										if (analysisRuleInstance2 is IHangDumpRule)
										{
											analysisRuleInfo2.Status = AnalysisRuleStatus.Running;
											UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName);
											if (this.PreExecuteRule != null)
											{
												this.PreExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
											}
											((IHangDumpRule)analysisRuleInstance2).RunAnalysisRule(_manager, netDbgObj2, progress);
										}
										if (analysisRuleInstance2 is IHangThreadRule)
										{
											foreach (NetDbgThread thread in netDbgObj2.Threads)
											{
												analysisRuleInfo2.Status = AnalysisRuleStatus.Running;
												if (this.PreExecuteRule != null)
												{
													this.PreExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo2.DisplayName, Path.GetFileName(item3), thread.ThreadID.ToString()));
												}
												((IHangThreadRule)analysisRuleInstance2).RunAnalysisRule(_manager, netDbgObj2, thread, progress);
												if (this.PostExecuteRule != null)
												{
													this.PostExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo2.DisplayName, Path.GetFileName(item3), thread.ThreadID.ToString()));
												}
											}
										}
									}
								}
								if (filterResult2 != RuleFilterResults.FilterReturnedFalse && analysisRuleInstance2 is IExceptionThreadRule && isCrashDump)
								{
									if (analysisRuleInstance2 is ISingleDumpRuleFilter)
									{
										filterResult2 = ((!((ISingleDumpRuleFilter)analysisRuleInstance2).ShouldRunAnalysis(netDbgObj2, analysisMode, ref analysisRuleInfo2.FilterReason)) ? RuleFilterResults.FilterReturnedFalse : RuleFilterResults.FilterReturnedTrue);
									}
									else if (netDbgObj2.IsKernelMode)
									{
										AutoFilterKernelDumps(analysisRuleInstance2, analysisRuleInfo2, ref filterResult2);
									}
									if (this.OnRuleFilter != null)
									{
										this.OnRuleFilter(this, new RuleFilterEventArgs(analysisRuleInfo2.DisplayName, Manager.GetDumpFiles(), filterResult2));
									}
									if (filterResult2 != RuleFilterResults.FilterReturnedFalse)
									{
										analysisRuleInfo2.Status = AnalysisRuleStatus.Running;
										UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName);
										if (this.PreExecuteRule != null)
										{
											this.PreExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo2.DisplayName, Path.GetFileName(item3), netDbgObj2.ExceptionThread.ThreadID.ToString()));
										}
										((IExceptionThreadRule)analysisRuleInstance2).RunAnalysisRule(_manager, netDbgObj2, netDbgObj2.ExceptionThread, netDbgObj2.NativeException, netDbgObj2.ManagedException, progress);
									}
								}
								goto IL_0c19;
							}
							if (xamlAnalysisRuleInfo2 != null)
							{
								DynamicActivity dynamicActivity = (DynamicActivity)ActivityXamlServices.Load(xamlAnalysisRuleInfo2.Location);
								if (dynamicActivity.Properties.Count == 1)
								{
									continue;
								}
								bool isCrashDump2 = netDbgObj2.IsCrashDump;
								if (isCrashDump2)
								{
									DataFilesIncludesCrashDumps = true;
								}
								if (_manager.DoHangAnalysisOnCrashDumps || !isCrashDump2)
								{
									if (dynamicActivity.Properties.Count == 2 || dynamicActivity.Properties.Count == 3)
									{
										filterResult2 = xamlAnalysisRuleInfo2.GetRuleFilterResult(netDbgObj2);
										if (this.OnRuleFilter != null)
										{
											this.OnRuleFilter(this, new RuleFilterEventArgs(xamlAnalysisRuleInfo2.DisplayName, Manager.GetDumpFiles(), filterResult2));
										}
									}
									switch (dynamicActivity.Properties.Count)
									{
									case 2:
										if (filterResult2 != RuleFilterResults.FilterReturnedFalse)
										{
											UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName);
											if (this.PreExecuteRule != null)
											{
												this.PreExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
											}
											xamlAnalysisRuleInfo2.RunAnalysisRule(7200000, _manager, netDbgObj2);
											if (this.PostExecuteRule != null)
											{
												this.PostExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
											}
										}
										break;
									case 3:
										if (filterResult2 == RuleFilterResults.FilterReturnedFalse)
										{
											break;
										}
										foreach (NetDbgThread thread2 in netDbgObj2.Threads)
										{
											UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName, thread2.ThreadID.ToString());
											if (this.PreExecuteRule != null)
											{
												this.PreExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), thread2.ThreadID.ToString()));
											}
											xamlAnalysisRuleInfo2.RunAnalysisRule(7200000, _manager, netDbgObj2, thread2);
											if (this.PostExecuteRule != null)
											{
												this.PostExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), thread2.ThreadID.ToString()));
											}
											if (xamlAnalysisRuleInfo2.Exception != null)
											{
												break;
											}
										}
										break;
									}
								}
								if (dynamicActivity.Properties.Count == 5 && netDbgObj2.IsCrashDump)
								{
									if (this.PreExecuteRule != null)
									{
										this.PreExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), netDbgObj2.ExceptionThread.ThreadID.ToString()));
									}
									UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName);
									xamlAnalysisRuleInfo2.RunAnalysisRule(7200000, _manager, netDbgObj2, netDbgObj2.ExceptionThread, netDbgObj2.NativeException, netDbgObj2.ManagedException);
									if (this.PostExecuteRule != null)
									{
										this.PostExecuteRule(this, new RuleExecutionEventArgs(xamlAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), netDbgObj2.ExceptionThread.ThreadID.ToString()));
									}
								}
								goto IL_0c19;
							}
							UpdateProgress(progress, analysisRuleInfo2.DisplayName, netDbgObj2.DumpFileShortName);
							mexCodeAnalysisRuleInfo2.Reset();
							filterResult2 = mexCodeAnalysisRuleInfo2.FilterResult;
							analysisRuleInfo2.FilterReason = mexCodeAnalysisRuleInfo2.FilterReason;
							if (this.OnRuleFilter != null)
							{
								this.OnRuleFilter(this, new RuleFilterEventArgs(analysisRuleInfo2.DisplayName, Manager.GetDumpFiles(), filterResult2));
							}
							if (filterResult2 != RuleFilterResults.FilterReturnedFalse)
							{
								UdeScanRule udeScanRuleInstance = mexCodeAnalysisRuleInfo2.UdeScanRuleInstance;
								UdeRuleAttribute ruleInfo = mexCodeAnalysisRuleInfo2.RuleInfo;
								if (this.PreExecuteRule != null)
								{
									this.PreExecuteRule(this, new RuleExecutionEventArgs(mexCodeAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
								}
								analysisRuleInfo2.Status = AnalysisRuleStatus.Running;
								udeScanRuleInstance.RunRule();
								WriteMexReport(udeScanRuleInstance, ruleInfo);
								if (this.PostExecuteRule != null)
								{
									this.PostExecuteRule(this, new RuleExecutionEventArgs(mexCodeAnalysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
								}
							}
							goto IL_0c19;
							IL_0c19:
							if ((analysisRuleInfo2.Exception != null || (analysisRuleInfo2.Status != AnalysisRuleStatus.Complete && analysisRuleInfo2.Status != AnalysisRuleStatus.Running)) && analysisMode == AnalysisModes.Interactive)
							{
								ShowRuleNotCompletedMessage();
							}
						}
						catch (Exception exception2)
						{
							ShowRuleNotCompletedMessage();
							analysisRuleInfo2.Exception = exception2;
						}
						finally
						{
							if (filterResult2 == RuleFilterResults.FilterReturnedFalse)
							{
								analysisRuleInfo2.WasFiltered = true;
							}
							else if (codeAnalysisRuleInfo2 != null && this.PostExecuteRule != null)
							{
								this.PostExecuteRule(this, new RuleExecutionEventArgs(analysisRuleInfo2.DisplayName, Path.GetFileName(item3), ""));
							}
							if (analysisRuleInfo2.Status == AnalysisRuleStatus.Running)
							{
								analysisRuleInfo2.Status = AnalysisRuleStatus.Complete;
							}
						}
					}
				}
			}
		}
		finally
		{
			_dumpTypeBeingAnalyzedCurrently = DumpFileType.None;
		}
		return true;
	}

	private void WriteMexReport(UdeScanRule udeRule, UdeRuleAttribute ruleInfo)
	{
		string htmlOutput = udeRule.HtmlOutput;
		if (!string.IsNullOrEmpty(htmlOutput))
		{
			Manager.WriteLine(htmlOutput);
		}
		else
		{
			string verboseOutput = udeRule.VerboseOutput;
			if (!string.IsNullOrEmpty(verboseOutput))
			{
				Manager.WriteLine($"<pre style='font-family:monospace;line-height:.55'>{verboseOutput}</pre>");
			}
		}
		foreach (UdeScanRule.ReportedData reportedData in udeRule.ReportedDatas)
		{
			string recommendation = string.Empty;
			string description = ruleInfo.Description;
			uint bemis = reportedData.Bemis;
			if (!ruleInfo.DisableUDE)
			{
				_ = reportedData.SSID;
			}
			if (bemis != 0)
			{
				string arg = string.Format("<a href='http://bemis.partners.extranet.microsoft.com/{0}/EN-US'>BEMIS Article #{0}</a>", bemis);
				recommendation = $"See the following Article for more information:<BR><BR>&nbsp;&nbsp;&nbsp;&nbsp;{arg}";
			}
			else
			{
				uint bugID = reportedData.BugID;
				string text = null;
				if (bugID != 0)
				{
					string text2 = (string.IsNullOrEmpty(reportedData.BugDB) ? string.Empty : reportedData.BugDB.ToLower());
					text = $"{text2} : #{bugID}";
					if (text2 == "winse" || text2 == "windowsse")
					{
						text = $"<a href='http://bugcheck/bugs/WindowsSE/{bugID}'>{text}</a>";
					}
					recommendation = $"See the following <font color='red'>BUG</font> for more information:<BR><BR>&nbsp;&nbsp;&nbsp;&nbsp;{text}";
				}
			}
			Manager.ReportError(description, recommendation, 0, reportedData.SSID);
		}
	}

	private static void AutoFilterKernelDumps(IAnalysisRuleBase analysisRule, AnalysisRuleInfo ruleStatus, ref RuleFilterResults filterResult)
	{
		if (!(analysisRule is IAnalysisRuleMetadata2 { SupportsKernelDumps: not false }) && !(analysisRule is MexCodeAnalysisRuleInfo) && (!(ruleStatus is CodeAnalysisRuleInfo codeAnalysisRuleInfo) || (!codeAnalysisRuleInfo.DisplayName.ToUpper().Contains("KERNEL") && !codeAnalysisRuleInfo.Description.ToUpper().Contains("KERNEL"))))
		{
			filterResult = RuleFilterResults.FilterReturnedFalse;
			ruleStatus.FilterReason = "This rule applies only to <b>usermode</b> dumps.  However, only <b>kernel</b> dumps were selected for analysis";
		}
	}

	private void UpdateProgress(NetProgress progress, string ruleName, string dumpFileShortName, string threadNumber)
	{
		progress.OverallStatus = "Rule:  " + ruleName;
		string text = null;
		if (!string.IsNullOrEmpty(dumpFileShortName))
		{
			text = "Dump:  " + dumpFileShortName;
			if (!string.IsNullOrEmpty(threadNumber))
			{
				text = text + ".  Thread #:  " + threadNumber;
			}
			progress.CurrentStatus = text;
		}
	}

	private void UpdateProgress(NetProgress progress, string ruleName, string dumpFileShortName)
	{
		UpdateProgress(progress, ruleName, dumpFileShortName, null);
	}

	private void UpdateProgress(NetProgress progress, string ruleName)
	{
		UpdateProgress(progress, ruleName, null, null);
	}

	private void ShowRuleNotCompletedMessage()
	{
		if (!_ruleNotCompletedMessageWritten)
		{
			_manager.ReportOther("One or more of the selected rules were not completed.", "See the <a href='#Table_Summary'>Analysis Rule Summary</a> for more information.", "Notification", "debugdiag-analysis.png", 100000, "{4d904f3a-d50e-43f7-9258-f30cddfc3e4c}");
			_ruleNotCompletedMessageWritten = true;
		}
	}

	/// <summary>
	/// Add a list of dump files to be analyzed
	/// </summary>
	/// <param name="dumpFiles">List of strings containing the dump file names with full path information</param>
	/// <param name="symbolPath">String that contains the symbol location</param>
	public void AddDumpFiles(List<string> dumpFiles, string symbolPath)
	{
		SymbolPath = symbolPath;
		foreach (string dumpFile in dumpFiles)
		{
			AddDumpFile(dumpFile, symbolPath);
		}
	}

	/// <summary>
	/// Add a dump to the list of dumps to be analyzed 
	/// </summary>
	/// <param name="dumpFile">Dump file name with full path</param>
	/// <param name="symbolPath">String that contains the symbol location</param>
	public void AddDumpFile(string dumpFile, string symbolPath)
	{
		SymbolPath = symbolPath;
		if ((NetDbgObj.GetDumpFileType(dumpFile) & DumpFileType._32bitAll) > DumpFileType.None)
		{
			_dumpFilesByType[DumpFileType._32bitAll].Add(dumpFile);
		}
		else
		{
			_dumpFilesByType[DumpFileType._64bitAll].Add(dumpFile);
		}
	}

	private List<string> GetDumpFiles(DumpFileType dumpFileType)
	{
		if (_dumpTypeBeingAnalyzedCurrently == DumpFileType.None)
		{
			List<string> list = new List<string>();
			{
				foreach (List<string> value in _dumpFilesByType.Values)
				{
					foreach (string item in value)
					{
						list.Add(item);
					}
				}
				return list;
			}
		}
		return _dumpFilesByType[dumpFileType];
	}

	/// <summary>
	/// This property retuns a List of Strings containing the file names of the dumps currently being analyzed .
	/// </summary>
	/// <returns>List of strings with the file name and paths of the dumps</returns>
	public List<string> GetDumpFiles()
	{
		return GetDumpFiles(_dumpTypeBeingAnalyzedCurrently);
	}

	private bool RunX86Analysis(NetProgress progress, List<string> dumpFiles, List<AnalysisRuleInfo> analysisRuleInfos, string symbolPath, string imagePath, string reportFileFullPath, bool twoTabs, out NetResults results, out List<object> facts)
	{
		results = null;
		facts = null;
		progress.SetCurrentRange(0, 2);
		progress.CurrentStatus = "Connecting to 32 bit Analyzer...";
		progress.CurrentPosition = 1;
		try
		{
			Guid guid = Guid.NewGuid();
			new ManualResetEvent(initialState: false);
			string text = null;
			foreach (string assemblyResolvePath in AssemblyResolvePaths)
			{
				text = Path.Combine(assemblyResolvePath, "DebugDiag.x86AnalysisHost.exe");
				if (File.Exists(text))
				{
					break;
				}
			}
			if (!File.Exists(text))
			{
				text = Path.Combine(ShadowCopyDir, "x86Support", "DebugDiag.x86AnalysisHost.exe");
				Console.WriteLine("2. checking {0}", text);
			}
			if (!File.Exists(text))
			{
				text = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "x86Support", "DebugDiag.x86AnalysisHost.exe");
				Console.WriteLine("3. checking {0}", text);
			}
			if (!File.Exists(text))
			{
				text = Path.Combine(InstallDir, "x86Support", "DebugDiag.x86AnalysisHost.exe");
				Console.WriteLine("4. checking {0}", text);
			}
			if (!File.Exists(text))
			{
				throw new Exception($"Could not locate x86AnalysisHost.\r\n\tDDv2InstallDir = {InstallDir}\r\n\tx86AnalysisHostPath = {text}");
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text, guid.ToString());
			_x86AnalysisHostErrors.Clear();
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.ErrorDialog = false;
			processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			processStartInfo.CreateNoWindow = true;
			_x86AnalysisHostProcess = Process.Start(processStartInfo);
			_x86AnalysisHostProcess.OutputDataReceived += OnProcessOutput;
			_x86AnalysisHostProcess.ErrorDataReceived += OnProcessError;
			_x86AnalysisHostProcess.BeginOutputReadLine();
			_x86AnalysisHostProcess.BeginErrorReadLine();
			_x86AnalysisHostProcess.Exited += delegate(object s, EventArgs e)
			{
				Process obj2 = (Process)s;
				obj2.OutputDataReceived -= OnProcessOutput;
				obj2.ErrorDataReceived -= OnProcessError;
			};
			Uri uri = new Uri($"net.pipe://localhost/{guid.ToString()}");
			TimeSpan analysisCompletedTimeout = Settings.Default.AnalysisCompletedTimeout;
			TimeSpan x86AnalysisStartupTimeout = Settings.Default.x86AnalysisStartupTimeout;
			InstanceContext callbackInstance = new InstanceContext(new AnalysisServiceProgress(progress));
			NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
			netNamedPipeBinding.MaxReceivedMessageSize = 2147483647L;
			TimeSpan timeSpan2 = (netNamedPipeBinding.ReceiveTimeout = analysisCompletedTimeout);
			TimeSpan timeSpan4 = (netNamedPipeBinding.SendTimeout = timeSpan2);
			TimeSpan openTimeout = (netNamedPipeBinding.CloseTimeout = timeSpan4);
			netNamedPipeBinding.OpenTimeout = openTimeout;
			DuplexChannelFactory<IAnalysisService> factory = new DuplexChannelFactory<IAnalysisService>(callbackInstance, netNamedPipeBinding, new EndpointAddress(uri));
			x86AnalysisService.ConfigureAnalysisServiceEndpoint(factory.Endpoint.Contract);
			Thread.Sleep(500);
			int num = 0;
			int num2 = (int)x86AnalysisStartupTimeout.TotalMilliseconds / 500 - 1;
			string text2 = null;
			while (text2 == null)
			{
				try
				{
					ThreadPool.QueueUserWorkItem(delegate
					{
						_proxyEvent.Reset();
						_analysisService = factory.CreateChannel();
						_proxyEvent.Set();
					});
					if (!_proxyEvent.WaitOne(10000))
					{
						throw new Exception("Could not create x86AnalysisHost proxy within allowable timeout.");
					}
					_analysisService = factory.CreateChannel();
					results = _analysisService.RunAnalysisRules(analysisRuleInfos, dumpFiles, symbolPath, imagePath, reportFileFullPath, analysisCompletedTimeout, twoTabs, Manager.SourceInfoEnabled, Manager.SetContextOnCrashDumps, Manager.DoHangAnalysisOnCrashDumps, Manager.IncludeHttpHeadersInClientConns, Manager.GroupIdenticalStacks, Manager.InstructionAddressEnabled, out facts);
					try
					{
						((ICommunicationObject)_analysisService).Close();
					}
					catch (Exception)
					{
					}
					return true;
				}
				catch (FaultException)
				{
					throw;
				}
				catch (CommunicationException ex3)
				{
					if (num++ == num2)
					{
						throw;
					}
					if (ex3 is EndpointNotFoundException)
					{
						Thread.Sleep(500);
					}
					if (_analysisService != null)
					{
						((ICommunicationObject)_analysisService).Abort();
					}
					if (_x86AnalysisHostProcess == null || _x86AnalysisHostProcess.HasExited)
					{
						break;
					}
					continue;
				}
			}
		}
		catch (FaultException<AnalysisTimeoutException> exception)
		{
			ShowRuleNotCompletedMessage();
			foreach (AnalysisRuleInfo analysisRuleInfo in analysisRuleInfos)
			{
				analysisRuleInfo.Exception = exception;
			}
		}
		finally
		{
			Kill();
		}
		return false;
	}

	private void OnProcessOutput(object sender, DataReceivedEventArgs e)
	{
		Trace.WriteLine("x86AnalysisHost Output:  " + e.Data);
	}

	private void OnProcessError(object sender, DataReceivedEventArgs e)
	{
		_x86AnalysisHostErrors.Append(e.Data);
		Trace.WriteLine("x86AnalysisHost ERROR:  " + e.Data);
	}

	/// <summary>
	/// This function will show the reports that where generated on IE
	/// </summary>
	/// <param name="type">DumpFile type information for the reports that need to be shown</param>
	/// <returns>Returns true if the reports where successfully launched on IE</returns>
	public bool ShowReportFiles(DumpFileType type = DumpFileType._32bitAll | DumpFileType._64bitAll)
	{
		bool flag = true;
		if ((type & DumpFileType._32bitAll) > DumpFileType.None && !string.IsNullOrEmpty(_reportFileNames[0]))
		{
			flag = ShowReportFile(_reportFileNames[0]);
		}
		if ((type & DumpFileType._32bitAll) > DumpFileType.None && !string.IsNullOrEmpty(_reportFileNames[1]))
		{
			flag = ShowReportFile(_reportFileNames[1]) && flag;
		}
		return flag;
	}

	private bool ShowReportFile(string reportFilePath)
	{
		Process process = new Process();
		process.StartInfo = new ProcessStartInfo();
		process.StartInfo.UseShellExecute = true;
		process.StartInfo.FileName = reportFilePath;
		return process.Start();
	}

	/// <summary>
	/// Initialize the <c>NetScriptManager</c> Object setting the default properties for the reports
	/// </summary>
	/// <param name="includeSourceAndLineInformationInAnalysisReports">Boolean value to include source and line information on the report when true.</param>
	/// <param name="setContextOnCrashDumps">Boolean value to get the context of the exception for crash dumps when set to true.</param>
	/// <param name="doHangAnalysisOnCrashDumps">Boolean value to perform a combined Crash and Hang Analysis when set to true</param>
	/// <param name="includeHttpHeadersInClientConns">Boolean value to include HEader information on Client connection information for web applications.</param>
	public void Initialize(bool includeSourceAndLineInformationInAnalysisReports, bool setContextOnCrashDumps, bool doHangAnalysisOnCrashDumps, bool includeHttpHeadersInClientConns)
	{
		_manager.Initialize(includeSourceAndLineInformationInAnalysisReports, setContextOnCrashDumps, doHangAnalysisOnCrashDumps, includeHttpHeadersInClientConns, groupIdenticalStacks: true, includeInstructionPointerInAnalysisReports: false);
	}

	/// <summary>
	/// Initialize the <c>NetScriptManager</c> Object setting the default properties for the reports
	/// </summary>
	/// <param name="includeSourceAndLineInformationInAnalysisReports">Boolean value to include source and line information on the report when true.</param>
	/// <param name="setContextOnCrashDumps">Boolean value to get the context of the exception for crash dumps when set to true.</param>
	/// <param name="doHangAnalysisOnCrashDumps">Boolean value to perform a combined Crash and Hang Analysis when set to true</param>
	/// <param name="includeHttpHeadersInClientConns">Boolean value to include HEader information on Client connection information for web applications.</param>            
	/// <param name="groupIdenticalStacks">Boolean value to group identical thread call stacks together into a single call stack in the report.</param>
	public void Initialize(bool includeSourceAndLineInformationInAnalysisReports, bool setContextOnCrashDumps, bool doHangAnalysisOnCrashDumps, bool includeHttpHeadersInClientConns, bool groupIdenticalStacks, bool includeInstructionPointerInAnalysisReports)
	{
		_manager.Initialize(includeSourceAndLineInformationInAnalysisReports, setContextOnCrashDumps, doHangAnalysisOnCrashDumps, includeHttpHeadersInClientConns, groupIdenticalStacks, includeInstructionPointerInAnalysisReports);
	}

	private static string GetDefaultRegVal(string keyName)
	{
		return (string)Registry.GetValue(keyName, "", string.Empty);
	}

	/// <summary>
	/// This method allows to stop the execution of the rules.
	/// </summary>
	public void Kill()
	{
		if (_x86AnalysisHostProcess == null)
		{
			return;
		}
		lock (_x86AnalysisHostProcess)
		{
			if (_x86AnalysisHostProcess != null && !_x86AnalysisHostProcess.HasExited)
			{
				try
				{
					if (!_x86AnalysisHostProcess.WaitForExit(0))
					{
						_x86AnalysisHostProcess.Kill();
					}
				}
				catch (Exception)
				{
				}
			}
			_x86AnalysisHostProcess = null;
		}
	}
}
