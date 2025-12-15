using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3a707211-afdd-4495-ad4f-56fecdf8163f")]
public interface IDebugSymbols2 : IDebugSymbols
{
	[PreserveSig]
	new int GetSymbolOptions(out SYMOPT Options);

	[PreserveSig]
	new int AddSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	new int RemoveSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	new int SetSymbolOptions([In] SYMOPT Options);

	[PreserveSig]
	new unsafe int GetNameByOffset([In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new int GetOffsetByName([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out ulong Offset);

	[PreserveSig]
	new unsafe int GetNearNameByOffset([In] ulong Offset, [In] int Delta, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new unsafe int GetLineByOffset([In] ulong Offset, [In] uint* Line, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder FileBuffer, [In] int FileBufferSize, [In] uint* FileSize, [In] ulong* Displacement);

	[PreserveSig]
	new int GetOffsetByLine([In] uint Line, [In][MarshalAs(UnmanagedType.LPStr)] string File, out ulong Offset);

	[PreserveSig]
	new int GetNumberModules(out uint Loaded, out uint Unloaded);

	[PreserveSig]
	new int GetModuleByIndex([In] uint Index, out ulong Base);

	[PreserveSig]
	new unsafe int GetModuleByModuleName([In][MarshalAs(UnmanagedType.LPStr)] string Name, [In] uint StartIndex, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new unsafe int GetModuleByOffset([In] ulong Offset, [In] uint StartIndex, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new unsafe int GetModuleNames([In] uint Index, [In] ulong Base, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ImageNameBuffer, [In] int ImageNameBufferSize, [In] uint* ImageNameSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder ModuleNameBuffer, [In] int ModuleNameBufferSize, [In] uint* ModuleNameSize, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder LoadedImageNameBuffer, [In] int LoadedImageNameBufferSize, [In] uint* LoadedImageNameSize);

	[PreserveSig]
	new int GetModuleParameters([In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] ulong[] Bases, [In] uint Start, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_MODULE_PARAMETERS[] Params);

	[PreserveSig]
	new int GetSymbolModule([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out ulong Base);

	[PreserveSig]
	new unsafe int GetTypeName([In] ulong Module, [In] uint TypeId, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize);

	[PreserveSig]
	new int GetTypeId([In] ulong Module, [In][MarshalAs(UnmanagedType.LPStr)] string Name, out uint TypeId);

	[PreserveSig]
	new int GetTypeSize([In] ulong Module, [In] uint TypeId, out uint Size);

	[PreserveSig]
	new int GetFieldOffset([In] ulong Module, [In] uint TypeId, [In][MarshalAs(UnmanagedType.LPStr)] string Field, out uint Offset);

	[PreserveSig]
	new unsafe int GetSymbolTypeId([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, out uint TypeId, [In] ulong* Module);

	[PreserveSig]
	new unsafe int GetOffsetTypeId([In] ulong Offset, out uint TypeId, [In] ulong* Module);

	[PreserveSig]
	new unsafe int ReadTypedDataVirtual([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteTypedDataVirtual([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int OutputTypedDataVirtual([In] DEBUG_OUTCTL OutputControl, [In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] DEBUG_TYPEOPTS Flags);

	[PreserveSig]
	new unsafe int ReadTypedDataPhysical([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteTypedDataPhysical([In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int OutputTypedDataPhysical([In] DEBUG_OUTCTL OutputControl, [In] ulong Offset, [In] ulong Module, [In] uint TypeId, [In] DEBUG_TYPEOPTS Flags);

	[PreserveSig]
	new unsafe int GetScope([In] ulong* InstructionOffset, [In] DEBUG_STACK_FRAME* ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	new int SetScope([In] ulong InstructionOffset, [In] DEBUG_STACK_FRAME ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	new int ResetScope();

	[PreserveSig]
	new int GetScopeSymbolGroup([In] DEBUG_SCOPE_GROUP Flags, [In][MarshalAs(UnmanagedType.Interface)] IDebugSymbolGroup Update, [MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup Symbols);

	[PreserveSig]
	new int CreateSymbolGroup([MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup Group);

	[PreserveSig]
	new int StartSymbolMatch([In][MarshalAs(UnmanagedType.LPStr)] string Pattern, out ulong Handle);

	[PreserveSig]
	new unsafe int GetNextSymbolMatch([In] ulong Handle, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* MatchSize, [In] ulong* Offset);

	[PreserveSig]
	new int EndSymbolMatch([In] ulong Handle);

	[PreserveSig]
	new int Reload([In][MarshalAs(UnmanagedType.LPStr)] string Module);

	[PreserveSig]
	new unsafe int GetSymbolPath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new int SetSymbolPath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	new int AppendSymbolPath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	new unsafe int GetImagePath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new int SetImagePath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	new int AppendImagePath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	new unsafe int GetSourcePath([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new unsafe int GetSourcePathElement([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* ElementSize);

	[PreserveSig]
	new int SetSourcePath([In][MarshalAs(UnmanagedType.LPStr)] string Path);

	[PreserveSig]
	new int AppendSourcePath([In][MarshalAs(UnmanagedType.LPStr)] string Addition);

	[PreserveSig]
	new unsafe int FindSourceFile([In] uint StartElement, [In][MarshalAs(UnmanagedType.LPStr)] string File, [In] DEBUG_FIND_SOURCE Flags, [In] uint* FoundElement, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* FoundSize);

	[PreserveSig]
	new unsafe int GetSourceFileLineOffsets([In][MarshalAs(UnmanagedType.LPStr)] string File, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Buffer, [In] int BufferLines, [In] uint* FileLines);

	[PreserveSig]
	unsafe int GetModuleVersionInformation([In] uint Index, [In] ulong Base, [In][MarshalAs(UnmanagedType.LPStr)] string Item, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* VerInfoSize);

	[PreserveSig]
	unsafe int GetModuleNameString([In] DEBUG_MODNAME Which, [In] uint Index, [In] ulong Base, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	unsafe int GetConstantName([In] ulong Module, [In] uint TypeId, [In] ulong Value, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	unsafe int GetFieldName([In] ulong Module, [In] uint TypeId, [In] uint FieldIndex, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	int GetTypeOptions(out DEBUG_TYPEOPTS Options);

	[PreserveSig]
	int AddTypeOptions([In] DEBUG_TYPEOPTS Options);

	[PreserveSig]
	int RemoveTypeOptions([In] DEBUG_TYPEOPTS Options);

	[PreserveSig]
	int SetTypeOptions([In] DEBUG_TYPEOPTS Options);
}
