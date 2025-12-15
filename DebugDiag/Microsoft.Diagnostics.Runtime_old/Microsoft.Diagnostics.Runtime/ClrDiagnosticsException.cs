using System;

namespace Microsoft.Diagnostics.Runtime;

public class ClrDiagnosticsException : Exception
{
	public enum HR
	{
		UnknownError = -2128281600,
		RuntimeUninitialized,
		DebuggerError,
		DataRequestError,
		DacError,
		RevisionError,
		CrashDumpError,
		ApplicationError
	}

	public new int HResult => base.HResult;

	internal ClrDiagnosticsException(string message)
		: base(message)
	{
		base.HResult = -2128281600;
	}

	internal ClrDiagnosticsException(string message, HR hr)
		: base(message)
	{
		base.HResult = (int)hr;
	}

	internal static void ThrowRevisionError(int revision, int runtimeRevision)
	{
		throw new ClrDiagnosticsException($"You must not reuse any object other than ClrRuntime after calling flush!\nClrModule revision ({revision}) != ClrRuntime revision ({runtimeRevision}).", HR.RevisionError);
	}
}
