using System;

namespace Microsoft.Diagnostics.Runtime.Interop;

public struct WINDBG_EXTENSION_APIS
{
	public uint nSize;

	public IntPtr lpOutputRoutine;

	public IntPtr lpGetExpressionRoutine;

	public IntPtr lpGetSymbolRoutine;

	public IntPtr lpDisasmRoutine;

	public IntPtr lpCheckControlCRoutine;

	public IntPtr lpReadProcessMemoryRoutine;

	public IntPtr lpWriteProcessMemoryRoutine;

	public IntPtr lpGetThreadContextRoutine;

	public IntPtr lpSetThreadContextRoutine;

	public IntPtr lpIoctlRoutine;

	public IntPtr lpStackTraceRoutine;
}
