using System;
using System.Collections.Generic;
using DebugDiag.DbgEng;
using DebugDiag.DbgLib;
using Microsoft.Diagnostics.Runtime;
using ClrObject = Microsoft.Diagnostics.RuntimeExt.ClrObject;

namespace DebugDiag.DotNet;

/// <summary>
/// This object manages information about a thread in the dump file. An instance of this object can be obtained from the 
/// <c>NetDbgObj.GetThreadBySystemID</c>, the <c>NetDbgObj.GetThreadByManagedThreadId</c> and ExceptionThread methods of 
/// the <see cref="T:DebugDiag.DotNet.NetDbgObj" /> object, or by getting the thread list using the <c>NetDbgObj.Threads</c> method.
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
///             manager.WriteLine("System ID: " + DbgThread.SystemID.ToString());
///             manager.WriteLine("Debugger ID: " + DbgThread.ThreadID.ToString());
///             manager.WriteLine("Start Address: " + debugger.GetAs32BitHexString(DbgThread.StartAddress) + " " + debugger.GetSymbolFromAddress(DbgThread.StartAddress));
///             manager.WriteLine("Stack Address: " + debugger.GetAs32BitHexString(DbgThread.StackAddress));
///             manager.WriteLine("Frame Address: " + debugger.GetAs32BitHexString(DbgThread.FrameAddress));
///             manager.WriteLine("Instruction Address: " + debugger.GetAs32BitHexString(DbgThread.InstructionAddress) + " " + debugger.GetSymbolFromAddress(DbgThread.InstructionAddress)); 
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
public class NetDbgThread
{
	private IDbgThread _legacyThread;

	private bool _disposed;

	private ClrThread _managedThread;

	private NetDbgObj _debugger;

	private COMApartmentType _comApartmentType = COMApartmentType.Unknown;

	private NetDbgStackFrames _mixedStackFrames;

	private NetDbgStackFrames _nativeStackFrames;

	private NetDbgStackFrames _managedStackFrames;

	/// <summary>
	/// Get an instance of the NetDbgObj being used to analyze the dump
	/// </summary>
	public NetDbgObj Debugger => _debugger;

	/// <summary>
	/// Returns the COM apartment threading type
	/// </summary>
	public COMApartmentType COMApartmentType => _comApartmentType;

	/// <summary>
	/// This method returns a <c>NetDbgStackFrames</c> object that represents the native and managed stacks combined.
	/// </summary>
	public NetDbgStackFrames StackFrames
	{
		get
		{
			if (_mixedStackFrames == null)
			{
				_mixedStackFrames = new NetDbgStackFrames(_legacyThread, _debugger);
			}
			return _mixedStackFrames;
		}
	}

	/// <summary>
	/// This property returns an instance of a <c>NetDbgStackFrames</c> object, which represents the native (unmanaged) stack frames on this thread. 
	/// </summary>
	public NetDbgStackFrames NativeStackFrames
	{
		get
		{
			if (_nativeStackFrames == null)
			{
				_nativeStackFrames = new NetDbgStackFrames(_legacyThread, _debugger, includeNativeFrames: true, includeManagedFrames: false);
			}
			return _nativeStackFrames;
		}
	}

	/// <summary>
	/// This property returns an instance of a <c>NetDbgStackFrames</c> object, which represents the managed stack frames on this thread. 
	/// </summary>
	public NetDbgStackFrames ManagedStackFrames
	{
		get
		{
			if (_managedStackFrames == null)
			{
				_managedStackFrames = new NetDbgStackFrames(_legacyThread, _debugger, includeNativeFrames: false);
			}
			return _managedStackFrames;
		}
	}

	/// <summary>
	/// This method returns a <c>ClrThread</c> representing the managed (.Net) thread.
	/// </summary>
	public ClrThread ManagedThread => _managedThread;

	/// <summary>
	/// This property returns the system thread ID for this thread.
	/// </summary>
	public double SystemID => _legacyThread.SystemID;

	/// <summary>
	/// This property returns the debugger thread ID for this thread.
	/// </summary>
	public int ThreadID => _legacyThread.ThreadID;

	/// <summary>
	/// This property returns the address of the thread procedure for this thread, if extended thread information is available, otherwise it will return NULL.  
	/// Call <c>NetDbgObj.ExtendedThreadInfoAvailable</c> to determine if extended thread information is available.
	/// </summary>
	public double StartAddress => _legacyThread.StartAddress;

	/// <summary>
	/// This property returns the date and time that the thread was created. 
	/// </summary>
	public DateTime CreateTime => _legacyThread.CreateTime;

	/// <summary>
	/// This property returns the value of the EIP register for this thread context. 
	/// </summary>
	public double InstructionAddress => _legacyThread.InstructionAddress;

	/// <summary>
	/// This property returns the value of the ESP register for this thread context.
	/// </summary>
	public double StackAddress => _legacyThread.StackAddress;

	/// <summary>
	/// This property returns the value of the EBP register for this thread context.
	/// </summary>
	public double FrameAddress => _legacyThread.FrameAddress;

	/// <summary>
	/// This Property returns the value of the specified Register name
	/// </summary>
	/// <param name="RegisterName">String with the register name to return</param>
	/// <returns>Double that contains the value found on the given register</returns>
	public double this[string RegisterName] => _legacyThread[RegisterName];

	/// <summary>
	/// This property returns the address of a critical section structure if this thread is currently waiting on a critical section, otherwise it returns zero.
	/// </summary>
	public double WaitingOnCritSecAddr => _legacyThread.WaitingOnCritSecAddr;

	/// <summary>
	/// This property returns the debugger process ID for target process (COM Call). 
	/// </summary>
	public int COMDestinationProcessID => _legacyThread.COMDestinationProcessID;

	/// <summary>
	/// This property returns the debugger thread ID for the thread in target process (COM Call). 
	/// </summary>
	public int COMDestinationThreadID => _legacyThread.COMDestinationThreadID;

	/// <summary>
	/// This property that returns the Source socket used on this thread if a winsock communication is occurring on the thread
	/// </summary>
	public string SocketSourceAddress => _legacyThread.SocketSourceAddress;

	/// <summary>
	/// This property that returns the Destination Address used on this thread if a winsock communication is occurring on the thread
	/// </summary>
	public string SocketDestinationAddress => _legacyThread.SocketDestinationAddress;

	/// <summary>
	/// This property returns the RPC source bindings if an RPC communication is detected on the thread
	/// </summary>
	public string RpcSourceBindings => _legacyThread.RpcSourceBindings;

	/// <summary>
	/// This property returns the RPC destination bindings if an RPC communication is detected on the thread
	/// </summary>
	public string RpcDestinationBindings => _legacyThread.RpcDestinationBindings;

	internal NetDbgThread(IDbgThread legacyThread, NetDbgObj debugger)
	{
		_debugger = debugger;
		_legacyThread = legacyThread;
		if (debugger.ClrRuntime != null)
		{
			foreach (ClrThread thread in debugger.ClrRuntime.Threads)
			{
				if ((double)thread.OSThreadId == _legacyThread.SystemID)
				{
					_managedThread = thread;
					break;
				}
			}
		}
		try
		{
			IDebugSystemObjects2 debugSystemObjects = (IDebugSystemObjects2)_debugger.RawDebugger;
			uint Id = 0u;
			ulong Offset = 0uL;
			ulong num = 0uL;
			int num2 = 0;
			if (debugSystemObjects != null)
			{
				debugSystemObjects.GetCurrentThreadId(out Id);
				if (Id != _legacyThread.ThreadID)
				{
					debugSystemObjects.SetCurrentThreadId((uint)_legacyThread.ThreadID);
				}
				debugSystemObjects.GetCurrentThreadTeb(out Offset);
				if (Id != _legacyThread.ThreadID)
				{
					debugSystemObjects.SetCurrentThreadId(Id);
				}
			}
			if (Offset != 0L)
			{
				if (_debugger.Is32Bit)
				{
					num = _debugger.ReadDWord((Offset & 0xFFFFFFFFu) + 3968);
					num2 = (int)_debugger.ReadDWord(num + 12);
				}
				else
				{
					num = _debugger.ReadQWord(Offset + 5976);
					num2 = (int)_debugger.ReadDWord(num + 20);
				}
				if (Convert.ToBoolean(num2 & 0x80))
				{
					_comApartmentType = COMApartmentType.STA;
				}
				else if (Convert.ToBoolean(num2 & 0x100))
				{
					_comApartmentType = COMApartmentType.MTA;
				}
				else if (Convert.ToBoolean(num2 & 0x800))
				{
					_comApartmentType = COMApartmentType.Neutral;
				}
				else
				{
					_comApartmentType = COMApartmentType.Unknown;
				}
			}
		}
		catch (Exception)
		{
			_comApartmentType = COMApartmentType.Unknown;
		}
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrException</c> objects found on the stack
	/// </summary>
	/// <returns></returns>
	public IEnumerable<ClrException> EnumerateStackExceptionObjects()
	{
		if (_debugger.ClrHeap == null)
		{
			yield break;
		}
		foreach (ClrObject item in EnumerateStackObjects())
		{
			ClrType heapType = item.GetHeapType();
			if (heapType != null && heapType.IsException)
			{
				yield return _debugger.ClrHeap.GetExceptionObject(item.GetValue());
			}
		}
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrObject</c> or managed (.Net) objects found on the stack
	/// </summary>
	/// <param name="stopSearchAtStackPointer">Optional value to indicate if the search should stop at the stack pointer, the default vlue is true.</param>
	/// <returns>Collection of ClrObjects representing the objects found on the stack</returns>
	public IEnumerable<ClrObject> EnumerateStackObjects(bool stopSearchAtStackPointer = true)
	{
		return EnumerateStackObjects(null, stopSearchAtStackPointer);
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrRoot</c> objects of all managed (.Net) objects found on the stack
	/// </summary>
	/// <returns>Collection of ClrRoot objects found on the stack</returns>
	/// <overloads>This method has two overloaded methods</overloads>
	public IEnumerable<ClrRoot> EnumerateStackObjectRoots()
	{
		return EnumerateStackObjectRoots(null);
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrRoot</c> objects of the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Managed Type name</param>
	/// <returns>Collection of ClrRoot objects found on the stack</returns>
	public IEnumerable<ClrRoot> EnumerateStackObjectRoots(string typeName)
	{
		return EnumerateStackObjectRoots(typeName, beginSearchAtStackPointer: true);
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrRoot</c> objects of the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Managed Type name</param>
	/// <param name="beginSearchAtStackPointer">Booloean value to indicate where to start looking for the objects.</param>
	/// <returns>Collection of ClrRoot objects found on the stack</returns>
	public IEnumerable<ClrRoot> EnumerateStackObjectRoots(string typeName, bool beginSearchAtStackPointer)
	{
		if (_managedThread == null || !_managedThread.IsAlive || _managedThread.IsUnstarted)
		{
			yield break;
		}
		double stackAddress = StackAddress;
		foreach (ClrRoot item in _managedThread.EnumerateStackObjects())
		{
			if ((!beginSearchAtStackPointer || (double)item.Address >= stackAddress) && (string.IsNullOrEmpty(typeName) || (item.Type != null && item.Type.Name == typeName)))
			{
				yield return item;
			}
		}
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrObject</c> or managed (.Net) objects found on the stack
	/// </summary>
	/// <returns>Collection of ClrObject representing the objects found on the stack</returns>
	/// <overloads>This method has three overloaded methods</overloads>
	public IEnumerable<ClrObject> EnumerateStackObjects()
	{
		return EnumerateStackObjects(null);
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrObject</c> or managed (.Net) objects found on the stack of the type specified
	/// </summary>
	/// <param name="typeName">Managed type name</param>
	/// <returns>Collection of ClrObject representing the objects found on the stack</returns>
	public IEnumerable<ClrObject> EnumerateStackObjects(string typeName)
	{
		return EnumerateStackObjects(typeName, beginSearchAtStackPointer: true);
	}

	/// <summary>
	/// This method returns an IEnumerable collection of <c>ClrObject</c> or managed (.Net) objects found on the stack of the type specified
	/// </summary>
	/// <param name="typeName">Managed type name</param>
	/// <param name="beginSearchAtStackPointer">Booloean value to point where to start looking for the objects.</param>
	/// <returns>Collection of ClrObject representing the objects found on the stack</returns>
	public IEnumerable<ClrObject> EnumerateStackObjects(string typeName, bool beginSearchAtStackPointer)
	{
		if (_managedThread == null || !_managedThread.IsAlive || _managedThread.IsUnstarted)
		{
			yield break;
		}
		_ = StackAddress;
		foreach (ClrRoot item in EnumerateStackObjectRoots(typeName, beginSearchAtStackPointer))
		{
			yield return new ClrObject(_debugger.ClrHeap, null, item.Object);
		}
	}

	/// <summary>
	/// This Function returns a ClrRoot of the first managed object that is found on the .Net stack.
	/// </summary>
	/// <returns>First ClrRoot Object found on the stack</returns>
	/// <overloads>This methos has two overloads</overloads>
	public ClrRoot FindFirstStackObjectRoot()
	{
		return FindFirstStackObjectRoot(null);
	}

	/// <summary>
	/// This Function returns a ClrRoot of the first managed object that is found on the .Net stack that matches with the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Type name for filtering objects matching only the specified type.</param>
	/// <returns>First ClrRoot Object found on the stack</returns>
	public ClrRoot FindFirstStackObjectRoot(string typeName)
	{
		return FindFirstStackObjectRoot(typeName, beginSearchAtStackPointer: true);
	}

	/// <summary>
	/// This Function returns a ClrRoot of the first managed object that is found on the .Net stack that matches with the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Type name to filter objects matching only the specified type.</param>
	/// <param name="beginSearchAtStackPointer">Indicates if the search must begin on the stack pointer if set to true.</param>
	/// <returns>First ClrRoot Object found on the stack</returns>
	public ClrRoot FindFirstStackObjectRoot(string typeName, bool beginSearchAtStackPointer)
	{
		using (IEnumerator<ClrRoot> enumerator = EnumerateStackObjectRoots(typeName, beginSearchAtStackPointer).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	/// <summary>
	/// This Function returns a ClrObject of the first managed object that is found on the .Net stack.
	/// </summary>
	/// <returns>returns a ClrObject of the first object found on the stack.</returns>
	/// <overloads>This method has two overloads.</overloads>
	public ClrObject FindFirstStackObject()
	{
		return FindFirstStackObject(null);
	}

	/// <summary>
	/// This Function returns a ClrObject of the first managed object that is found on the .Net stack that matches with the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Type name to filter objects matching only the specified type.</param>
	/// <returns>ClrObject of the first managed object that matches the type on the managed stack.</returns>
	public ClrObject FindFirstStackObject(string typeName)
	{
		return FindFirstStackObject(typeName, beginSearchAtStackPointer: true);
	}

	/// <summary>
	/// This Function returns a ClrObject of the first managed object that is found on the .Net stack that matches with the type specified on the parameter.
	/// </summary>
	/// <param name="typeName">Type name to filter objects matching only the specified type.</param>
	/// <param name="beginSearchAtStackPointer">Indicates if the search must begin on the stack pointer if set to true.</param>
	/// <returns>ClrObject of the first managed object that matches the type on the managed stack.</returns>
	public ClrObject FindFirstStackObject(string typeName, bool beginSearchAtStackPointer)
	{
		using (IEnumerator<ClrObject> enumerator = EnumerateStackObjects(typeName, beginSearchAtStackPointer).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds the first frame which matches the searchText provided, using CompleteMatchIgnoreOffsets as the searchOption and ignoring case
	/// </summary>
	/// <param name="searchText">The frame text to search for</param>
	/// <returns>The 0-based frame number of the first matching frame</returns>
	public int FindFrame(string searchText)
	{
		return FindFrame(searchText, ignoreCase: true);
	}

	/// <summary>
	/// Finds the first frame which matches the searchText provided, using CompleteMatchIgnoreOffsets as the searchOption and honoring/ignoring case depending on ignoreCase
	/// </summary>
	/// <param name="searchText">The frame text to search for</param>
	/// <param name="ignoreCase">True to ignore case.  Otherwise case will be considered.</param>
	/// <returns>The 0-based frame number of the first matching frame</returns>        
	public int FindFrame(string searchText, bool ignoreCase)
	{
		return FindFrame(searchText, ignoreCase, FrameSearchOptions.CompleteMatchIgnoreOffsets);
	}

	/// <summary>
	/// Finds the first frame which matches the searchText provided, using the specified searchOption and honoring/ignoring case depending on ignoreCase
	/// </summary>
	/// <param name="searchText">The frame text to search for</param>
	/// <param name="ignoreCase">True to ignore case.  Otherwise case will be considered.</param>
	/// <param name="searchOption">A FrameSearchOptions value which specifies what portion of the stack frame must match the searchText</param>
	/// <returns>The 0-based frame number of the first matching frame</returns>        
	public int FindFrame(string searchText, bool ignoreCase, FrameSearchOptions searchOption)
	{
		int num = 0;
		foreach (NetDbgStackFrame stackFrame in StackFrames)
		{
			_debugger.GetSymbolFromAddress(stackFrame.InstructionAddress);
			if (stackFrame.IsTextInFrame(searchText, ignoreCase, searchOption))
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	/// <summary>
	/// Searches for a matching frame or set of consecutive frames within the call stack
	/// </summary>
	/// <param name="searchFrames">Array of string with the frame pattern that you are looking into the stack</param>
	/// <returns>The frame number of the top-most stack frame if a match is found, otherwise returns -1</returns>
	/// <overloads>This method has one overloaded methods</overloads>
	public int StackMatch(params string[] searchFrames)
	{
		return StackMatch(ignoreCase: true, searchFrames);
	}

	/// <summary>
	/// Searches for a matching frame or set of consecutive frames within the call stack
	/// </summary>
	/// <param name="ignoreCase">Match string ignoring the case if set to true.</param>
	/// <param name="searchFrames">Array of string with the frame pattern that you are looking into the stack</param>
	/// <returns>The frame number of the top-most stack frame if a match is found, otherwise returns -1</returns>
	public int StackMatch(bool ignoreCase, params string[] searchFrames)
	{
		return StackMatch(ignoreCase, FrameSearchOptions.CompleteMatchIgnoreOffsets, searchFrames);
	}

	private int StackMatch(bool ignoreCase, FrameSearchOptions searchOption, params string[] searchFrames)
	{
		if (searchFrames == null || searchFrames.Length == 0)
		{
			throw new ArgumentNullException("searchFrames");
		}
		if (searchFrames.Length == 1)
		{
			searchFrames = searchFrames[0].Split(new string[2] { "\r\n", "|" }, StringSplitOptions.RemoveEmptyEntries);
		}
		List<string> list = new List<string>();
		string[] array = searchFrames;
		for (int i = 0; i < array.Length; i++)
		{
			string text = NetDbgStackFrame.CleanSearchFrameText(array[i], trimManagedModuleAndRuntimeFrameInfo: false);
			if (!text.StartsWith("["))
			{
				list.Add(text);
			}
		}
		List<NetDbgStackFrame> list2 = new List<NetDbgStackFrame>();
		foreach (NetDbgStackFrame stackFrame in StackFrames)
		{
			if (stackFrame.InstructionAddress != 0.0)
			{
				list2.Add(stackFrame);
			}
		}
		for (int j = 0; j < list2.Count && list2.Count >= j + list.Count; j++)
		{
			bool flag = true;
			for (int k = 0; k < list.Count; k++)
			{
				if (!list2[j + k].IsTextInFrame(list[k], ignoreCase, searchOption))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return j;
			}
		}
		return -1;
	}

	/// <summary>
	/// This property returns the value of a specific register for this thread context.
	/// </summary>
	/// <param name="registerName">Rame of the CPU register</param>
	/// <returns>Double containing the value found on the resgiter</returns>
	/// <example>
	/// <code language="cs">
	///
	/// </code>
	/// </example>
	public double GetRegister(string registerName)
	{
		return this[registerName];
	}

	/// <summary>
	/// This method will return the amount of time this thread has executed in user mode since it has started, if extended thread information is available, 
	/// otherwise the values returned will be zero.  Call <c>NetDbgObj.ExtendedThreadInfoAvailable</c> to determine if extended thread information is available.
	/// </summary>
	/// <param name="pvtDays">Output parameter that contains the number of days spent on user time</param>
	/// <param name="pvtHours">Output parameter that contains the number of hours spent on user time</param>
	/// <param name="pvtMinutes">Output parameter that contains the number of minutes spent on user time</param>
	/// <param name="pvtSeconds">Output parameter that contains the number of seconds spent on user time</param>
	/// <param name="pvtMilliSeconds">Output parameter that contains the number of milliseconds spent on user time</param>
	public void GetUserTime(out object pvtDays, out object pvtHours, out object pvtMinutes, out object pvtSeconds, out object pvtMilliSeconds)
	{
		_legacyThread.GetUserTime(out pvtDays, out pvtHours, out pvtMinutes, out pvtSeconds, out pvtMilliSeconds);
	}

	/// <summary>
	/// This method will return the amount of time this thread has executed in kernel mode since it has started, if extended thread information is available, 
	/// otherwise the values returned will be zero.  Call <c>NetDbgObj.ExtendedThreadInfoAvailable</c> to determine if extended thread information is available.
	/// </summary>
	/// <param name="pvtDays">Output parameter that contains the number of days spent on user time</param>
	/// <param name="pvtHours">Output parameter that contains the number of hours spent on user time</param>
	/// <param name="pvtMinutes">Output parameter that contains the number of minutes spent on user time</param>
	/// <param name="pvtSeconds">Output parameter that contains the number of seconds spent on user time</param>
	/// <param name="pvtMilliSeconds">Output parameter that contains the number of milliseconds spent on user time</param>
	public void GetKernelTime(out object pvtDays, out object pvtHours, out object pvtMinutes, out object pvtSeconds, out object pvtMilliSeconds)
	{
		_legacyThread.GetKernelTime(out pvtDays, out pvtHours, out pvtMinutes, out pvtSeconds, out pvtMilliSeconds);
	}

	/// <summary>
	/// This method changes the current thread context to the thread context at the address passed into the function.
	/// </summary>
	/// <param name="ContextAddress">The method will return True if this succeeds, or False if it does not.</param>
	/// <returns></returns>
	public bool ChangeThreadContext(double ContextAddress)
	{
		return _legacyThread.ChangeThreadContext(ContextAddress);
	}

	/// <summary>
	/// This property can be called to clear the currently loaded stack frames for the active thread from the memory. 
	/// </summary>
	public void FlushStackFrames()
	{
		_legacyThread.FlushStackFrames();
		_mixedStackFrames = null;
		_nativeStackFrames = null;
	}

	/// <summary>
	/// This method walks pointers in the specified address range and returns all the strings which begin with the specified value
	/// </summary>
	/// <param name="searchStringContentsStart">The beginning value of the string to be found</param>
	/// <param name="caseSensitive"></param>
	/// <param name="startAddress">The beginning of the address range to be searched</param>
	/// <param name="endAddress">The end of the address range to be searched</param>
	/// <returns>List of strings with pointers in the specified address range which begin with the specified value</returns>
	public List<string> StringSearch(string searchStringContentsStart, ulong startAddress = 0uL, ulong endAddress = 0uL, bool caseSensitive = true)
	{
		if (startAddress * endAddress == 0L && startAddress + endAddress != 0L)
		{
			throw new ArgumentException("startAddress and endAddress must either be both 0, or both nonzero");
		}
		if (startAddress == 0L)
		{
			IDebugSystemObjects2 obj = _debugger.RawDebugger as IDebugSystemObjects2;
			ulong Offset = 0uL;
			obj.GetCurrentThreadTeb(out Offset);
			if (_debugger.Is32Bit)
			{
				startAddress = _debugger.ReadDWord(Offset + 8);
				endAddress = _debugger.ReadDWord(Offset + 4);
			}
			else
			{
				startAddress = _debugger.ReadDWord(Offset + 16);
				endAddress = _debugger.ReadDWord(Offset + 8);
			}
		}
		return null;
	}
}
