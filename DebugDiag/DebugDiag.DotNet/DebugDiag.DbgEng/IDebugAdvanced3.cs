using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("cba4abb4-84c4-444d-87ca-a04e13286739")]
public interface IDebugAdvanced3 : IDebugAdvanced2, IDebugAdvanced
{
	[PreserveSig]
	new int GetThreadContext([In] IntPtr Context, [In] uint ContextSize);

	[PreserveSig]
	new int SetThreadContext([In] IntPtr Context, [In] uint ContextSize);

	[PreserveSig]
	new unsafe int Request([In] DEBUG_REQUEST Request, [In] void* InBuffer, [In] int InBufferSize, [In] void* OutBuffer, [In] int OutBufferSize, [In] int* OutSize);

	[PreserveSig]
	new unsafe int GetSourceFileInformation([In] DEBUG_SRCFILE Which, [In][MarshalAs(UnmanagedType.LPStr)] string SourceFile, [In] ulong Arg64, [In] uint Arg32, [In] void* Buffer, [In] int BufferSize, [In] int* InfoSize);

	[PreserveSig]
	new unsafe int FindSourceFileAndToken([In] uint StartElement, [In] ulong ModAddr, [In][MarshalAs(UnmanagedType.LPStr)] string File, [In] DEBUG_FIND_SOURCE Flags, [In] void* FileToken, [In] int FileTokenSize, [In] int* FoundElement, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] int* FoundSize);

	[PreserveSig]
	new unsafe int GetSymbolInformation([In] DEBUG_SYMINFO Which, [In] ulong Arg64, [In] uint Arg32, [In] void* Buffer, [In] int BufferSize, [In] int* InfoSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder StringBuffer, [In] int StringBufferSize, [In] int* StringSize);

	[PreserveSig]
	new unsafe int GetSystemObjectInformation([In] DEBUG_SYSOBJINFO Which, [In] ulong Arg64, [In] uint Arg32, [In] void* Buffer, [In] int BufferSize, [In] int* InfoSize);

	[PreserveSig]
	unsafe int GetSourceFileInformationWide([In] DEBUG_SRCFILE Which, [In][MarshalAs(UnmanagedType.LPWStr)] string SourceFile, [In] ulong Arg64, [In] uint Arg32, [In] void* Buffer, [In] int BufferSize, [In] int* InfoSize);

	[PreserveSig]
	unsafe int FindSourceFileAndTokenWide([In] uint StartElement, [In] ulong ModAddr, [In][MarshalAs(UnmanagedType.LPWStr)] string File, [In] DEBUG_FIND_SOURCE Flags, [In] void* FileToken, [In] int FileTokenSize, [In] int* FoundElement, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] int* FoundSize);

	[PreserveSig]
	unsafe int GetSymbolInformationWide([In] DEBUG_SYMINFO Which, [In] ulong Arg64, [In] uint Arg32, [In] void* Buffer, [In] int BufferSize, [In] int* InfoSize, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder StringBuffer, [In] int StringBufferSize, [In] int* StringSize);
}
