#define TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DebugDiag.DbgEng;
using DebugDiag.DbgLib;
using DebugDiag.DotNet.DbgEngInterop;
using DebugDiag.DotNet.DbgLibInterop;
using DebugDiag.DotNet.Kernel;
using DebugDiag.DotNet.WinDE;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using ClrObject = Microsoft.Diagnostics.RuntimeExt.ClrObject;
using Microsoft.Mex.DotNetDbg;
using Microsoft.Mex.Framework;
using Symbols;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents an instance of the debugger.  An instance of this object is obtained from the <see cref="M:DebugDiag.DotNet.NetScriptManager.GetDebugger(System.String)" />
/// method of the <see cref="T:DebugDiag.DotNet.NetScriptManager" /> object.
/// </summary>
/// <remarks>
/// <example>
/// This sample shows how to get a reference of the debugger instance.
/// <code language="cs">
/// //Declares an instance of NetAnalyzer
/// using (NetAnalyzer analyzer = new NetAnalyzer())
/// {
///     //Get an instance of the debugger through the NetScriptManager object
///     NetScriptManager manager = analyzer.Manager;
///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
///
///     //... your code goes here
///
///     // Dispose debugger object to free unmanaged resources
///     debugger.Dispose();
/// }
/// </code>
/// </example>
/// </remarks>
public class NetDbgObj : IDisposable
{
	private class Read
	{
		internal unsafe static int ReadVirtual64(NetDbgObj debugger, ulong address, out ulong value)
		{
			ulong num = default(ulong);
			int num2 = debugger.DebugDataSpaces4.ReadVirtual(address, (IntPtr)(&num), 8u, null);
			value = ((num2 >= 0) ? num : 0);
			return num2;
		}

		internal unsafe static int ReadUnicodeStringVirtualWide(NetDbgObj debugger, ulong address, out string value, uint maxLength = 1024u)
		{
			StringBuilder stringBuilder = new StringBuilder((int)maxLength);
			value = null;
			uint num = default(uint);
			int num2 = debugger.DebugDataSpaces4.ReadUnicodeStringVirtualWide(address, maxLength, stringBuilder, maxLength, &num);
			if (Utils.SUCCEEDED(num2))
			{
				value = stringBuilder.ToString();
			}
			return num2;
		}
	}

	private BugCheckData bugCheckData;

	private bool? isKernelMode;

	/// <summary>
	/// This property contains the exception that ocurred, if any, while attempting to initialize the Microsoft.Diagnostics.Runtime.ClrRuntime.  
	/// Check this property after opening a dump file to determine if managed analysis will be possible.
	/// </summary>
	public Exception ClrInitException;

	private string dacFileLocation;

	private ClrHeap clrHeap;

	private IDbgObj4 _legacyDebugger;

	private DebugDiag.DbgEng.IDebugClient _rawDebugger;

	private bool _disposed;

	private Dictionary<string, object> _methodOrPropCaches = new Dictionary<string, object>();

	private string _dumpFileShortName;

	private string _dumpFileFullPath;

	private List<NetDbgThread> _threads;

	private List<NetDbgThread> _managedThreads;

	private ulong _pebAddress;

	private static LibraryModule dbglibModule;

	private static bool? _isRegistered;

	private Dictionary<ulong, string> _heapCache;

	private bool _heapCacheInitialized;

	private const bool MANAGED_HEAP_CACHE_ENABLED = false;

	private static object dacLibrary = null;

	private DebugUtilities mexDebugUtilities;

	private ThreadInfo kernelFaultThread;

	private DebugDiag.DbgEng.IDebugControl2 _debugControl4;

	private DebugDiag.DbgEng.IDebugDataSpaces4 _debugDataSpaces4;

	private DebugDiag.DbgEng.IDebugAdvanced2 _debugAdvanced2;

	private string commandLine;

	private bool envVarsLoaded;

	private Dictionary<string, string> envVars = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

	private static Dictionary<string, LibraryModule> _loadForeverModules = new Dictionary<string, LibraryModule>();

	private Dictionary<string, object> _extensions = new Dictionary<string, object>();

	private Threads mexThreads;

	public bool IsKernelMode
	{
		get
		{
			if (!isKernelMode.HasValue)
			{
				((DebugDiag.DbgEng.IDebugControl)RawDebugger).GetDebuggeeType(out var Class, out var _);
				isKernelMode = Class == DebugDiag.DbgEng.DEBUG_CLASS.KERNEL;
			}
			return isKernelMode.Value;
		}
	}

	public BugCheckData BugCheck
	{
		get
		{
			if (bugCheckData == null)
			{
				bugCheckData = new BugCheckData();
				if (((DebugDiag.DbgEng.IDebugControl)RawDebugger).ReadBugCheckData(out var Code, out var Arg, out var Arg2, out var Arg3, out var Arg4) >= 0)
				{
					Code &= 0xEFFFFFFFu;
					bugCheckData.BugCheckCode = Code;
					bugCheckData.Arg1 = Arg;
					bugCheckData.Arg2 = Arg2;
					bugCheckData.Arg3 = Arg3;
					bugCheckData.Arg4 = Arg4;
				}
			}
			return bugCheckData;
		}
	}

	/// <summary>
	/// This property returns an instance of the <see cref="T:DebugDiag.DotNet.NetDbgThread" /> object which contains information about the thread that caused an exception. 
	/// </summary>
	public NetDbgThread ExceptionThread => new NetDbgThread(_legacyDebugger.ExceptionThread, this);

	/// <summary>
	/// This property returns an instance of the <see cref="T:DebugDiag.DotNet.NetDbgException" /> object.
	/// </summary>
	public NetDbgException NativeException
	{
		get
		{
			if (_legacyDebugger == null || _legacyDebugger.Exception == null)
			{
				return null;
			}
			return new NetDbgException(_legacyDebugger.Exception, this);
		}
	}

	/// <summary>
	/// This property returns an instance of the ClrException object.
	/// </summary>
	public ClrException ManagedException
	{
		get
		{
			ClrException result = null;
			NetDbgThread exceptionThread = ExceptionThread;
			if (exceptionThread != null && exceptionThread.ManagedThread != null)
			{
				result = exceptionThread.ManagedThread.CurrentException;
			}
			return result;
		}
	}

	/// <summary>
	/// This property returns a <c>Microsoft.Diagnostics.Runtime.ClrRuntime</c> object that allows access to Managed objects and structures on the dump.
	/// </summary>
	public ClrRuntime ClrRuntime { get; set; }

	public List<ClrRuntime> ClrRuntimes { get; set; }

	/// <summary>
	/// This read only property provides the name of the DAC file used to access the managed objects and structures on the dump.
	/// </summary>
	public string DacFileName
	{
		get
		{
			if (ClrVersionInfo != null && ClrVersionInfo.DacInfo != null)
			{
				return ClrVersionInfo.DacInfo.FileName;
			}
			return null;
		}
	}

	/// <summary>
	/// This read only property provides the path of the DAC file used to access the managed objects and structures on the dump.
	/// </summary>
	public string DacFileLocation
	{
		get
		{
			return dacFileLocation;
		}
		private set
		{
			dacFileLocation = value;
		}
	}

	/// <summary>
	/// True if there are multiple CLR Runtimes loaded in the process, each having one or more managed threads.
	/// </summary>
	public bool HasMultipleClrRuntimesWithThreads { get; set; }

	/// <summary>
	/// A cached reference to the CLR heap, if the heap is walkable.
	/// Prefer this to NetDbgObj.ClrRuntime.GetHeap(), which can throw.
	/// </summary>
	public ClrHeap ClrHeap
	{
		get
		{
			return clrHeap;
		}
		set
		{
			clrHeap = value;
		}
	}

	private bool EnableCaching { get; set; }

	private static LibraryModule DbglibModule
	{
		get
		{
			if (dbglibModule == null)
			{
				dbglibModule = GetLibraryModule("dbglib.dll");
			}
			return dbglibModule;
		}
	}

	private static bool IsRegistered
	{
		get
		{
			if (!_isRegistered.HasValue)
			{
				_isRegistered = true;
				try
				{
					Marshal.FinalReleaseComObject(new DbgControlClass_Legacy());
				}
				catch (COMException)
				{
					_isRegistered = false;
				}
			}
			return _isRegistered.Value;
		}
	}

	/// <summary>
	/// This property returns true whenever the process is 32 bits or false otherwise.
	/// </summary>
	public bool Is32Bit => Is32BitLegacy(_legacyDebugger);

	/// <summary>
	/// This property returns true if any version of the .Net CLR is loaded into the process, or false otherwise.
	/// </summary>
	public bool IsManaged { get; set; }

	/// <summary>
	/// This property returns a List of <c>NetDbgThread</c> for all the managed threads found on the process.
	/// </summary>
	public List<NetDbgThread> ManagedThreads
	{
		get
		{
			if (_managedThreads == null)
			{
				_managedThreads = new List<NetDbgThread>();
				foreach (NetDbgThread thread in Threads)
				{
					_managedThreads.Add(thread);
				}
			}
			return _managedThreads;
		}
	}

	/// <summary>
	/// Returns the number of logic cores found on the OS
	/// </summary>
	public int NumberProcessors
	{
		get
		{
			uint Number = 0u;
			((DebugDiag.DbgEng.IDebugControl)RawDebugger).GetNumberProcessors(out Number);
			return (int)Number;
		}
	}

	/// <summary>
	/// This property returns the file name and extension of the current dump.
	/// </summary>
	public string DumpFileShortName
	{
		get
		{
			if (string.IsNullOrEmpty(_dumpFileShortName))
			{
				_dumpFileShortName = Path.GetFileName(DumpFileFullPath);
			}
			return _dumpFileShortName;
		}
	}

	/// <summary>
	/// This property returns the full path an file name of the current dump file.
	/// </summary>
	public string DumpFileFullPath => _dumpFileFullPath;

	/// <summary>
	/// This property returns a List of <c>NetDbgThread</c> objects find on the dump file being analyzed
	/// </summary>
	public List<NetDbgThread> Threads
	{
		get
		{
			if (_threads == null)
			{
				_threads = new List<NetDbgThread>();
				foreach (IDbgThread item in _legacyDebugger.ThreadInfo)
				{
					_threads.Add(new NetDbgThread(item, this));
				}
			}
			return _threads;
		}
	}

	/// <summary>
	/// This Property returns the ClrInfo that corresponds to the ClrRuntime that has been selected for analysis.  Targets may have more than one ClrRuntime loaded SxS,
	/// but only one will be selected for analysis.
	/// </summary>
	public ClrInfo ClrVersionInfo { get; set; }

	/// <summary>
	/// This property returns a List of <c>ClrObject</c> containing all the System.Web.HttpContext objects found on the managed heap of the process.
	/// </summary>
	public List<ClrObject> AspxPages
	{
		get
		{
			if (ClrRuntime == null)
			{
				return null;
			}
			List<ClrObject> list = new List<ClrObject>();
			foreach (ClrObject item in EnumerateHeapObjects("System.Web.HttpContext"))
			{
				list.Add(item);
			}
			return list;
		}
	}

	/// <summary>
	/// This property returns the date and time that the dump file was created. 
	/// </summary>
	[Obsolete("Use DumpCreationTime instead")]
	public DateTime CreateTime => DumpCreationTime;

	public DebugUtilities MexDebugUtilities
	{
		get
		{
			return mexDebugUtilities;
		}
		private set
		{
			mexDebugUtilities = value;
			InitKernel(value);
		}
	}

	/// <summary>
	/// This property returns True if extended thread information is available otherwise it returns False.
	/// </summary>
	public bool ExtendedThreadInfoAvailable => _legacyDebugger.ExtendedThreadInfoAvailable;

	/// <summary>
	/// This property returns an instance of the ModuleInfo object. 
	/// </summary>
	public IModuleInfo Modules => _legacyDebugger.ModuleInfo;

	/// <summary>
	/// This property returns an instance of the CritSecInfo object.  
	/// The CritSecInfo object can be used to retrieve DbgCritSec objects which are used to query information about critical sections in the dump file.
	/// </summary>
	public ICritSecInfo CritSecs => _legacyDebugger.CritSecInfo;

	/// <summary>
	/// This property returns True if the dump file was generated when an exception occurred in the process, otherwise it returns False.
	/// </summary>
	public bool IsCrashDump
	{
		get
		{
			if (!IsKernelMode)
			{
				return _legacyDebugger.IsCrashDump;
			}
			if (((DebugDiag.DbgEng.IDebugControl)RawDebugger).ReadBugCheckData(out var Code, out var _, out var _, out var _, out var _) >= 0)
			{
				switch (Code & 0xEFFFFFFFu)
				{
				case 128u:
				case 273u:
					return false;
				case 226u:
					return false;
				case 0u:
					return false;
				case 239u:
				case 244u:
					return false;
				case 158u:
				case 234u:
				case 257u:
				case 307u:
					return false;
				case 3735936685u:
					return false;
				}
			}
			if (GetModuleByModuleName("myfault") != null)
			{
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// This property returns the process ID. 
	/// </summary>
	public int ProcessID => _legacyDebugger.ProcessID;

	/// <summary>
	/// This property returns the Major version number of the operating system. 
	/// </summary>
	public int OSVersionMajor => _legacyDebugger.OSVersionMajor;

	/// <summary>
	/// This property returns the Minor version number of the operating system. 
	/// </summary>
	public int OSVersionMinor => _legacyDebugger.OSVersionMinor;

	/// <summary>
	/// This property returns an instance of the EnvironmentVariables object.
	/// </summary>
	public IEnvironmentVariables EnvironmentVariables => _legacyDebugger.EnvironmentVariables;

	/// <summary>
	/// This property returns a dictionary of the environment variables in the target process
	/// </summary>
	public Dictionary<string, string> EnvironmentVariablesDictionary
	{
		get
		{
			LoadEnvironmentVariablesDictionary();
			return envVars;
		}
	}

	/// <summary>
	/// This property returns the address of the Process Environment Block (PEB) in the target process
	/// </summary>
	public double PebAddress
	{
		get
		{
			if (_pebAddress == 0L)
			{
				((DebugDiag.DbgEng.IDebugSystemObjects2)RawDebugger).GetCurrentProcessPeb(out var Offset);
				_pebAddress = Offset;
			}
			return _pebAddress;
		}
	}

	private DebugDiag.DbgEng.IDebugControl2 DebugControl4
	{
		get
		{
			if (_debugControl4 == null)
			{
				_debugControl4 = RawDebugger as DebugDiag.DbgEng.IDebugControl2;
			}
			return _debugControl4;
		}
	}

	private DebugDiag.DbgEng.IDebugDataSpaces4 DebugDataSpaces4
	{
		get
		{
			if (_debugDataSpaces4 == null)
			{
				_debugDataSpaces4 = RawDebugger as DebugDiag.DbgEng.IDebugDataSpaces4;
			}
			return _debugDataSpaces4;
		}
	}

	private DebugDiag.DbgEng.IDebugAdvanced2 DebugAdvanced2
	{
		get
		{
			if (_debugAdvanced2 == null)
			{
				_debugAdvanced2 = RawDebugger as DebugDiag.DbgEng.IDebugAdvanced2;
			}
			return _debugAdvanced2;
		}
	}

	public string CommandLine => commandLine ?? (commandLine = GetTargetCommandLine());

	/// <summary>
	/// This property returns the type of the memory dumps. For example: MINI DUMPS, FULL DUMPS etc.
	/// </summary>
	public string DumpType => _legacyDebugger.DumpType;

	/// <summary>
	/// This property returns the time the dump file was created. 
	/// </summary>
	public DateTime DumpCreationTime
	{
		get
		{
			ThrowIfLiveTarget();
			uint TimeDate = 0u;
			((DebugDiag.DbgEng.IDebugControl2)RawDebugger).GetCurrentTimeDate(out TimeDate);
			return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(TimeDate).ToLocalTime();
		}
	}

	private bool IsSnapshotDump
	{
		get
		{
			ThrowIfLiveTarget();
			string text = DumpComment.ToLower();
			if (text.Contains("|") && text.Contains("snapshot"))
			{
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// This property returns the process uptime. 
	/// </summary>
	public double ProcessUpTime
	{
		get
		{
			double num = _legacyDebugger.ProcessUpTime;
			if (num == 0.0 || IsSnapshotDump)
			{
				num = (DumpCreationTime - Threads[0].CreateTime).TotalSeconds;
			}
			return num;
		}
	}

	/// <summary>
	/// This property returns the comment string from a dump file. Not valid for live targets.
	/// </summary>
	public unsafe string DumpComment
	{
		get
		{
			ThrowIfLiveTarget();
			IntPtr zero = IntPtr.Zero;
			zero = Marshal.AllocHGlobal(1048576);
			string text = string.Empty;
			try
			{
				Microsoft.Mex.DotNetDbg.DEBUG_READ_USER_MINIDUMP_STREAM dEBUG_READ_USER_MINIDUMP_STREAM = default(Microsoft.Mex.DotNetDbg.DEBUG_READ_USER_MINIDUMP_STREAM);
				dEBUG_READ_USER_MINIDUMP_STREAM.StreamType = Microsoft.Mex.DotNetDbg.MINIDUMP_STREAM_TYPE.CommentStreamW;
				dEBUG_READ_USER_MINIDUMP_STREAM.Flags = 0u;
				dEBUG_READ_USER_MINIDUMP_STREAM.Offset = 0uL;
				dEBUG_READ_USER_MINIDUMP_STREAM.Buffer = zero;
				dEBUG_READ_USER_MINIDUMP_STREAM.BufferSize = 1048576u;
				dEBUG_READ_USER_MINIDUMP_STREAM.BufferUsed = 0u;
				bool flag = false;
				if (Utils.SUCCEEDED(DebugAdvanced2.Request(DebugDiag.DbgEng.DEBUG_REQUEST.READ_USER_MINIDUMP_STREAM, &dEBUG_READ_USER_MINIDUMP_STREAM, sizeof(Microsoft.Mex.DotNetDbg.DEBUG_READ_USER_MINIDUMP_STREAM), null, 0, null)))
				{
					text += "Comment: '";
					text += Marshal.PtrToStringUni(zero, (int)dEBUG_READ_USER_MINIDUMP_STREAM.BufferUsed / 2);
					int num = text.IndexOf('\0');
					if (num > 0)
					{
						text = text.Remove(num);
					}
					text = text.TrimEnd(default(char));
					text += "'";
					flag = true;
				}
				dEBUG_READ_USER_MINIDUMP_STREAM.StreamType = Microsoft.Mex.DotNetDbg.MINIDUMP_STREAM_TYPE.CommentStreamA;
				if (Utils.SUCCEEDED(DebugAdvanced2.Request(DebugDiag.DbgEng.DEBUG_REQUEST.READ_USER_MINIDUMP_STREAM, &dEBUG_READ_USER_MINIDUMP_STREAM, sizeof(Microsoft.Mex.DotNetDbg.DEBUG_READ_USER_MINIDUMP_STREAM), null, 0, null)))
				{
					if (flag)
					{
						text += "\n";
					}
					text += "Comment: '";
					text += Marshal.PtrToStringAnsi(zero, (int)dEBUG_READ_USER_MINIDUMP_STREAM.BufferUsed);
					int num2 = text.IndexOf('\0');
					if (num2 > 0)
					{
						text = text.Remove(num2);
					}
					text = text.TrimEnd(default(char));
					text += "'";
				}
			}
			catch
			{
			}
			finally
			{
				if (zero != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(zero);
					zero = IntPtr.Zero;
				}
			}
			return text;
		}
	}

	/// <summary>
	/// This property returns the system uptime after last reboot. 
	/// </summary>
	public double SystemUpTime => _legacyDebugger.SystemUpTime;

	/// <summary>
	/// This property returns the name of the process for this debugger object.
	/// </summary>
	public string ExecutableName => _legacyDebugger.ExecutableName;

	/// <summary>
	/// This property is primarily used by the extensions to provide interactive feedback. 
	/// For Example, this is the last line displayed below progress bar when the tool is analyzing the data.
	/// </summary>
	public string DebuggerStatus
	{
		set
		{
			_legacyDebugger.DebuggerStatus = value;
		}
	}

	/// <summary>
	/// This property controls how often the OnIdleProcessing event fires.
	/// </summary>
	public int IdleProcessingInterval
	{
		get
		{
			return _legacyDebugger.IdleProcessingInterval;
		}
		set
		{
			_legacyDebugger.IdleProcessingInterval = value;
		}
	}

	/// <summary>
	/// This property returns the version of the service pack for the operating system. 
	/// </summary>
	public string OSServicePack => _legacyDebugger.OSServicePack;

	/// <summary>
	/// This property returns the build number for the operating system. 
	/// </summary>
	public string OSBuild => _legacyDebugger.OSBuild;

	/// <summary>
	/// This property returns the version number of the operating system.
	/// </summary>
	public string OSVersion => _legacyDebugger.OSVersion;

	/// <summary>
	/// This property returns an instance of the DbgState object.
	/// </summary>
	public IDbgState State => _legacyDebugger.State;

	/// <summary>
	/// This property accepts the location of the dump file. This property is only used for live debugging sessions. 
	/// It provides a way for scripts to set the location where dumps should be created.
	/// <value>Path where teh dump should be stored.</value>
	/// </summary>
	public string DumpPath
	{
		get
		{
			return _legacyDebugger.DumpPath;
		}
		set
		{
			_legacyDebugger.DumpPath = value;
		}
	}

	/// <summary>
	/// This property returns True if the dump file has the CLR loaded and an appropriate sos.dll or psscor.dll extension could not be found, otherwise it returns False. 
	/// </summary>
	public bool IsClrExtensionExtensionMissing => _legacyDebugger.IsClrExtensionMissing;

	/// <summary>
	/// This method will return a reference object of the COM object that implements the IDebugClient and represents the debugger.
	/// </summary>
	public object RawDebugger
	{
		get
		{
			if (_rawDebugger == null)
			{
				IDbgObj4 legacyDebugger = _legacyDebugger;
				_rawDebugger = (DebugDiag.DbgEng.IDebugClient)legacyDebugger.RawDebugger;
			}
			return _rawDebugger;
		}
	}

	/// <summary>
	/// This property returns the number of milliseconds the process had been running.
	/// </summary>
	public double DumpTickCount => _legacyDebugger.DumpTickCount;

	public IList<DebugDiag.DotNet.WinDE.DumpInfo> MexDumpInfo { get; private set; }

	private void InitMex(IDbgObj4 legacyDebugger)
	{
		Microsoft.Mex.DotNetDbg.IDebugClient debugClient = null;
		debugClient = (Microsoft.Mex.DotNetDbg.IDebugClient)legacyDebugger.RawDebugger;
		MexDebugUtilities = new DebugUtilities(debugClient)
		{
			IsFirstCommand = true
		};
		MexFrameworkClass.Initialize(MexDebugUtilities, null);
	}

	public void SetContextFromTrapFrame()
	{
		string text = Execute("kv").ToLower();
		int num = text.ToLower().IndexOf("(trapframe @ ");
		if (num > -1)
		{
			int num2 = text.IndexOf(')', num);
			if (num2 > -1)
			{
				string text2 = text.Substring(num + 12, num2 - num - 12);
				Execute(".trap " + text2);
			}
		}
	}

	/// <summary>
	/// This method returns an instance of a <see cref="T:DebugDiag.DotNet.NetDbgThread" /> object based on the system thread ID passed into the function.
	/// </summary>
	/// <param name="SystemID">Integer value representing the Thread ID</param>
	/// <returns>Instance of a <see cref="T:DebugDiag.DotNet.NetDbgThread" /> object</returns>
	public NetDbgThread GetThreadBySystemID(int SystemID)
	{
		return new NetDbgThread(_legacyDebugger.GetThreadBySystemID(SystemID), this);
	}

	/// <summary>
	/// Use this mehtod to obtain an instance of a <see cref="T:DebugDiag.DotNet.NetDbgException" /> object. 
	/// </summary>
	/// <param name="dblAddress">The Double value passed into the method is the address of the exception record. </param>
	/// <returns>Returns an instance of a <see cref="T:DebugDiag.DotNet.NetDbgException" /> object.</returns>
	public NetDbgException GetExceptionObjectFromAddress(double dblAddress)
	{
		IDbgException exceptionObjectFromAddress = _legacyDebugger.GetExceptionObjectFromAddress(dblAddress);
		if (exceptionObjectFromAddress != null)
		{
			return new NetDbgException(exceptionObjectFromAddress, this);
		}
		return null;
	}

	private static LibraryModule LoadLibrary(string fileName)
	{
		string directoryName = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
		directoryName = Path.Combine(directoryName, fileName);
		if (File.Exists(directoryName))
		{
			try
			{
				return LibraryModule.LoadModule(directoryName);
			}
			catch (Exception)
			{
			}
		}
		string currentDirectory = Environment.CurrentDirectory;
		currentDirectory = Path.Combine(currentDirectory, fileName);
		if (File.Exists(currentDirectory))
		{
			return LibraryModule.LoadModule(currentDirectory);
		}
		throw new Exception($"Module not found: {fileName}. Search paths:\r\n\t1. {currentDirectory}\r\n\t2. {directoryName}");
	}

	/// <summary>
	/// This method creates a NetDbgObj that represents an instance of the debugger using a Dumpfile and optionaly other paramaters
	/// </summary>
	/// <param name="dumpPath">Required, dump file name including its path</param>
	/// <param name="symbolPath">Optional, symbols path, the default value is null</param>
	/// <param name="imagePath">Optional, compiled modules path, the default vlue is null</param>
	/// <param name="pProgress">Optional, Object that will show the progress of the executed analysis, the default value is null</param>
	/// <param name="throwOnBitnessMismatch">Optional, the default value is false</param>
	/// <param name="loadClrRuntime">Optional, pass true to load CLR runtime information, the default value is true</param>
	/// <param name="loadClrRuntime">Optional, pass true to load CLR heap information, the default value is true</param>
	/// <returns>A Reference to the <c>NetDbgObj</c> that represents the debugger used to open the dump file.</returns>
	public static NetDbgObj OpenDump(string dumpPath, string symbolPath = null, string imagePath = null, object pProgress = null, bool throwOnBitnessMismatch = false, bool loadClrRuntime = true, bool loadClrHeap = true)
	{
		DbgControl_Legacy dbgControl_Legacy = null;
		if (string.IsNullOrEmpty(symbolPath))
		{
			symbolPath = string.Empty;
		}
		if (string.IsNullOrEmpty(imagePath))
		{
			imagePath = string.Empty;
		}
		try
		{
			dbgControl_Legacy = CreateNewLegacyControl();
			IDbgObj4 legacyDebugger = dbgControl_Legacy.OpenDump(dumpPath, symbolPath, imagePath, pProgress);
			return new NetDbgObj(dumpPath, legacyDebugger, symbolPath, dumpPath, throwOnBitnessMismatch, loadClrRuntime, loadClrHeap);
		}
		finally
		{
			if (dbgControl_Legacy != null)
			{
				Marshal.FinalReleaseComObject(dbgControl_Legacy);
			}
		}
	}

	public static DbgControl_Legacy CreateNewLegacyControl()
	{
		DbgControl_Legacy result = null;
		if (IsRegistered)
		{
			result = new DbgControlClass_Legacy();
		}
		else if (DbglibModule != null)
		{
			result = (DbgControl_Legacy)ComHelper.CreateInstance(DbglibModule, new Guid("4001052F-9B7B-46A6-AD4B-C6984222BE62"));
		}
		return result;
	}

	private NetDbgObj(string dumpPath, IDbgObj4 legacyDebugger, string symbolPath, string dumpFileFullPath, bool throwOnBitnessMismatch = true, bool loadClrRuntime = true, bool loadClrHeap = true, bool loadMex = true)
	{
		try
		{
			ClrRuntimes = new List<ClrRuntime>();
			_dumpFileFullPath = dumpFileFullPath;
			_legacyDebugger = legacyDebugger;
			if (loadMex)
			{
				InitMex(legacyDebugger);
			}
			if (loadClrRuntime)
			{
				CreateRuntimeAndGetHeap(dumpPath, legacyDebugger, symbolPath, throwOnBitnessMismatch, loadClrHeap);
			}
			EnableCaching = true;
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	private static bool Is32BitLegacy(IDbgObj4 legacyDebugger)
	{
		IDbgModule dbgModule = null;
		try
		{
			dbgModule = legacyDebugger.GetModuleByModuleName("WOW64");
			if (dbgModule != null)
			{
				return true;
			}
			return legacyDebugger.Execute(".effmach").ToUpper().Contains("X86");
		}
		finally
		{
			if (dbgModule != null)
			{
				Marshal.FinalReleaseComObject(dbgModule);
			}
		}
	}

	/// <summary>
	/// This property returns a IEnumerable collection of <c>ClrObject</c> objects, containing all the managed objects found on the managed heap.
	/// </summary>
	/// <returns>Collection of ClrObject found on the heap</returns>
	public IEnumerable<ClrObject> EnumerateHeapObjects()
	{
		return EnumerateHeapObjects(null);
	}

	/// <summary>
	/// This property returns a IEnumerable collection of <c>ClrException</c> objects, containing all the managed Exceptions found on the managed heap.
	/// </summary>
	/// <returns>Collection of ClrException found on the heap</returns>
	public IEnumerable<ClrException> EnumerateHeapExceptionObjects()
	{
		if (ClrHeap == null)
		{
			yield break;
		}
		foreach (ClrObject item in EnumerateHeapObjects())
		{
			ClrType heapType = item.GetHeapType();
			if (heapType != null && heapType.IsException)
			{
				yield return ClrHeap.GetExceptionObject(item.GetValue());
			}
		}
	}

	/// <summary>
	/// This property returns a IEnumerable collection of <c>ClrObject</c> objects found on the managed heap, filtered by the value specified on typeName parameter.
	/// </summary>
	/// <param name="typeName">Name of the Type you want to look for on the heap</param>
	/// <returns>Collection of ClrObject found on the heap matching the type</returns>
	public IEnumerable<ClrObject> EnumerateHeapObjects(string typeName)
	{
		foreach (ulong item in EnumerateHeap(typeName))
		{
			yield return new ClrObject(ClrHeap, null, item);
		}
	}

	/// <summary>
	/// This function returns a collection of uLong type repressenting the address of the objects found on the heap of the type specified on the parameter 
	/// </summary>
	/// <param name="typeName">Name of the type you are looking on the heap</param>
	/// <returns>IEnumerable collection of uLong values with the addresses for the objects found on the heap</returns>
	public IEnumerable<ulong> EnumerateHeap(string typeName)
	{
		if (ClrHeap == null || !ClrHeap.CanWalkHeap)
		{
			yield break;
		}
		InitHeapCache();
		if (_heapCache != null)
		{
			foreach (ulong heapObjAddr in _heapCache.Keys)
			{
				if (string.IsNullOrEmpty(typeName))
				{
					yield return heapObjAddr;
				}
				if (_heapCache[heapObjAddr] == typeName)
				{
					yield return heapObjAddr;
				}
			}
			yield break;
		}
		foreach (ulong item in ClrHeap.EnumerateObjectAddresses())
		{
			ClrType objectType = ClrHeap.GetObjectType(item);
			if (objectType != null && (string.IsNullOrEmpty(typeName) || objectType.Name == typeName))
			{
				yield return item;
			}
		}
	}

	private void InitHeapCache()
	{
	}

	/// <summary>
	/// This method returns the first object that is found on the heap that matches the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Type of the object you are looking for on the heap</param>
	/// <returns>CLrObject representing the first object found on the heap that matches the type specified</returns>
	public ClrObject FindFirstHeapObject(string typeName)
	{
		using (IEnumerator<ClrObject> enumerator = EnumerateHeapObjects(typeName).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	private static bool IsBitnessMismatch(IDbgObj4 legacyDebugger, out bool debuggerHostIs32bit, out bool targetIs32bit)
	{
		debuggerHostIs32bit = !Environment.Is64BitProcess;
		targetIs32bit = Is32BitLegacy(legacyDebugger);
		return debuggerHostIs32bit != targetIs32bit;
	}

	private static bool IsBitnessMismatch(IDbgObj4 legacyDebugger)
	{
		bool debuggerHostIs32bit = false;
		bool targetIs32bit = false;
		return IsBitnessMismatch(legacyDebugger, out debuggerHostIs32bit, out targetIs32bit);
	}

	private void CreateRuntimeAndGetHeap(string dumpPath, IDbgObj4 legacyDebugger, string symbolPath, bool throwOnBitnessMismatch, bool loadClrHeap)
	{
		bool debuggerHostIs32bit = false;
		bool targetIs32bit = false;
		IDbgModule dbgModule = null;
		if (((dbgModule = legacyDebugger.GetModuleByModuleName("CLR")) != null || (dbgModule = legacyDebugger.GetModuleByModuleName("MSCORWKS")) != null || (dbgModule = legacyDebugger.GetModuleByModuleName("MSCORSVR")) != null || (dbgModule = legacyDebugger.GetModuleByModuleName("CORECLR")) != null) && throwOnBitnessMismatch && IsBitnessMismatch(legacyDebugger, out debuggerHostIs32bit, out targetIs32bit))
		{
			throw new BitnessMismatchException(string.Format("The debugger bitness ({0}) doesn't match the target bitness ({1}).  This is required when the target process has the .NET runtime loaded.", debuggerHostIs32bit ? "32 bit" : "64 bit", targetIs32bit ? "32 bit" : "64 bit"));
		}
		if (dbgModule == null)
		{
			return;
		}
		_rawDebugger = (DebugDiag.DbgEng.IDebugClient)legacyDebugger.RawDebugger;
		DataTarget dataTarget = DataTarget.CreateFromDebuggerInterface((Microsoft.Diagnostics.Runtime.Interop.IDebugClient)_rawDebugger);
		if (dataTarget.ClrVersions == null || dataTarget.ClrVersions.Count == 0)
		{
			return;
		}
		if (dataTarget.ClrVersions.Count > 0)
		{
			for (int i = 0; i < dataTarget.ClrVersions.Count; i++)
			{
				bool flag = true;
				ClrRuntime clrRuntime = null;
				try
				{
					clrRuntime = CreateRuntime(symbolPath, dataTarget, i, out var clrInfo);
					if (clrInfo != null)
					{
						IsManaged = true;
					}
				}
				catch (ClrDiagnosticsException ex)
				{
					if (ClrInitException == null || ex.HResult != -2128281599)
					{
						ClrInitException = ex;
					}
				}
				catch (Exception clrInitException)
				{
					ClrInitException = clrInitException;
				}
				if (clrRuntime == null || clrRuntime.Threads == null)
				{
					flag = false;
				}
				if (clrRuntime != null)
				{
					ClrRuntimes.Add(clrRuntime);
				}
				if (flag && ClrRuntime != null && ClrRuntime.Threads != null)
				{
					if (clrRuntime.Threads.Count > 0 && ClrRuntime.Threads.Count > 0)
					{
						HasMultipleClrRuntimesWithThreads = true;
					}
					if (clrRuntime.Threads.Count < ClrRuntime.Threads.Count)
					{
						flag = false;
					}
					else if (clrRuntime.Threads.Count == ClrRuntime.Threads.Count && ClrVersionInfo != null)
					{
						if (dataTarget.ClrVersions[i].Version.Major < ClrVersionInfo.Version.Major)
						{
							flag = false;
						}
						if (dataTarget.ClrVersions[i].Version.Major == ClrVersionInfo.Version.Major && dataTarget.ClrVersions[i].Version.Minor < ClrVersionInfo.Version.Minor)
						{
							flag = false;
						}
					}
				}
				if (flag)
				{
					ClrRuntime = clrRuntime;
					ClrVersionInfo = dataTarget.ClrVersions[i];
				}
			}
			if (ClrRuntime != null)
			{
				ClrInitException = null;
			}
		}
		if (ClrRuntime != null && loadClrHeap)
		{
			ClrHeap = ClrRuntime.Heap;
		}
		if (ClrHeap == null && loadClrHeap)
		{
			ClrRuntime = null;
		}
	}

	private ClrRuntime CreateRuntime(string symbolPath, DataTarget target, int runtimeIndex, out ClrInfo clrInfo)
	{
		ClrInfo clrInfo2 = target.ClrVersions[runtimeIndex];
		string text = clrInfo2.LocalMatchingDac;
		clrInfo = clrInfo2;
		if (!string.IsNullOrEmpty(symbolPath) && !File.Exists(text))
		{
			string noisySymbolInfo = null;
			text = GetDacFromSymbolServer(clrInfo2.DacInfo.FileName, (int)clrInfo2.DacInfo.TimeStamp, (int)clrInfo2.DacInfo.FileSize, symbolPath, ref noisySymbolInfo);
			if (!File.Exists(text))
			{
				ClrInitException = new DacNotFoundException(clrInfo2.DacInfo.FileName, text, noisySymbolInfo);
				Trace.WriteLine(ClrInitException.Message);
			}
		}
		if (File.Exists(text))
		{
			DacFileLocation = text;
			ClrRuntime clrRuntime = clrInfo.CreateRuntime(text);
			if (clrRuntime != null && dacLibrary == null)
			{
				FieldInfo field = clrRuntime.GetType().BaseType.BaseType.GetField("_library", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
				if (field != null)
				{
					dacLibrary = field.GetValue(clrRuntime);
				}
			}
		}
		if (File.Exists(text))
		{
			return clrInfo.CreateRuntime(text);
		}
		return null;
	}

	private static string GetDacFromSymbolServer(string dacRequestFileName, int timeStamp, int fileSize, string symbolPath)
	{
		string noisySymbolInfo = string.Empty;
		return GetDacFromSymbolServer(dacRequestFileName, timeStamp, fileSize, symbolPath, ref noisySymbolInfo);
	}

	private static string GetDacFromSymbolServer(string dacRequestFileName, int timeStamp, int fileSize, string symbolPath, ref string noisySymbolInfo)
	{
		for (int i = 0; i < 3; i++)
		{
			try
			{
				StringWriter stringWriter = new StringWriter();
				string result = new SymbolReader(stringWriter, symbolPath).FindExecutableFilePath(dacRequestFileName, timeStamp, fileSize);
				if (noisySymbolInfo == null)
				{
					noisySymbolInfo = stringWriter.ToString();
				}
				return result;
			}
			catch
			{
				Thread.Sleep(i * 500);
			}
		}
		return "";
	}

	/// <summary>
	/// Disposes the object and release all native resources.
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
			throw new ObjectDisposedException(GetType().Name);
		}
		if (_legacyDebugger != null)
		{
			UnloadExtensions();
			Marshal.FinalReleaseComObject(_legacyDebugger);
		}
		if (_rawDebugger != null)
		{
			Marshal.FinalReleaseComObject(_rawDebugger);
		}
		_legacyDebugger = null;
		_disposed = true;
	}

	internal void PrependExtPath(string path)
	{
		Execute(".extpath " + path + ";" + GetExtensionPath());
	}

	/// <summary>
	///     Gets the extension path in use by the debugger.
	///     If an error occurs the return value is an empty string.
	/// </summary>
	public unsafe string GetExtensionPath()
	{
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			int num = default(int);
			if (Utils.FAILED(DebugAdvanced2.Request(DebugDiag.DbgEng.DEBUG_REQUEST.GET_EXTENSION_SEARCH_PATH_WIDE, null, 0, null, 0, &num)))
			{
				return string.Empty;
			}
			num += 16;
			intPtr = Marshal.AllocHGlobal(num);
			if (Utils.FAILED(DebugAdvanced2.Request(DebugDiag.DbgEng.DEBUG_REQUEST.GET_EXTENSION_SEARCH_PATH_WIDE, null, 0, intPtr.ToPointer(), num, &num)))
			{
				return string.Empty;
			}
			return Marshal.PtrToStringUni(intPtr);
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
				intPtr = IntPtr.Zero;
			}
		}
	}

	internal void AddManagedIP(NetDbgStackFrame netDbgStackFrame)
	{
		AddCacheableValue("GetSymbolFromAddress", netDbgStackFrame.InstructionAddress, netDbgStackFrame.GetFrameText(includeOffset: true, includeSourceInfo: false));
	}

	private void AddCacheableValue(string cachedMethodOrPropName, object key, object value)
	{
		GetDictionary(cachedMethodOrPropName)[key] = value;
	}

	private object GetCacheableValue(string cachedMethodOrPropName, Func<object> GetValue, object key)
	{
		object value = null;
		Dictionary<object, object> dictionary = GetDictionary(cachedMethodOrPropName);
		if (dictionary != null && dictionary.TryGetValue(key, out value))
		{
			return value;
		}
		value = GetValue();
		if (dictionary != null)
		{
			dictionary[key] = value;
		}
		return value;
	}

	private Dictionary<object, object> GetDictionary(string cachedMethodOrPropName)
	{
		Dictionary<object, object> dictionary = null;
		object value = null;
		if (EnableCaching)
		{
			if (_methodOrPropCaches.TryGetValue(cachedMethodOrPropName, out value))
			{
				dictionary = (Dictionary<object, object>)value;
			}
			else
			{
				dictionary = new Dictionary<object, object>();
				_methodOrPropCaches[cachedMethodOrPropName] = dictionary;
			}
		}
		return dictionary;
	}

	/// <summary>
	/// Returns the symbol information for the address passed into the function.
	/// <example>This example shows how to get the symbols infromation for a given address:
	/// <code language="cs">
	/// // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSymbolFromAddress(new UIntPtr(0x79222b54UL));
	///
	///     //Writes the results on the report
	///     manager.Write(strResults);
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// w3svc!HTTP_REQUEST::`vftable'
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">UintPtr with the address value used for querying the symbol</param>
	/// <returns>String with symbols information.</returns>
	public string GetSymbolFromAddress(UIntPtr Address)
	{
		return GetSymbolFromAddress((double)(ulong)Address);
	}

	/// <summary>
	/// Returns the symbol information for the address passed into the function.
	/// <example>This example shows how to get the symbols infromation for a given address:
	/// <code language="cs">
	/// // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSymbolFromAddress(0x79222b54UL);
	///     manager.Write(strResults);
	///
	///     //Release debugger native objects
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// w3svc!HTTP_REQUEST::`vftable'
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">Uint64 with the address value used for querying the symbol</param>
	/// <returns>String with symbols information if found.</returns>
	public string GetSymbolFromAddress(ulong Address)
	{
		return GetSymbolFromAddress((double)Address);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool IsWow64Process([In] IntPtr processHandle, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

	[DllImport("kernel32", SetLastError = true)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static extern bool CloseHandle(IntPtr handle);

	internal static DumpFileType GetTargetProcessType(int targetProcessId)
	{
		bool flag = false;
		bool flag2 = false;
		IEnumerator enumerator = Process.GetProcessById(targetProcessId).Modules.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				switch (((ProcessModule)enumerator.Current).ModuleName)
				{
				case "wow64.dll":
					flag = true;
					break;
				case "clr.dll":
					flag2 = true;
					break;
				case "mscorwks.dll":
					flag2 = true;
					break;
				case "mscorsvr.dll":
					flag2 = true;
					break;
				case "coreclr.dll":
					flag2 = true;
					break;
				}
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
		if (flag)
		{
			if (flag2)
			{
				return DumpFileType._32bitWithClr;
			}
			return DumpFileType._32bitWithoutClr;
		}
		if (flag2)
		{
			return DumpFileType._64bitWithClr;
		}
		return DumpFileType._64bitWithoutClr;
	}

	internal static DumpFileType GetDumpFileType(string dumpFile, string symbolPath = null)
	{
		using NetDbgObj netDbgObj = OpenDump(dumpFile, symbolPath, null, null, throwOnBitnessMismatch: false, loadClrRuntime: true, loadClrHeap: false);
		if (netDbgObj.Is32Bit)
		{
			if (!netDbgObj.IsManaged)
			{
				return DumpFileType._32bitWithoutClr;
			}
			return DumpFileType._32bitWithClr;
		}
		if (!netDbgObj.IsManaged)
		{
			return DumpFileType._64bitWithoutClr;
		}
		return DumpFileType._64bitWithClr;
	}

	/// <summary>
	/// This method returns an instance of a <see cref="T:DebugDiag.DotNet.NetDbgThread" /> object based on the system managed thread ID passed into the function. 
	/// </summary>
	/// <param name="managedThreadId">Integer value representing the ID of the Managed Thread.</param>
	/// <returns>Instance of <see cref="T:DebugDiag.DotNet.NetDbgThread" /> object.</returns>
	public NetDbgThread GetThreadByManagedThreadId(int managedThreadId)
	{
		foreach (NetDbgThread thread in Threads)
		{
			if (thread.ManagedThread != null && thread.ManagedThread.ManagedThreadId == managedThreadId)
			{
				return thread;
			}
		}
		return null;
	}

	internal void SetContextFromExceptionRecord()
	{
		DebugDiag.DbgEng.IDebugSymbols3 debugSymbols = null;
		if (IsCrashDump)
		{
			if (RawDebugger is DebugDiag.DbgEng.IDebugSymbols3 debugSymbols2)
			{
				debugSymbols2.SetScopeFromStoredEvent();
			}
			else
			{
				Execute(".ecxr");
			}
		}
	}

	internal IDbgModule GetModuleByAddress(double Address, out string moduleName)
	{
		moduleName = string.Empty;
		IDbgModule moduleByAddress = GetModuleByAddress(Address);
		if (moduleByAddress != null)
		{
			moduleName = moduleByAddress.ModuleName;
			return moduleByAddress;
		}
		string text = Execute("ln 0n" + Address);
		int num = text.IndexOf('!');
		if (num > -1)
		{
			int num2 = text.Substring(0, num).LastIndexOf(' ');
			if (num2 > -1)
			{
				moduleName = text.Substring(num2 + 1, num - num2 - 1);
				return GetModuleByModuleName(moduleName);
			}
		}
		return null;
	}

	/// <summary>
	/// This method walks pointers in the specified address range and returns all the strings which begin with the specified value
	/// </summary>
	/// <param name="searchStringContentsStart">The beginning value of the string to be found</param>
	/// <param name="caseSensitive"></param>
	/// <param name="startAddress">The beginning of the address range to be searched</param>
	/// <param name="endAddress">The end of the address range to be searched</param>
	/// <returns>List of strings with pointers in the specified address range which begin with the specified value</returns>
	public List<string> StringSearch(string searchStringContentsStart, ulong startAddress, ulong endAddress, bool caseSensitive = true)
	{
		if (endAddress <= startAddress)
		{
			throw new ArgumentException("endAddress must be greater than startAddress");
		}
		if (string.IsNullOrEmpty(searchStringContentsStart))
		{
			throw new ArgumentNullException("searchStringContentsStart");
		}
		if (searchStringContentsStart.Length < 2)
		{
			throw new ArgumentException("searchStringContentsStart must be at least 2 characters");
		}
		try
		{
			List<string> list = new List<string>();
			int num = 0;
			bool is32Bit = Is32Bit;
			uint num2 = (is32Bit ? 4u : 8u);
			ulong num3 = (endAddress - startAddress) / num2;
			int num4 = (is32Bit ? 9 : 27);
			int length = (is32Bit ? 8 : 17);
			string bstrCommand = $"dpp {startAddress:X} L?{num3:X}";
			string text = Execute(bstrCommand);
			while (num >= 0)
			{
				string value = $"00{(int)searchStringContentsStart[1]:X}00{(int)searchStringContentsStart[0]:X}";
				num = text.IndexOf(value, num + 1, StringComparison.Ordinal);
				if (num < 0)
				{
					break;
				}
				if (ulong.TryParse(text.Substring(num - num4, length).Replace("`", ""), NumberStyles.HexNumber, null, out var result) && ReadUnicodeString(result).StartsWith(searchStringContentsStart, (!caseSensitive) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture))
				{
					list.Add(ReadUnicodeString(result));
				}
			}
			if (list.Count > 0)
			{
				return list;
			}
		}
		catch (Exception)
		{
		}
		return null;
	}

	public ThreadInfo GetKernelFaultThread(out Microsoft.Mex.Framework.Stack stack)
	{
		stack = null;
		ThreadInfo threadInfo = GetKernelFaultThread();
		if (threadInfo == null)
		{
			return null;
		}
		stack = threadInfo.StackCorrected;
		string text = Execute("kv").ToLower();
		int num = text.ToLower().IndexOf("(trapframe @ ");
		if (num > -1)
		{
			int num2 = text.IndexOf(')', num);
			if (num2 > -1)
			{
				string text2 = text.Substring(num + 12, num2 - num - 12);
				_ = threadInfo.EThreadAddress;
				Execute(".trap " + text2);
				stack = new Microsoft.Mex.Framework.Stack(mexDebugUtilities).GetWow64Stack();
			}
		}
		return threadInfo;
	}

	public ThreadInfo GetKernelFaultThread()
	{
		if (kernelFaultThread != null)
		{
			return kernelFaultThread;
		}
		foreach (ThreadInfo thread in new Threads(mexDebugUtilities, ThreadOptions.RunningOnly | ThreadOptions.PrePopulate).GetThreads())
		{
			Microsoft.Mex.Framework.StackFrame stackFrame = thread.StackCorrected.StackFramesList.FirstOrDefault();
			if (stackFrame != null && stackFrame.SymbolNameWithoutOffset == "nt!KeBugCheckEx")
			{
				kernelFaultThread = thread;
				return kernelFaultThread;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns the symbol information for the address passed into the function.
	/// <example>This example shows how to get the symbols infromation for a given address:
	/// <code language="cs">
	/// // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSymbolFromAddress((double)0x79222b54UL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native objects
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// w3svc!HTTP_REQUEST::`vftable'
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">Double with the address value used for querying the symbol</param>
	/// <returns>String with symbols information if found.</returns>
	public string GetSymbolFromAddress(double Address)
	{
		return (string)GetCacheableValue("GetSymbolFromAddress", delegate
		{
			if (_legacyDebugger.GetModuleByAddress(Address) != null)
			{
				return _legacyDebugger.GetSymbolFromAddress(Address);
			}
			if (ClrRuntime != null)
			{
				ClrMethod clrMethod = null;
				try
				{
					clrMethod = ClrRuntime.GetMethodByAddress((ulong)Address);
				}
				catch (Exception)
				{
				}
				if (clrMethod != null)
				{
					return GetSignatureFromClrMethod(clrMethod);
				}
			}
			return _legacyDebugger.GetSymbolFromAddress(Address);
		}, Address);
	}

	internal static string GetSignatureFromClrMethod(ClrMethod method)
	{
		string text = method.ToString();
		if (text.StartsWith("<"))
		{
			string text2 = text.Replace(" ", "");
			int num = text2.IndexOf("signature=", StringComparison.CurrentCultureIgnoreCase);
			if (num > -1)
			{
				int num2 = text2.IndexOf("'", num + 1) + 1;
				if (num2 > 0)
				{
					int num3 = text2.IndexOf("'", num2 + 1) - 1;
					text = text2.Substring(num2, num3 - num2 + 1).Replace(",", ", ");
				}
			}
		}
		return text;
	}

	internal static string GetDisplayTextFromClrStackFrame(ClrStackFrame internalFrame)
	{
		string text = $"[{internalFrame.ToString()}]";
		if (internalFrame.Method != null)
		{
			text = $"{text} {GetSignatureFromClrMethod(internalFrame.Method)}";
		}
		return text;
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string, up to the specified max length.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.ReadUnicodeString((double)0x010caf8fUL, 1024);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <param name="maxLength">Maximum number of characters to read</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(double Address, int maxLength)
	{
		Read.ReadUnicodeStringVirtualWide(this, (ulong)Address, out var value, (uint)maxLength);
		return value;
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string, up to the specified max length.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.ReadUnicodeString(0x010caf8fUL, 1024);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <param name="maxLength">Maximum number of characters to read</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(ulong Address, int maxLength)
	{
		return ReadUnicodeString((double)Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string, up to the specified max length.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.ReadUnicodeString(new IntPtr(0x010caf8fUL), 1024);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <param name="maxLength">Maximum number of characters to read</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(IntPtr Address, int maxLength)
	{
		return ReadUnicodeString((long)Address);
	}

	/// <summary>
	/// This method executes a debugger command and returns the output of that command.
	/// <example>This example shows how to evaluate an expression on the debugger:</example>
	/// <code language="cs">
	/// // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.Execute("? 8+2");
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Evaluate expression: 10 = 0000000a</code>
	/// </summary>
	/// <param name="bstrCommand">Command to execute on the debugger for example: kc</param>
	/// <returns>output string generated by the debugger when running the command specified on the parameter bstrCommand</returns>
	public string Execute(string bstrCommand)
	{
		return _legacyDebugger.Execute(bstrCommand);
	}

	/// <summary>
	/// This method undecorates decorated C++ symbol names.  The method requires a Pointer to a null-terminated string that specifies a decorated C++ symbol name. 
	/// This name can be identified by the first character of the name, which is always a question mark (?). 
	/// </summary>
	/// <param name="DecoratedSym">String representing the decorated symbol</param>
	/// <returns>String representing the undecorated value of the symbol</returns>
	public string UnDecorateSymbol(string DecoratedSym)
	{
		return _legacyDebugger.UnDecorateSymbol(DecoratedSym);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string and return that string.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadANSIString((double)0x010ced71UL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadANSIString(double Address)
	{
		return _legacyDebugger.ReadANSIString(Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string and return that string.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadANSIString(0x010ced71UL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadANSIString(ulong Address)
	{
		return ReadANSIString((double)Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string and return that string.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadANSIString(new UIntPtr(0x010ced71UL));
	///     manager.Write(strResults);
	///
	///     //Release Debugger Native Resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadANSIString(UIntPtr Address)
	{
		return ReadANSIString((double)(ulong)Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadUnicodeString((double)0x010caf8fUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(double Address)
	{
		return _legacyDebugger.ReadUnicodeString(Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadUnicodeString(0x010caf8fUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(ulong Address)
	{
		return ReadUnicodeString((double)Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an UNICODE string and return that string.
	/// <example>This example shows how to use the ReadUnicodeString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadUnicodeString(new UIntPtr(0x010caf8fUL));
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Hello World!</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value where the string is located.</param>
	/// <returns>String value found on the address if any. If a string does not reside at that address the string returned is empty.</returns>
	public string ReadUnicodeString(UIntPtr Address)
	{
		return ReadUnicodeString((double)(ulong)Address);
	}

	/// <summary>
	/// This method returns a 32-Bit string representation of a Double value.
	/// <example>This example shows how to get the 32-Bit string representation of a Double value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs32BitHexString((double)0x010ed48cUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x010ed48c</code></example>
	/// </summary>
	/// <param name="Address">Double value that will be converted to string.</param>
	/// <returns>String representation</returns>
	public string GetAs32BitHexString(double Address)
	{
		return _legacyDebugger.GetAs32BitHexString(Address);
	}

	/// <summary>
	/// This method returns a 32-Bit string representation of a Uint64 value.
	/// <example>This example shows how to get the 32-Bit string representation of a Uint64 value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs32BitHexString(0x010ed48cUL);
	///     manager.Write(strResults);.
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x010ed48c</code></example>
	/// </summary>
	/// <param name="Address">Uint64 value that will be converted to string.</param>
	/// <returns>String Representation</returns>
	public string GetAs32BitHexString(ulong Address)
	{
		return GetAs32BitHexString((double)Address);
	}

	/// <summary>
	/// This method returns a 32-Bit string representation of a UIntPtr value.
	/// <example>This example shows how to get the 32-Bit string representation of a UIntPtr value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs32BitHexString(new UIntPtr(0x010ed48cUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x010ed48c</code></example>
	/// </summary>
	/// <param name="Address">UIntPtr value that will be converted to string.</param>
	/// <returns>String Representation</returns>
	public string GetAs32BitHexString(UIntPtr Address)
	{
		return GetAs32BitHexString((double)(ulong)Address);
	}

	/// <summary>
	/// This method returns a 64-Bit string representation of a double value.
	/// <example>This example shows how to get the 64-Bit string representation of a double value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs64BitHexString((double)0x010ed48cUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger Native Resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x00000000`010ed48c</code></example>
	/// </summary>
	/// <param name="Address">double value that will be converted to string.</param>
	/// <returns>String Representation</returns>
	public string GetAs64BitHexString(double Address)
	{
		return _legacyDebugger.GetAs64BitHexString(Address);
	}

	/// <summary>
	/// This method returns a 64-Bit string representation of a UInt64 value.
	/// <example>This example shows how to get the 64-Bit string representation of a UInt64 value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs64BitHexString(0x010ed48cUL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x00000000`010ed48c</code></example>
	/// </summary>
	/// <param name="Address">UInt64 value that will be converted to string.</param>
	/// <returns>String Representation</returns>
	public string GetAs64BitHexString(ulong Address)
	{
		return GetAs64BitHexString((double)Address);
	}

	/// <summary>
	/// This method returns a 64-Bit string representation of a UIntPtr value.
	/// <example>This example shows how to get the 64-Bit string representation of a UIntPtr value:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;     
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetAs64BitHexString(new UIntPtr(0x010ed48cUL));
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>0x00000000`010ed48c</code></example>
	/// </summary>
	/// <param name="Address">UIntPtr value that will be converted to string.</param>
	/// <returns>String Representation</returns>
	public string GetAs64BitHexString(UIntPtr Address)
	{
		return GetAs64BitHexString((double)(ulong)Address);
	}

	/// <summary>
	/// Returns an instance of a DbgModule object .  
	/// </summary>
	/// <param name="Address">This Double value passed into the method is the base address of the module.</param>
	/// <returns>Reference to a COM object that implements the <c>IDbgModule</c> interface</returns>
	public IDbgModule GetModuleByAddress(double Address)
	{
		return _legacyDebugger.GetModuleByAddress(Address);
	}

	/// <summary>
	/// Returns an instance of a DbgModule object .  
	/// </summary>
	/// <param name="Address">This UInt64 value passed into the method is the base address of the module.</param>
	/// <returns>Reference to a COM object that implements the <c>IDbgModule</c> interface</returns>
	public IDbgModule GetModuleByAddress(ulong Address)
	{
		return GetModuleByAddress((double)Address);
	}

	/// <summary>
	/// Returns an instance of a DbgModule object .  
	/// </summary>
	/// <param name="Address">This UIntPtr value passed into the method is the base address of the module.</param>
	/// <returns>Reference to a COM object that implements the <c>IDbgModule</c> interface</returns>
	public IDbgModule GetModuleByAddress(UIntPtr Address)
	{
		return GetModuleByAddress((double)(ulong)Address);
	}

	/// <summary>
	/// Returns an instance of a DbgModule object.
	/// </summary>
	/// <param name="ModuleName">The string passed into the method is the name of the module. </param>
	/// <returns>Reference to a COM object that implements the <c>IDbgModule</c> interface</returns>
	public IDbgModule GetModuleByModuleName(string ModuleName)
	{
		return _legacyDebugger.GetModuleByModuleName(ModuleName);
	}

	/// <summary>
	/// This method reads an 8-Bit value at the address that is passed into the function and returns it as an Integer value.
	/// </summary>
	/// <param name="Address">Address to read</param>
	/// <returns>8 Bit Value represented by a integer</returns>
	public int ReadByte(double Address)
	{
		return _legacyDebugger.ReadByte(Address);
	}

	/// <summary>
	/// This method reads an 8-Bit value at the address that is passed into the function and returns it as an Integer value.
	/// </summary>
	/// <param name="Address">Address to read</param>
	/// <returns>8 Bit Value represented by a integer</returns>
	public int ReadByte(ulong Address)
	{
		return ReadByte((double)Address);
	}

	/// <summary>
	/// This method reads an 8-Bit value at the address that is passed into the function and returns it as an Integer value.
	/// </summary>
	/// <param name="Address">Address to read</param>
	/// <returns>8 Bit Value represented by a integer</returns>
	public int ReadByte(UIntPtr Address)
	{
		return ReadByte((double)(ulong)Address);
	}

	/// <summary>
	/// This method reads 16-Bits of data at the address passed into the function and returns it as an Integer value. 
	/// </summary>
	/// <param name="Address">Address value where the 16 Bits of data is located</param>
	/// <returns>Integer value representing the 16 bits obtained on the address specified.</returns>
	public int ReadWord(double Address)
	{
		return _legacyDebugger.ReadWord(Address);
	}

	/// <summary>
	/// This method reads 16-Bits of data at the address passed into the function and returns it as an Integer value. 
	/// </summary>
	/// <param name="Address">Address value where the 16 Bits of data is located</param>
	/// <returns>Integer value representing the 16 bits obtained on the address specified.</returns>
	public int ReadWord(ulong Address)
	{
		return ReadWord((double)Address);
	}

	/// <summary>
	/// This method reads 16-Bits of data at the address passed into the function and returns it as an Integer value. 
	/// </summary>
	/// <param name="Address">Address value where the 16 Bits of data is located</param>
	/// <returns>Integer value representing the 16 bits obtained on the address specified.</returns>
	public int ReadWord(UIntPtr Address)
	{
		return ReadWord((double)(ulong)Address);
	}

	/// <summary>
	/// This method reads 32-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 32 bits will be read</param>
	/// <returns>Double number representing the 32 bits value</returns>
	public double ReadDWord(double Address)
	{
		return _legacyDebugger.ReadDWord(Address);
	}

	/// <summary>
	/// This method reads 32-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 32 bits will be read</param>
	/// <returns>Double number representing the 32 bits value</returns>
	public ulong ReadDWord(ulong Address)
	{
		return (ulong)ReadDWord((double)Address);
	}

	/// <summary>
	/// This method reads 32-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 32 bits will be read</param>
	/// <returns>Double number representing the 32 bits value</returns>
	public UIntPtr ReadDWord(UIntPtr Address)
	{
		return (UIntPtr)(ulong)ReadDWord((double)(ulong)Address);
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public double ReadQWord(double Address)
	{
		return _legacyDebugger.ReadQWord(Address);
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public ulong ReadQWord(ulong Address)
	{
		return (ulong)ReadQWord((double)Address);
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public UIntPtr ReadQWord(UIntPtr Address)
	{
		return (UIntPtr)(ulong)ReadQWord((double)(ulong)Address);
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public double ReadPointer(double Address)
	{
		if (Utils.SUCCEEDED(Read.ReadVirtual64(this, (ulong)Address, out var value)))
		{
			return GetPointerValue(value);
		}
		return 0.0;
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public ulong ReadPointer(ulong Address)
	{
		return (ulong)ReadPointer((double)Address);
	}

	/// <summary>
	/// This method reads 64-Bits of data at the address passed into the function and returns it as a Double value.
	/// </summary>
	/// <param name="Address">Address where the 64 bits will be read</param>
	/// <returns>Double number representing the 64 bits value</returns>
	public UIntPtr ReadPointer(UIntPtr Address)
	{
		return (UIntPtr)(ulong)ReadPointer((double)(ulong)Address);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string if the Boolean value passed in is False, or as a Unicode string if the Boolean value passed in is True.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadString((double)0x010ced71UL, false);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address Value where the string is located</param>
	/// <param name="bUnicode">True indicates to read a Unicode, False will attempt to get the string as ANSI</param>
	/// <returns>String Value if any string is found, It will return an empty string if it cannot find a string at the address passed in.</returns>
	public string ReadString(double Address, bool bUnicode)
	{
		return _legacyDebugger.ReadString(Address, bUnicode);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string if the Boolean value passed in is False, or as a Unicode string if the Boolean value passed in is True.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadString(0x010ced71UL, false);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address Value where the string is located</param>
	/// <param name="bUnicode">True indicates to read a Unicode, False will attempt to get the string as ANSI</param>
	/// <returns>String Value if any string is found, It will return an empty string if it cannot find a string at the address passed in.</returns>
	public string ReadString(ulong Address, bool bUnicode)
	{
		return ReadString((double)Address, bUnicode);
	}

	/// <summary>
	/// This method tries to read the address passed into the function as an ANSI string if the Boolean value passed in is False, or as a Unicode string if the Boolean value passed in is True.
	/// <example>This example shows how to use the ReadANSIString to get a string value from a dump file:
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger..ReadString(new UIntPtr(0x010ced71UL), false);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }
	/// </code>
	/// Results are:
	/// <code>Default.htm</code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address Value where the string is located</param>
	/// <param name="bUnicode">True indicates to read a Unicode, False will attempt to get the string as ANSI</param>
	/// <returns>String Value if any string is found, It will return an empty string if it cannot find a string at the address passed in.</returns>
	public string ReadString(UIntPtr Address, bool bUnicode)
	{
		return ReadString((double)(ulong)Address, bUnicode);
	}

	private string GetTargetCommandLine()
	{
		uint? fieldOffset = GetFieldOffset("ntdll!_PEB", "ProcessParameters");
		if (fieldOffset.HasValue)
		{
			uint? fieldOffset2 = GetFieldOffset("ntdll!_RTL_USER_PROCESS_PARAMETERS", "CommandLine");
			if (fieldOffset2.HasValue)
			{
				uint? fieldOffset3 = GetFieldOffset("ntdll!_UNICODE_STRING", "Buffer");
				if (fieldOffset3.HasValue)
				{
					string targetCommandLine = GetTargetCommandLine(fieldOffset.Value, fieldOffset2.Value, fieldOffset3.Value, guessing: false);
					if (!string.IsNullOrWhiteSpace(targetCommandLine))
					{
						return targetCommandLine;
					}
				}
			}
		}
		string text = Execute("dt @$peb ntdll!_PEB ProcessParameters");
		if (text != null)
		{
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
				if (!text.EndsWith("_RTL_USER_PROCESS_PARAMETERS\n"))
				{
					continue;
				}
				string text2 = GetAfter(text, ": ");
				if (string.IsNullOrEmpty(text2))
				{
					break;
				}
				if (text2[text2.Length - 1] == '\n')
				{
					text2 = text2.Substring(0, text2.Length - 1);
				}
				text = Execute($"dt {text2} CommandLine.");
				if (text == null)
				{
					break;
				}
				string[] array2 = text.Split('\n');
				string text3 = array2[array2.Length - 1];
				if (text3.Trim() == "")
				{
					text3 = array2[array2.Length - 2];
				}
				string text4 = null;
				if (text3.Contains(" Buffer "))
				{
					text3 = GetAfter(text3, "  \"");
					if (text3.EndsWith(" \""))
					{
						text4 = text3.Substring(0, text3.Length - 2);
					}
					else if (text3.EndsWith("\""))
					{
						text4 = text3.Substring(0, text3.Length - 1);
					}
				}
				if (string.IsNullOrEmpty(text4))
				{
					break;
				}
				string targetCommandLine = TrimMatchingQuotePair(text4);
				if (string.IsNullOrWhiteSpace(targetCommandLine))
				{
					break;
				}
				return targetCommandLine;
			}
		}
		if (Is32Bit && GetModuleByModuleName("WOW64") == null)
		{
			return GetTargetCommandLine(16u, 64u, 4u, guessing: true);
		}
		return GetTargetCommandLine(32u, 112u, 8u, guessing: true);
	}

	private string GetTargetCommandLine(uint processParametersOffset, uint commandLineOffset, uint bufferOffset, bool guessing)
	{
		ulong num = 0uL;
		num = ((!Is32Bit || GetModuleByModuleName("WOW64") != null) ? ReadQWord((ulong)PebAddress + processParametersOffset) : ReadDWord((ulong)PebAddress + processParametersOffset));
		if (num != 0L)
		{
			ulong num2 = num + commandLineOffset;
			ulong num3 = ReadPointer(num2 + bufferOffset);
			if (num3 != 0L)
			{
				if (guessing)
				{
					return ReadUnicodeString(num3, 32768);
				}
				return ReadUnicodeString(num3);
			}
		}
		return null;
	}

	private string GetAfter(string s, string subString)
	{
		int num = s.IndexOf(subString);
		if (num > -1)
		{
			return s.Substring(num + subString.Length);
		}
		return null;
	}

	private char FirstTrimmedChar(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return '\0';
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] != ' ')
			{
				return s[i];
			}
		}
		return '\0';
	}

	private char LastTrimmedChar(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return '\0';
		}
		for (int num = s.Length - 1; num >= 0; num--)
		{
			if (s[num] != ' ')
			{
				return s[num];
			}
		}
		return '\0';
	}

	/// <summary>
	/// If the string begins AND ends with a matching pair of single quotes, or matching pair of double qoutes, 
	/// trim the quotes away and return the interior string.
	/// </summary>
	private string TrimMatchingQuotePair(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
		{
			return s;
		}
		if ((FirstTrimmedChar(s) == '"' && LastTrimmedChar(s) == '"') || (FirstTrimmedChar(s) == '\'' && LastTrimmedChar(s) == '\''))
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < s.Length; i++)
			{
				if ((s[i] == '"') | (s[i] == '\''))
				{
					num = i;
					break;
				}
			}
			for (int num3 = s.Length - 1; num3 >= 0; num3--)
			{
				if ((s[num3] == '"') | (s[num3] == '\''))
				{
					num2 = num3;
					break;
				}
			}
			if (num2 > num)
			{
				return s.Substring(num + 1, num2 - num + 1);
			}
			return s;
		}
		return s;
	}

	private unsafe uint? GetFieldOffset(string moduleAndType, string fieldName)
	{
		if (!(RawDebugger is DebugDiag.DbgEng.IDebugSymbols3 debugSymbols))
		{
			return null;
		}
		ulong module = default(ulong);
		if (Utils.FAILED(debugSymbols.GetSymbolTypeIdWide(moduleAndType, out var TypeId, &module)))
		{
			return null;
		}
		uint value = default(uint);
		if (Utils.FAILED(debugSymbols.GetFieldTypeAndOffsetWide(module, TypeId, fieldName, null, &value)))
		{
			return null;
		}
		return value;
	}

	private ulong GetPointerValue(ulong value)
	{
		if (!Is32Bit)
		{
			return value;
		}
		return TruncateAndSignExtendAddress(value);
	}

	private ulong TruncateAndSignExtendAddress(ulong address)
	{
		return (ulong)(int)address;
	}

	private int ReadVirtual64(ulong address, out ulong value)
	{
		return Read.ReadVirtual64(this, address, out value);
	}

	private void LoadEnvironmentVariablesDictionary()
	{
		if (envVarsLoaded)
		{
			return;
		}
		envVarsLoaded = true;
		ulong num = (ulong)PebAddress;
		ulong num2 = 32uL;
		ulong num3 = 128uL;
		if (Is32Bit && GetModuleByModuleName("wow64") == null)
		{
			num2 = 16uL;
			num3 = 72uL;
		}
		ReadVirtual64(num + num2, out var value);
		ReadVirtual64(value + num3, out var value2);
		string text = ReadUnicodeString(value2);
		while (text != null && text.Length > 0)
		{
			int num4 = text.IndexOf('=');
			if (num4 != -1 && num4 != text.Length - 1)
			{
				if (num4 > 0)
				{
					string key = text.Substring(0, num4);
					string value3 = text.Substring(num4 + 1);
					envVars[key] = value3;
				}
				value2 += (ulong)((long)text.Length * 2L + 2);
				text = ReadUnicodeString(value2);
				continue;
			}
			break;
		}
	}

	/// <summary>
	/// Returns the source code line information for the address passed into the function.
	/// <example>This example shows how to get the source code line infromation of a given code address
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSourceInfoFromAddress((double)0x77f88553UL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// d:\srv03rtm\base\ntdll\daytona\obj\i386\usrstubs.asm @ 395 + c
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value to match with source code</param>
	/// <returns>String with source code information</returns>
	public string GetSourceInfoFromAddress(double Address)
	{
		return _legacyDebugger.GetSourceInfoFromAddress(Address);
	}

	/// <summary>
	/// Returns the source code line information for the address passed into the function.
	/// <example>This example shows how to get the source code line infromation of a given code address
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSourceInfoFromAddress(0x77f88553UL);
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// d:\srv03rtm\base\ntdll\daytona\obj\i386\usrstubs.asm @ 395 + c
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value to match with source code</param>
	/// <returns>String with source code information</returns>
	public string GetSourceInfoFromAddress(ulong Address)
	{
		return GetSourceInfoFromAddress((double)Address);
	}

	/// <summary>
	/// Returns the source code line information for the address passed into the function.
	/// <example>This example shows how to get the source code line infromation of a given code address
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///
	///     string strResults = debugger.GetSourceInfoFromAddress(new UIntPtr(0x77f88553UL));
	///     manager.Write(strResults);
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// } 
	/// </code>
	/// Results are:
	/// <code>
	/// d:\srv03rtm\base\ntdll\daytona\obj\i386\usrstubs.asm @ 395 + c
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="Address">Address value to match with source code</param>
	/// <returns>String with source code information</returns>
	public string GetSourceInfoFromAddress(UIntPtr Address)
	{
		return GetSourceInfoFromAddress((double)(ulong)Address);
	}

	private static LibraryModule GetLibraryModule(string path)
	{
		LibraryModule value = null;
		if (!_loadForeverModules.TryGetValue(path, out value))
		{
			value = LoadLibrary(path);
			_loadForeverModules[path] = value;
		}
		return value;
	}

	/// <summary>
	/// Returns an instance of one of the extension object. For example: HTTPInfo, ASPInfo etc.
	/// <example>
	/// <code language="cs">
	///  // Creates an instance of the NetAnalyzer
	/// using (NetAnalyzer analyzer = new NetAnalyzer())
	/// {
	///     //Get an instance of the debugger through the NetScriptManager object
	///     NetScriptManager manager = analyzer.Manager;
	///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
	///     var utilExt = debugger.GetExtensionObject("CrashHangExt", "Utils");
	///     var utilExt = debugger.GetExtensionObject("IISInfo", "HTTPInfo");
	///     ...
	///
	///     //Release Debugger native resources
	///     debugger.Dispose();
	/// }            
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="DllName">Extension Dll's name.</param>
	/// <param name="ClassName">Class you want to use to create an instance.</param>
	/// <returns>Object representing the instance of the class</returns>
	public object GetExtensionObject(string DllName, string ClassName)
	{
		object value = null;
		string key = (DllName + "|" + ClassName).ToUpper();
		if (_extensions.TryGetValue(key, out value))
		{
			return value;
		}
		if (IsRegistered)
		{
			value = _legacyDebugger.GetExtensionObject(DllName, ClassName);
		}
		else
		{
			if (!Guid.TryParse(DllName, out var result))
			{
				result = GetExtensionGuid(DllName, ClassName);
			}
			value = ComHelper.CreateInstance(GetLibraryModule("Exts\\" + DllName + ".dll"), result);
			(value as IDbgCOMExt).InitExtension(RawDebugger, _legacyDebugger);
		}
		_extensions.Add(key, value);
		return value;
	}

	private Guid GetExtensionGuid(string DllName, string ClassName)
	{
		string text = DllName + "." + ClassName;
		return text switch
		{
			"CrashHangExt.Utils" => new Guid("7233D6F8-AD31-440F-BAF0-9E7A292A53DA"), 
			"MemoryExt.VMInfo" => new Guid("5457FBA4-9406-4270-A36E-72B3268274B2"), 
			"IISInfo.HTTPInfo" => new Guid("2EED1F54-77A7-447C-8D85-2707A985D1EC"), 
			"IISInfo.ASPInfo" => new Guid("FEC54C8B-DDFD-43BE-94DD-E93C84AEB580"), 
			"COMPlusDDExt.CComplusRoot" => new Guid("842BD27A-8738-4FB1-B6DA-118DE9E79E55"), 
			_ => throw new Exception("Unknown Extension Type: " + text), 
		};
	}

	/// <summary>
	/// This method unloads any NetDbgHost extensions loaded so far. This method is for internal use only and should not be called from any custom analysis rule.
	/// </summary>
	public void UnloadExtensions()
	{
		try
		{
			if (IsRegistered)
			{
				_legacyDebugger.UnloadExtensions();
			}
		}
		catch
		{
		}
		foreach (object value in _extensions.Values)
		{
			try
			{
				if (!IsRegistered)
				{
					((IDbgCOMExt)value).TerminateExtension();
				}
				Marshal.FinalReleaseComObject(value);
			}
			catch
			{
			}
		}
	}

	private void ThrowIfLiveTarget()
	{
		try
		{
			_ = DumpType;
		}
		catch (COMException)
		{
			throw new Exception("Not supported for live targets");
		}
	}

	/// <summary>
	/// This method is used to write an arbitrary string to the text log file created and maintained by DbgHost. Applicable for live debugging only.
	/// </summary>
	/// <param name="Output">String value to write on the log file.</param>
	public void Write(string Output)
	{
		_legacyDebugger.Write(Output);
	}

	/// <summary>
	/// This method creates a user dump of the debuggee process.
	/// </summary>
	/// <param name="DumpReason">The DumpReason parameter is any arbitrary string that is appended to the dump file name 
	/// and comments to provide the reason for the dump creation to the end user.</param>
	/// <param name="bMiniDump">The bMiniDump parameter is a boolean value which, when set to true, creates a mini 
	/// dump of the target process and when set to false creates a full dump.</param>
	/// <param name="bProcessList">The bProcessList parameter is a boolean value which, when true, instructs the debugger to create
	/// a list of the running processes on the system at the time the dump is taken.</param>
	/// <returns>The return value is the full path to the dump created if successful for Live debugging only.</returns>
	public string CreateDump(string DumpReason, bool bMiniDump, bool bProcessList)
	{
		return _legacyDebugger.CreateDump(DumpReason, bMiniDump, bProcessList);
	}

	/// <summary>
	/// This method is similar to <see cref="M:DebugDiag.DotNet.NetDbgObj.CreateDump(System.String,System.Boolean,System.Boolean)" /> method but can be used to create user dumps for processes other than debuggee process.
	/// </summary>
	/// <param name="ProcessID">The input parameter ProcessID identifies the process to create a user dump for.</param>
	/// <param name="DumpReason">The DumpReason parameter is any arbitrary string that is appended to the dump file name and comments to provide the 
	/// reason for the dump creation to the end user.</param>
	/// <param name="bMiniDump">The bMiniDump parameter is a boolean value which, when set to true, creates a mini dump of the target process and 
	/// when set to false creates a full dump. </param>
	/// <param name="bCreateProcessList">The bProcessList parameter is a boolean value which, when true, instructs the debugger to create
	/// a list of the running processes on the system at the time the dump is taken.</param>
	/// <returns>The return value is the full path to the dump created if successful for Live debugging only.</returns>
	public string CreateDumpForProcessID(int ProcessID, string DumpReason, bool bMiniDump, bool bCreateProcessList)
	{
		return _legacyDebugger.CreateDumpForProcessID(ProcessID, DumpReason, bMiniDump, bCreateProcessList);
	}

	/// <summary>
	/// This method is similar to <see cref="M:DebugDiag.DotNet.NetDbgObj.CreateDump(System.String,System.Boolean,System.Boolean)" /> method but can be used to create user dumps for processes other than debuggee process.
	/// </summary>
	/// <param name="ProcessName">The input parameter ProcessName identifies the process to create user dumps for by name (Ex: notepad.exe).</param>
	/// <param name="DumpReason">The DumpReason parameter is any arbitrary string that is appended to the dump file name and comments to provide the 
	/// reason for the dump creation to the end user.</param>
	/// <param name="bMiniDump">The bMiniDump parameter is a boolean value which, when set to true, creates a mini dump of the target process and 
	/// when set to false creates a full dump. </param>
	/// <param name="bCreateProcessList">The bProcessList parameter is a boolean value which, when true, instructs the debugger to create
	/// a list of the running processes on the system at the time the dump is taken.</param>
	/// <returns>The return value is the full path to the dump created if successful for Live debugging only.</returns>
	public object CreateDumpForProcessName(string ProcessName, string DumpReason, bool bMiniDump, bool bCreateProcessList)
	{
		return _legacyDebugger.CreateDumpForProcessName(ProcessName, DumpReason, bMiniDump, bCreateProcessList);
	}

	/// <summary>
	/// This method loads (injects) LeakTrack into the debuggee process. This starts memory and handle leak tracking in the debuggee process.  Applicable for live debugging only.
	/// </summary>
	public void InjectLeakTrack()
	{
		_legacyDebugger.InjectLeakTrack();
	}

	/// <summary>
	/// Allows to set a breakpoint while in live debugging
	/// </summary>
	/// <param name="OffsetExpression">Expression where the breakpoint should be Added</param>
	/// <param name="vbIsManaged">Paramter to indicate if the code is native or managed, true indicates managed code</param>
	/// <returns>integer value indicating the index for the breakpoint added on the debugger</returns>
	public int AddCodeBreakpoint(string OffsetExpression, bool vbIsManaged)
	{
		return _legacyDebugger.AddCodeBreakpoint(OffsetExpression, vbIsManaged);
	}

	/// <summary>
	/// Allows to remove a breakpoint while in live debugging
	/// </summary>
	/// <param name="BreakpointId">Index of the breakpoint to remove</param>
	public void RemoveBreakpoint(int BreakpointId)
	{
		_legacyDebugger.RemoveBreakpoint(BreakpointId);
	}

	/// <summary>
	/// Returns an instance of a COM object that implements the IDbgBreakPoint interface
	/// </summary>
	/// <param name="BreakpointId">Index of the breakpoint</param>
	/// <returns>Reference to the COM object</returns>
	public IDbgBreakPoint GetBreakpoint(int BreakpointId)
	{
		return _legacyDebugger.GetBreakpoint(BreakpointId);
	}

	/// <summary>
	/// This method will generate a log file with the current list of running processes on the system. This method will work for a live debugging target only.   
	/// The file is placed in the path specified by the NetDbgObj.DumpPath, otherwise it is placed in the \DebugDiag\Logs\Misc directory. 
	/// </summary>
	/// <param name="FullPath">Path where the log will be written</param>
	public void CreateProcessList(string FullPath)
	{
		_legacyDebugger.CreateProcessList(FullPath);
	}

	/// <summary>
	/// This method can be called to detach the DbgHost.exe from a target process that it is currently debugging.  
	/// This call is asynchronous, and returns immediately even if the debugger has not attached. 
	/// </summary>
	public void Detach()
	{
		_legacyDebugger.Detach();
	}

	/// <summary>
	/// Method that will return true if leaktrack module has been loaded on the process pointed on the parameter
	/// </summary>
	/// <param name="ProcessID">integer value representing the process ID</param>
	/// <returns>True if leaktrack has been loaded, false otherwise</returns>
	public bool IsLeakTrackLoaded(int ProcessID)
	{
		return _legacyDebugger.IsLeakTrackLoaded(ProcessID);
	}

	/// <summary>
	/// This function will stop the debugger.
	/// </summary>
	public void Kill()
	{
		_legacyDebugger.Kill();
	}

	public IList<ThreadInfo> MexGetThreads(Threads.ThreadSortType sortType = Microsoft.Mex.Framework.Threads.ThreadSortType.None)
	{
		return mexThreads.GetThreads(sortType);
	}

	private void InitKernel(DebugUtilities mexDebugUtilities)
	{
		mexThreads = new Threads(mexDebugUtilities);
		MexDumpInfo = new List<DebugDiag.DotNet.WinDE.DumpInfo>
		{
			new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Bugcheck Code",
				QWord = BugCheck.BugCheckCode
			},
			new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Bugcheck Arg1",
				QWord = BugCheck.Arg1
			},
			new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Bugcheck Arg2",
				QWord = BugCheck.Arg2
			},
			new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Bugcheck Arg3",
				QWord = BugCheck.Arg3
			},
			new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Bugcheck Arg4",
				QWord = BugCheck.Arg4
			}
		};
		if (NativeException != null)
		{
			MexDumpInfo.Add(new DebugDiag.DotNet.WinDE.DumpInfo
			{
				PropertyName = "Last Exception Code",
				QWord = (ulong)NativeException.ExceptionCode
			});
		}
	}
}
