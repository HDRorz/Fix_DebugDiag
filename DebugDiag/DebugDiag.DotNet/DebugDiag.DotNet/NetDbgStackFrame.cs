using System;
using System.Linq;
using DebugDiag.DbgLib;
using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents a single frame of a call stack for a thread in the debugger.   An instance of this object is retrieved from the <c>NetDbgStackFrames</c> 
/// object.
/// </summary>
/// <remarks>
/// <example>
/// <code language="cs">
/// //Create an instance of the NetAnalyzer object to access the NetScriptManager
/// using (NetAnalyzer analyzer = new NetAnalyzer())
/// {
///     //Get an instance of the debugger through the NetScriptManager object
///     NetScriptManager manager = analyzer.Manager;
///     NetDbgObj debugger = manager.GetDebugger(@"C:\user.dmp");
///
///     if (debugger.IsCrashDump)
///     {
///             //Gets a reference to the Thread where the Exception happened
///             NetDbgThread DbgThread = debugger.ExceptionThread;
///
///             //Gets a reference to the collection of stackframes of the thread 
///             NetDbgStackFrames StackFrames = DbgThread.StackFrames;
///
///             //Get an instance of the Module Object using the debugger
///             NetDbgStackFrame StackFrame = StackFrames[2];
///
///             manager.WriteLine("Return Address: " + debugger.GetAs32BitHexString(StackFrame.ReturnAddress));
///             manager.WriteLine("Instruction Address: " + debugger.GetAs32BitHexString(StackFrame.InstructionAddress));
///             manager.WriteLine("Stack Address: " + debugger.GetAs32BitHexString(StackFrame.StackAddress));
///             manager.WriteLine("Frame Number: " + StackFrame.FrameNumber);
///             manager.WriteLine("Child EBP: " + debugger.GetAs32BitHexString(StackFrame.ChildEBP));
///             manager.WriteLine("Argument 1: " + debugger.GetAs32BitHexString(StackFrame.GetArg(0)));
///             manager.WriteLine("Argument 2: " + debugger.GetAs32BitHexString(StackFrame.GetArg(1)));
///             manager.WriteLine("Argument 3: " + debugger.GetAs32BitHexString(StackFrame.GetArg(2)));
///             manager.WriteLine("Argument 4: " + debugger.GetAs32BitHexString(StackFrame.GetArg(3)));   
///     }
///     else
///         manager.WriteLine("This is not a crash dump");
///
///     //Release Debugger native resources
///     debugger.Dispose();
/// }
/// </code>
/// </example>
/// </remarks>
public class NetDbgStackFrame
{
	private string _frameTextWithOffset;

	private string _frameTextWithOffsetAndSrc;

	private string _frameTextWithSrcInfo;

	private double _childEbp;

	private double _returnAddress;

	private double _instructionAddress;

	private double _stackAddress;

	private ulong _offset;

	private const int MAX_PARAM_COUNT = 3;

	private double[] _params = new double[3];

	private int _frameNumber;

	private NetDbgObj _debugger;

	private string _frameText;

	private bool _isNativeFrame;

	private string _functionName;

	private IDbgModule _module;

	private string _moduleName;

	private bool _inited;

	/// <summary>
	/// Returns tru if the current frame is for a managed (.Net) function
	/// </summary>
	public bool IsManaged => !_isNativeFrame;

	/// <summary>
	/// Returns an instance of the COM Module object associated with the code being executed on the current frame
	/// </summary>
	public IDbgModule Module
	{
		get
		{
			EnsureInit();
			return _module;
		}
	}

	/// <summary>
	/// Returns the module name associated with the code being executed on the current frame
	/// </summary>
	public string ModuleName
	{
		get
		{
			EnsureInit();
			return _moduleName;
		}
	}

	/// <summary>
	/// This property returns the value for the offset of the instruction being executed at that time.
	/// </summary>
	public ulong Offset
	{
		get
		{
			EnsureInit();
			return _offset;
		}
	}

	/// <summary>
	/// Returns a string representing the function name only that is being executed on the current frame
	/// </summary>
	public string FunctionName
	{
		get
		{
			if (_functionName == null)
			{
				_functionName = GetFrameText(includeOffset: false, includeSourceInfo: false);
				if (_functionName.Contains('!'))
				{
					_functionName = _functionName.Substring(_functionName.IndexOf('!') + 1);
				}
			}
			return _functionName;
		}
	}

	/// <summary>
	/// This property returns the return address for this stack frame. 
	/// </summary>
	public double ReturnAddress => _returnAddress;

	/// <summary>
	/// This property returns the instruction address for this stack frame.
	/// </summary>
	public double InstructionAddress => _instructionAddress;

	/// <summary>
	/// This property returns the calculated value of ESP for this stack frame.
	/// </summary>
	public double StackAddress => _stackAddress;

	/// <summary>
	/// This property returns the number for this frame, or gets the frame for the specified value. 
	/// A value of zero indicates the top of the stack frame.
	/// <value>Frame number to get</value>
	/// </summary>
	public int FrameNumber
	{
		get
		{
			return _frameNumber;
		}
		set
		{
			_frameNumber = value;
		}
	}

	/// <summary>
	/// This property returns the value of EBP for this stack frame. 
	/// </summary>
	public double ChildEBP => _childEbp;

	internal NetDbgStackFrame(ClrStackFrame clrStackFrame, NetDbgObj debugger)
	{
		_debugger = debugger;
		_stackAddress = clrStackFrame.StackPointer;
		_instructionAddress = clrStackFrame.InstructionPointer;
		if (clrStackFrame.Kind == ClrStackFrameType.Runtime)
		{
			_frameText = NetDbgObj.GetDisplayTextFromClrStackFrame(clrStackFrame);
		}
		else
		{
			_frameText = clrStackFrame.ToString();
		}
		if (!_frameText.Contains("!") && !string.IsNullOrEmpty(ModuleName))
		{
			_frameText = ModuleName + "!" + _frameText;
		}
		_isNativeFrame = false;
		if (_instructionAddress != 0.0)
		{
			_debugger.AddManagedIP(this);
		}
	}

	internal NetDbgStackFrame(IDbgStackFrame legacyStackFrame, NetDbgObj debugger)
	{
		_debugger = debugger;
		_isNativeFrame = true;
		UpdateWithNativeFrameInfo(legacyStackFrame);
	}

	/// <summary>
	/// Returns the string that represents the function being executed on the frame
	/// </summary>
	/// <param name="includeOffset">Boolean parameter to indicate if the offset value of the instruction being executed should be included on the return value</param>
	/// <param name="includeSourceInfo">Boolean parameter to indicate if the corresponding source file information should be included on the return value</param>
	/// <returns>Function name</returns>
	public string GetFrameText(bool includeOffset, bool includeSourceInfo)
	{
		EnsureInit();
		if (includeOffset && includeSourceInfo)
		{
			return _frameTextWithOffsetAndSrc;
		}
		if (includeSourceInfo)
		{
			return _frameTextWithSrcInfo;
		}
		if (includeOffset)
		{
			return _frameTextWithOffset;
		}
		return _frameText;
	}

	private void EnsureInit()
	{
		if (_inited)
		{
			return;
		}
		_inited = true;
		_module = _debugger.GetModuleByAddress(_instructionAddress, out _moduleName);
		if (_isNativeFrame)
		{
			_frameTextWithOffset = _debugger.GetSymbolFromAddress(_instructionAddress);
			int num = _frameTextWithOffset.LastIndexOf('+');
			if (num > -1)
			{
				_frameText = _frameTextWithOffset.Substring(0, num);
			}
			else
			{
				_frameText = _frameTextWithOffset;
			}
		}
		else
		{
			ClrMethod methodByAddress = _debugger.ClrRuntime.GetMethodByAddress((ulong)_instructionAddress);
			if (methodByAddress != null)
			{
				_offset = (ulong)_instructionAddress - methodByAddress.NativeCode;
			}
			if (_offset == 0L)
			{
				_frameTextWithOffset = _frameText;
			}
			else
			{
				_frameTextWithOffset = $"{_frameText}+{_offset:x}";
			}
			if (!string.IsNullOrEmpty(ModuleName))
			{
				_frameTextWithOffset = $"{ModuleName}!{_frameTextWithOffset}";
			}
		}
		string sourceInfoFromAddress = _debugger.GetSourceInfoFromAddress(_instructionAddress);
		if (string.IsNullOrEmpty(sourceInfoFromAddress))
		{
			_frameTextWithSrcInfo = _frameText;
			_frameTextWithOffsetAndSrc = _frameTextWithOffset;
		}
		else
		{
			_frameTextWithSrcInfo = $"{_frameText} [{sourceInfoFromAddress}]";
			_frameTextWithOffsetAndSrc = $"{_frameTextWithOffset} [{sourceInfoFromAddress}]";
		}
	}

	internal bool IsTextInFrame(string searchText, bool ignoreCase, FrameSearchOptions searchOption)
	{
		StringComparison comparisonType = (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
		searchText = CleanSearchFrameText(searchText, trimManagedModuleAndRuntimeFrameInfo: true);
		switch (searchOption)
		{
		case FrameSearchOptions.PartialMatch:
			if (GetFrameText(includeOffset: false, includeSourceInfo: false).IndexOf(searchText, comparisonType) > -1)
			{
				return true;
			}
			break;
		case FrameSearchOptions.CompleteMatchIgnoreOffsets:
			searchText = TrimOffset(searchText);
			if (string.Compare(searchText, GetFrameText(includeOffset: false, includeSourceInfo: false), ignoreCase) == 0)
			{
				return true;
			}
			break;
		case FrameSearchOptions.CompleteMatchIncludingOffsets:
			if (string.Compare(searchText, GetFrameText(includeOffset: true, includeSourceInfo: false), ignoreCase) == 0)
			{
				return true;
			}
			break;
		}
		return false;
	}

	internal static string CleanSearchFrameText(string searchText, bool trimManagedModuleAndRuntimeFrameInfo)
	{
		searchText = TrimSourceInfo(searchText);
		searchText = TrimAddresses(searchText);
		if (trimManagedModuleAndRuntimeFrameInfo)
		{
			searchText = TrimManagedModuleAndRuntimeFrameInfo(searchText);
		}
		return searchText;
	}

	private static string TrimSourceInfo(string searchText)
	{
		if (searchText.EndsWith("]"))
		{
			int num = searchText.LastIndexOf(" [");
			if (num > -1 && searchText.IndexOf(" @ ", num + 1) > -1)
			{
				searchText = searchText.Substring(0, num);
			}
		}
		return searchText;
	}

	private static string TrimAddresses(string searchText)
	{
		string text = null;
		int num = searchText.IndexOf('(');
		string text2;
		if (num > -1)
		{
			text2 = searchText.Substring(0, num);
			text = searchText.Substring(num);
		}
		else
		{
			text2 = searchText;
		}
		int num2 = text2.IndexOf('[');
		int num3 = -1;
		num3 = ((num2 == -1) ? text2.LastIndexOf(' ') : text2.Substring(0, num2).LastIndexOf(' '));
		if (num3 > -1)
		{
			text2 = text2.Substring(num3 + 1);
		}
		searchText = ((!string.IsNullOrEmpty(text)) ? (text2 + text) : text2);
		return searchText;
	}

	private static string TrimManagedModuleAndRuntimeFrameInfo(string searchText)
	{
		if (searchText.IndexOf('(') > -1)
		{
			int num = searchText.IndexOf('!');
			if (num > -1)
			{
				searchText = searchText.Substring(num + 1);
			}
		}
		return searchText;
	}

	private static string TrimOffset(string searchText)
	{
		int num = searchText.LastIndexOf('+');
		if (num > -1 && searchText.LastIndexOf(')') < num)
		{
			searchText = searchText.Substring(0, num);
		}
		return searchText;
	}

	internal void UpdateWithNativeFrameInfo(IDbgStackFrame legacyStackFrame)
	{
		if (_isNativeFrame)
		{
			_instructionAddress = legacyStackFrame.InstructionAddress;
			_stackAddress = legacyStackFrame.StackAddress;
		}
		_childEbp = legacyStackFrame.ChildEBP;
		_returnAddress = legacyStackFrame.ReturnAddress;
		_frameNumber = legacyStackFrame.FrameNumber;
		for (int i = 0; i < 3; i++)
		{
			_params[i] = legacyStackFrame[i];
		}
	}

	/// <summary>
	/// This property returns the function arguments passed on the stack for this stack frame. 
	/// When calling this function pass in a value of 0,1 2, or 3 to get the 1st, 2nd, 3rd or 4th argument on the stack respectively.
	/// </summary>
	/// <param name="argNumber">Integer that represents the argument index</param>
	/// <returns>Long representing the value found on that memory address</returns>
	public double GetArg(int argNumber)
	{
		return _params[argNumber];
	}
}
