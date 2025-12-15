using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("e3acb9d7-7ec2-4f0c-a0da-e81e0cbbe628")]
public interface IDebugClient5 : IDebugClient4, IDebugClient3, IDebugClient2, IDebugClient
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
	new int GetRunningProcessSystemIdByExecutableNameWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string ExeName, [In] DEBUG_GET_PROC Flags, out uint Id);

	[PreserveSig]
	new unsafe int GetRunningProcessDescriptionWide([In] ulong Server, [In] uint SystemId, [In] DEBUG_PROC_DESC Flags, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder ExeName, [In] int ExeNameSize, [In] uint* ActualExeNameSize, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Description, [In] int DescriptionSize, [In] uint* ActualDescriptionSize);

	[PreserveSig]
	new int CreateProcessWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS CreateFlags);

	[PreserveSig]
	new int CreateProcessAndAttachWide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] DEBUG_CREATE_PROCESS CreateFlags, [In] uint ProcessId, [In] DEBUG_ATTACH AttachFlags);

	[PreserveSig]
	new int OpenDumpFileWide([In][MarshalAs(UnmanagedType.LPWStr)] string FileName, [In] ulong FileHandle);

	[PreserveSig]
	new int WriteDumpFileWide([In][MarshalAs(UnmanagedType.LPWStr)] string DumpFile, [In] ulong FileHandle, [In] DEBUG_DUMP Qualifier, [In] DEBUG_FORMAT FormatFlags, [In][MarshalAs(UnmanagedType.LPWStr)] string Comment);

	[PreserveSig]
	new int AddDumpInformationFileWide([In][MarshalAs(UnmanagedType.LPWStr)] string FileName, [In] ulong FileHandle, [In] DEBUG_DUMP_FILE Type);

	[PreserveSig]
	new int GetNumberDumpFiles(out uint Number);

	[PreserveSig]
	new unsafe int GetDumpFile([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize, [In] ulong* Handle, out uint Type);

	[PreserveSig]
	new unsafe int GetDumpFileWide([In] uint Index, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize, [In] ulong* Handle, out uint Type);

	[PreserveSig]
	int AttachKernelWide([In] DEBUG_ATTACH Flags, [In][MarshalAs(UnmanagedType.LPWStr)] string ConnectOptions);

	[PreserveSig]
	unsafe int GetKernelConnectionOptionsWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* OptionsSize);

	[PreserveSig]
	int SetKernelConnectionOptionsWide([In][MarshalAs(UnmanagedType.LPWStr)] string Options);

	[PreserveSig]
	int StartProcessServerWide([In] DEBUG_CLASS Flags, [In][MarshalAs(UnmanagedType.LPWStr)] string Options, [In] IntPtr Reserved);

	[PreserveSig]
	int ConnectProcessServerWide([In][MarshalAs(UnmanagedType.LPWStr)] string RemoteOptions, out ulong Server);

	[PreserveSig]
	int StartServerWide([In][MarshalAs(UnmanagedType.LPWStr)] string Options);

	[PreserveSig]
	int OutputServersWide([In] DEBUG_OUTCTL OutputControl, [In][MarshalAs(UnmanagedType.LPWStr)] string Machine, [In] DEBUG_SERVERS Flags);

	[PreserveSig]
	int GetOutputCallbacksWide(out IntPtr Callbacks);

	[PreserveSig]
	int SetOutputCallbacksWide([In] IntPtr Callbacks);

	[PreserveSig]
	unsafe int GetOutputLinePrefixWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PrefixSize);

	[PreserveSig]
	int SetOutputLinePrefixWide([In][MarshalAs(UnmanagedType.LPWStr)] string Prefix);

	[PreserveSig]
	unsafe int GetIdentityWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* IdentitySize);

	[PreserveSig]
	int OutputIdentityWide([In] DEBUG_OUTCTL OutputControl, [In] uint Flags, [In][MarshalAs(UnmanagedType.LPWStr)] string Machine);

	[PreserveSig]
	int GetEventCallbacksWide(out IntPtr Callbacks);

	[PreserveSig]
	int SetEventCallbacksWide([In] IntPtr Callbacks);

	[PreserveSig]
	int CreateProcess2([In] ulong Server, [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine, [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer, [In] uint OptionsBufferSize, [In][MarshalAs(UnmanagedType.LPStr)] string InitialDirectory, [In][MarshalAs(UnmanagedType.LPStr)] string Environment);

	[PreserveSig]
	int CreateProcess2Wide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer, [In] uint OptionsBufferSize, [In][MarshalAs(UnmanagedType.LPWStr)] string InitialDirectory, [In][MarshalAs(UnmanagedType.LPWStr)] string Environment);

	[PreserveSig]
	int CreateProcessAndAttach2([In] ulong Server, [In][MarshalAs(UnmanagedType.LPStr)] string CommandLine, [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer, [In] uint OptionsBufferSize, [In][MarshalAs(UnmanagedType.LPStr)] string InitialDirectory, [In][MarshalAs(UnmanagedType.LPStr)] string Environment, [In] uint ProcessId, [In] DEBUG_ATTACH AttachFlags);

	[PreserveSig]
	int CreateProcessAndAttach2Wide([In] ulong Server, [In][MarshalAs(UnmanagedType.LPWStr)] string CommandLine, [In] ref DEBUG_CREATE_PROCESS_OPTIONS OptionsBuffer, [In] uint OptionsBufferSize, [In][MarshalAs(UnmanagedType.LPWStr)] string InitialDirectory, [In][MarshalAs(UnmanagedType.LPWStr)] string Environment, [In] uint ProcessId, [In] DEBUG_ATTACH AttachFlags);

	[PreserveSig]
	int PushOutputLinePrefix([In][MarshalAs(UnmanagedType.LPStr)] string NewPrefix, out ulong Handle);

	[PreserveSig]
	int PushOutputLinePrefixWide([In][MarshalAs(UnmanagedType.LPWStr)] string NewPrefix, out ulong Handle);

	[PreserveSig]
	int PopOutputLinePrefix([In] ulong Handle);

	[PreserveSig]
	int GetNumberInputCallbacks(out uint Count);

	[PreserveSig]
	int GetNumberOutputCallbacks(out uint Count);

	[PreserveSig]
	int GetNumberEventCallbacks([In] DEBUG_EVENT Flags, out uint Count);

	[PreserveSig]
	unsafe int GetQuitLockString([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	int SetQuitLockString([In][MarshalAs(UnmanagedType.LPStr)] string LockString);

	[PreserveSig]
	unsafe int GetQuitLockStringWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	int SetQuitLockStringWide([In][MarshalAs(UnmanagedType.LPWStr)] string LockString);
}
