using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("dd492d7f-71b8-4ad6-a8dc-1c887479ff91")]
public interface IDebugClient3 : IDebugClient2, IDebugClient
{
	[PreserveSig]
	new int AttachKernel([In] DEBUG_ATTACH Flags, [In][MarshalAs(UnmanagedType.LPStr)] string ConnectOptions);

	[PreserveSig]
	new unsafe int GetKernelConnectionOptions([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* OptionsSize);

	[PreserveSig]
	new int SetKernelConnectionOptions([In][MarshalAs(UnmanagedType.LPStr)] string Options);

	[PreserveSig]
	new int StartProcessServer([In] DEBUG_CLASS Flags, [In][MarshalAs(UnmanagedType.LPStr)] string Options, [In] IntPtr Reserved);

	[PreserveSig]
	new int ConnectProcessServer([In][MarshalAs(UnmanagedType.LPStr)] string RemoteOptions, out ulong Server);

	[PreserveSig]
	new int DisconnectProcessServer([In] ulong Server);

	[PreserveSig]
	new unsafe int GetRunningProcessSystemIds([In] ulong Server, [Out][MarshalAs(UnmanagedType.LPArray)] uint[] Ids, [In] uint Count, [In] uint* ActualCount);

	[PreserveSig]
	new int GetRunningProcessSystemIdByExecutableName([In] ulong Server, [In][MarshalAs(UnmanagedType.LPStr)] string ExeName, [In] DEBUG_GET_PROC Flags, out uint Id);

	[PreserveSig]
	new unsafe int GetRunningProcessDescription([In] ulong Server, [In] uint SystemId, [In] DEBUG_PROC_DESC Flags, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ExeName, [In] int ExeNameSize, [In] uint* ActualExeNameSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Description, [In] int DescriptionSize, [In] uint* ActualDescriptionSize);

	[PreserveSig]
	new int AttachProcess([In] ulong Server, [In] uint ProcessID, [In] DEBUG_ATTACH AttachFlags);

	[PreserveSig]
	new int CreateProcess([In] ulong Server, [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS Flags);

	[PreserveSig]
	new int CreateProcessAndAttach([In] ulong Server, [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS Flags, [In] uint ProcessId, [In] DEBUG_ATTACH AttachFlags);

	[PreserveSig]
	new int GetProcessOptions(out DEBUG_PROCESS Options);

	[PreserveSig]
	new int AddProcessOptions([In] DEBUG_PROCESS Options);

	[PreserveSig]
	new int RemoveProcessOptions([In] DEBUG_PROCESS Options);

	[PreserveSig]
	new int SetProcessOptions([In] DEBUG_PROCESS Options);

	[PreserveSig]
	new int OpenDumpFile([In][MarshalAs(UnmanagedType.LPStr)] string DumpFile);

	[PreserveSig]
	new int WriteDumpFile([In][MarshalAs(UnmanagedType.LPStr)] string DumpFile, [In] DEBUG_DUMP Qualifier);

	[PreserveSig]
	new int ConnectSession([In] DEBUG_CONNECT_SESSION Flags, [In] uint HistoryLimit);

	[PreserveSig]
	new int StartServer([In][MarshalAs(UnmanagedType.LPStr)] string Options);

	[PreserveSig]
	new int OutputServer([In] DEBUG_OUTCTL OutputControl, [In][MarshalAs(UnmanagedType.LPStr)] string Machine, [In] DEBUG_SERVERS Flags);

	[PreserveSig]
	new int TerminateProcesses();

	[PreserveSig]
	new int DetachProcesses();

	[PreserveSig]
	new int EndSession([In] DEBUG_END Flags);

	[PreserveSig]
	new int GetExitCode(out uint Code);

	[PreserveSig]
	new int DispatchCallbacks([In] uint Timeout);

	[PreserveSig]
	new int ExitDispatch([In][MarshalAs(UnmanagedType.Interface)] IDebugClient Client);

	[PreserveSig]
	new int CreateClient([MarshalAs(UnmanagedType.Interface)] out IDebugClient Client);

	[PreserveSig]
	new int GetInputCallbacks([MarshalAs(UnmanagedType.Interface)] out IDebugInputCallbacks Callbacks);

	[PreserveSig]
	new int SetInputCallbacks([In][MarshalAs(UnmanagedType.Interface)] IDebugInputCallbacks Callbacks);

	[PreserveSig]
	new int GetOutputCallbacks(out IntPtr Callbacks);

	[PreserveSig]
	new int SetOutputCallbacks([In] IntPtr Callbacks);

	[PreserveSig]
	new int GetOutputMask(out DEBUG_OUTPUT Mask);

	[PreserveSig]
	new int SetOutputMask([In] DEBUG_OUTPUT Mask);

	[PreserveSig]
	new int GetOtherOutputMask([In][MarshalAs(UnmanagedType.Interface)] IDebugClient Client, out DEBUG_OUTPUT Mask);

	[PreserveSig]
	new int SetOtherOutputMask([In][MarshalAs(UnmanagedType.Interface)] IDebugClient Client, [In] DEBUG_OUTPUT Mask);

	[PreserveSig]
	new int GetOutputWidth(out uint Columns);

	[PreserveSig]
	new int SetOutputWidth([In] uint Columns);

	[PreserveSig]
	new unsafe int GetOutputLinePrefix([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PrefixSize);

	[PreserveSig]
	new int SetOutputLinePrefix([In][MarshalAs(UnmanagedType.LPStr)] string Prefix);

	[PreserveSig]
	new unsafe int GetIdentity([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* IdentitySize);

	[PreserveSig]
	new int OutputIdentity([In] DEBUG_OUTCTL OutputControl, [In] uint Flags, [In][MarshalAs(UnmanagedType.LPStr)] string Format);

	[PreserveSig]
	new int GetEventCallbacks(out IntPtr Callbacks);

	[PreserveSig]
	new int SetEventCallbacks([In] IntPtr Callbacks);

	[PreserveSig]
	new int FlushCallbacks();

	[PreserveSig]
	new int WriteDumpFile2([In][MarshalAs(UnmanagedType.LPStr)] string DumpFile, [In] DEBUG_DUMP Qualifier, [In] DEBUG_FORMAT FormatFlags, [In][MarshalAs(UnmanagedType.LPStr)] string Comment);

	[PreserveSig]
	new int AddDumpInformationFile([In][MarshalAs(UnmanagedType.LPStr)] string InfoFile, [In] DEBUG_DUMP_FILE Type);

	[PreserveSig]
	new int EndProcessServer([In] ulong Server);

	[PreserveSig]
	new int WaitForProcessServerEnd([In] uint Timeout);

	[PreserveSig]
	new int IsKernelDebuggerEnabled();

	[PreserveSig]
	new int TerminateCurrentProcess();

	[PreserveSig]
	new int DetachCurrentProcess();

	[PreserveSig]
	new int AbandonCurrentProcess();

	[PreserveSig]
	int GetRunningProcessSystemIdByExecutableNameWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string ExeName, [In] DEBUG_GET_PROC Flags, out uint Id);

	[PreserveSig]
	unsafe int GetRunningProcessDescriptionWide([In] ulong Server, [In] uint SystemId, [In] DEBUG_PROC_DESC Flags, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder ExeName, [In] int ExeNameSize, [In] uint* ActualExeNameSize, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Description, [In] int DescriptionSize, [In] uint* ActualDescriptionSize);

	[PreserveSig]
	int CreateProcessWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS CreateFlags);

	[PreserveSig]
	int CreateProcessAndAttachWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS CreateFlags, [In] uint ProcessId, [In] DEBUG_ATTACH AttachFlags);
}
