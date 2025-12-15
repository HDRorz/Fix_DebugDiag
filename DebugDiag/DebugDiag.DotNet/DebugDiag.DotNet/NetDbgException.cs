using DebugDiag.DbgLib;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents an exception record.  It is obtained by calling the <c>NetDbgObj.Exception</c> or <c>NetDbgObj.GetExceptionObjectFromAddress</c> methods of 
/// the <see cref="T:DebugDiag.DotNet.NetDbgObj" /> object.
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
///         //Gets a reference to the Native Exception that happened
///         NetDbgException exc = debugger.NativeException;
///
///         manager.WriteLine("This is a crash dump");
///         manager.WriteLine("The exception address is: " + debugger.GetAs32BitHexString(exc.ExceptionAddress));
///         manager.WriteLine("The exception number is: " + debugger.GetAs32BitHexString(exc.ExceptionCode));
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
public class NetDbgException
{
	private IDbgException _legacyException;

	private NetDbgObj _debugger;

	/// <summary>
	/// Returns a Reference for the <c>NetDbgObj</c>
	/// </summary>
	public NetDbgObj Debugger => _debugger;

	/// <summary>
	/// This property returns the exception value of the exception record.
	/// </summary>
	public double ExceptionCode => _legacyException.ExceptionCode;

	/// <summary>
	/// This property returns the exception flags of the exception record.
	/// </summary>
	public double ExceptionFlags => _legacyException.ExceptionFlags;

	public double NestedExceptionAddress => _legacyException.NestedExceptionAddress;

	/// <summary>
	/// This property returns a count of the number of parameters associated with the exception record.
	/// </summary>
	public int NumberParameters => _legacyException.NumberParameters;

	/// <summary>
	/// This property returns the address the exception occurred at.  
	/// This address can be used to determine what module caused the exception to occur by calling the <c>NetDbgObj.GetModuleByAddress</c> method.
	/// </summary>
	public double ExceptionAddress => _legacyException.ExceptionAddress;

	internal NetDbgException(IDbgException legacyException, NetDbgObj debugger)
	{
		_legacyException = legacyException;
		_debugger = debugger;
	}

	/// <summary>
	/// This property returns an exception parameter if any are available.  
	/// This property takes an integer that is a zero based index of the parameter to retrieve.  
	/// Call the <c>NetDbgException.NumberParameters</c> property to retrieve the number of parameters.
	/// </summary>
	/// <param name="index">Integer value to point the index of the parameter to retrieve</param>
	/// <returns>Double value representing the value found on the specified parameter</returns>
	public double GetExceptionParam(int index)
	{
		return _legacyException[index];
	}
}
