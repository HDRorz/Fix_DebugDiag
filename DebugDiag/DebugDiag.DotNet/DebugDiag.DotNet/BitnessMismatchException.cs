using System;

namespace DebugDiag.DotNet;

/// <summary>
/// This exception is thrown if for any reason the debugger Bitness is different that the process being debugged ie. The Debugger is 64 bits and process is 32 bits.
/// </summary>
public class BitnessMismatchException : Exception
{
	/// <summary>
	/// Default constructor
	/// </summary>
	/// <param name="message">"Message to be added on the exception"</param>
	public BitnessMismatchException(string message)
		: base(message)
	{
	}
}
