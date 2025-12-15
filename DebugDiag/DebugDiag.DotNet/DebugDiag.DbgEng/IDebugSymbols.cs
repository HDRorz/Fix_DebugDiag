using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("8c31e98c-983a-48a5-9016-6fe5d667a950")]
public interface IDebugSymbols
{
	[PreserveSig]
	int GetSymbolOptions(out SYMOPT Options);

	[PreserveSig]
	int AddSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	int RemoveSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	int SetSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	unsafe int GetNameByOffset([In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	int GetOffsetByName([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out ulong Offset);

	[PreserveSig]
	unsafe int GetNearNameByOffset([In] ulong Offset, [In] int Delta, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	unsafe int GetLineByOffset([In] ulong Offset, [In] uint* Line, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder FileBuffer, [In] int FileBufferSize, [In] uint* FileSize, [In] ulong* Displacement);

	[PreserveSig]
	int GetOffsetByLine([In] uint Line, [In][MarshalAs(UnmanagedType.LPStr)] string File, out ulong Offset);

	[PreserveSig]
	int GetNumberModules(out uint Loaded, out uint Unloaded);

	[PreserveSig]
	int GetModuleByIndex([In] uint Index, out ulong Base);

	[PreserveSig]
	unsafe int GetModuleByModuleName([In][MarshalAs(UnmanagedType.LPStr)] string Name, [In] uint StartIndex, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	unsafe int GetModuleByOffset([In] ulong Offset, [In] uint StartIndex, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	unsafe int GetModuleNames([In] uint Index, [In] ulong Base, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ImageNameBuffer, [In] int ImageNameBufferSize, [In] uint* ImageNameSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ModuleNameBuffer, [In] int ModuleNameBufferSize, [In] uint* ModuleNameSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder LoadedImageNameBuffer, [In] int LoadedImageNameBufferSize, [In] uint* LoadedImageNameSize);

	[PreserveSig]
	int GetModuleParameters([In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] ulong[] Bases, [In] uint Start, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_MODULE_PARAMETERS[] Params);

	[PreserveSig]
	int GetSymbolModule([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out ulong Base);

	[PreserveSig]
	unsafe int GetTypeName([In] ulong Module, [In] uint TypeId, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize);

	[PreserveSig]
	int GetTypeId([In] ulong Module, [In][MarshalAs(UnmanagedType.LPStr)] string Name, out uint TypeId);

	[PreserveSig]
	int GetTypeSize([In] ulong Module, [In] uint TypeId, out uint Size);

	[PreserveSig]
	int GetFieldOffset([In] ulong Module, [In] uint TypeId, [In][MarshalAs(UnmanagedType.LPStr)] string Field, out uint Offset);

	[PreserveSig]
	unsafe int GetSymbolTypeId([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out uint TypeId, [In] ulong* Module);

	[PreserveSig]
	unsafe int GetOffsetTypeId([In] ulong Offset, out uint TypeId, [In] ulong* Module);

	[PreserveSig]
	unsafe int ReadTypedDataVirtual([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteTypedDataVirtual([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int OutputTypedDataVirtual([In] DEBUG_OUTCTL OutputControl, [In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] DEBUG_TYPEOPTS Flags);

	[PreserveSig]
	unsafe int ReadTypedDataPhysical([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteTypedDataPhysical([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int OutputTypedDataPhysical([In] DEBUG_OUTCTL OutputControl, [In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] DEBUG_TYPEOPTS Flags);

	[PreserveSig]
	unsafe int GetScope([In] ulong* InstructionOffset, [In] DEBUG_STACK_FRAME* ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	int SetScope([In] ulong InstructionOffset, [In] DEBUG_STACK_FRAME ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	int ResetScope();

	[PreserveSig]
	int GetScopeSymbolGroup([In] DEBUG_SCOPE_GROUP Flags, [In][MarshalAs(UnmanagedType.Interface)] IDebugSymbolGroup Update, [MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup Symbols);

	[PreserveSig]
	int CreateSymbolGroup([MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup Group);

	[PreserveSig]
	int StartSymbolMatch([In][MarshalAs(UnmanagedType.LPStr)] string Pattern, out ulong Handle);

	[PreserveSig]
	unsafe int GetNextSymbolMatch([In] ulong Handle, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* MatchSize, [In] ulong* Offset);

	[PreserveSig]
	int EndSymbolMatch([In] ulong Handle);

	[PreserveSig]
	int Reload([In][MarshalAs(UnmanagedType.LPStr)] string Module);

	[PreserveSig]
	unsafe int GetSymbolPath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	int SetSymbolPath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	int AppendSymbolPath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	unsafe int GetImagePath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	int SetImagePath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	int AppendImagePath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	unsafe int GetSourcePath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	unsafe int GetSourcePathElement([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* ElementSize);

	[PreserveSig]
	int SetSourcePath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	int AppendSourcePath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	unsafe int FindSourceFile([In] uint StartElement, [In][MarshalAs(UnmanagedType.LPStr)] string File, [In] DEBUG_FIND_SOURCE Flags, [In] uint* FoundElement, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* FoundSize);

	[PreserveSig]
	unsafe int GetSourceFileLineOffsets([In][MarshalAs(UnmanagedType.LPStr)] string File, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Buffer, [In] int BufferLines, [In] uint* FileLines);
}
