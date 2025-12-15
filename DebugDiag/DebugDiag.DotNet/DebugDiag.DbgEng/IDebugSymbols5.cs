using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("c65fa83e-1e69-475e-8e0e-b5d79e9cc17e")]
public interface IDebugSymbols5 : IDebugSymbols4, IDebugSymbols3, IDebugSymbols2, IDebugSymbols
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
	new unsafe int GetModuleVersionInformation([In] uint Index, [In] ulong Base, [In][MarshalAs(UnmanagedType.LPStr)] string Item, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* VerInfoSize);

	[PreserveSig]
	new unsafe int GetModuleNameString([In] DEBUG_MODNAME Which, [In] uint Index, [In] ulong Base, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new unsafe int GetConstantName([In] ulong Module, [In] uint TypeId, [In] ulong Value, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new unsafe int GetFieldName([In] ulong Module, [In] uint TypeId, [In] uint FieldIndex, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new int GetTypeOptions(out DEBUG_TYPEOPTS Options);

	[PreserveSig]
	new int AddTypeOptions([In] DEBUG_TYPEOPTS Options);

	[PreserveSig]
	new int RemoveTypeOptions([In] DEBUG_TYPEOPTS Options);

	[PreserveSig]
	new int SetTypeOptions([In] DEBUG_TYPEOPTS Options);

	[PreserveSig]
	new unsafe int GetNameByOffsetWide([In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new int GetOffsetByNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Symbol, out ulong Offset);

	[PreserveSig]
	new unsafe int GetNearNameByOffsetWide([In] ulong Offset, [In] int Delta, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new unsafe int GetLineByOffsetWide([In] ulong Offset, [In] uint* Line, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder FileBuffer, [In] int FileBufferSize, [In] uint* FileSize, [In] ulong* Displacement);

	[PreserveSig]
	new int GetOffsetByLineWide([In] uint Line, [In][MarshalAs(UnmanagedType.LPWStr)] string File, out ulong Offset);

	[PreserveSig]
	new unsafe int GetModuleByModuleNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Name, [In] uint StartIndex, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new int GetSymbolModuleWide([In][MarshalAs(UnmanagedType.LPWStr)] string Symbol, out ulong Base);

	[PreserveSig]
	new unsafe int GetTypeNameWide([In] ulong Module, [In] uint TypeId, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize);

	[PreserveSig]
	new int GetTypeIdWide([In] ulong Module, [In][MarshalAs(UnmanagedType.LPWStr)] string Name, out uint TypeId);

	[PreserveSig]
	new int GetFieldOffsetWide([In] ulong Module, [In] uint TypeId, [In][MarshalAs(UnmanagedType.LPWStr)] string Field, out uint Offset);

	[PreserveSig]
	new unsafe int GetSymbolTypeIdWide([In][MarshalAs(UnmanagedType.LPWStr)] string Symbol, out uint TypeId, [In] ulong* Module);

	[PreserveSig]
	new int GetScopeSymbolGroup2([In] DEBUG_SCOPE_GROUP Flags, [In][MarshalAs(UnmanagedType.Interface)] IDebugSymbolGroup2 Update, [MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup2 Symbols);

	[PreserveSig]
	new int CreateSymbolGroup2([MarshalAs(UnmanagedType.Interface)] out IDebugSymbolGroup2 Group);

	[PreserveSig]
	new int StartSymbolMatchWide([In][MarshalAs(UnmanagedType.LPWStr)] string Pattern, out ulong Handle);

	[PreserveSig]
	new unsafe int GetNextSymbolMatchWide([In] ulong Handle, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* MatchSize, [In] ulong* Offset);

	[PreserveSig]
	new int ReloadWide([In][MarshalAs(UnmanagedType.LPWStr)] string Module);

	[PreserveSig]
	new unsafe int GetSymbolPathWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new int SetSymbolPathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Path);

	[PreserveSig]
	new int AppendSymbolPathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Addition);

	[PreserveSig]
	new unsafe int GetImagePathWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new int SetImagePathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Path);

	[PreserveSig]
	new int AppendImagePathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Addition);

	[PreserveSig]
	new unsafe int GetSourcePathWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* PathSize);

	[PreserveSig]
	new unsafe int GetSourcePathElementWide([In] uint Index, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* ElementSize);

	[PreserveSig]
	new int SetSourcePathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Path);

	[PreserveSig]
	new int AppendSourcePathWide([In][MarshalAs(UnmanagedType.LPWStr)] string Addition);

	[PreserveSig]
	new unsafe int FindSourceFileWide([In] uint StartElement, [In][MarshalAs(UnmanagedType.LPWStr)] string File, [In] DEBUG_FIND_SOURCE Flags, [In] uint* FoundElement, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* FoundSize);

	[PreserveSig]
	new unsafe int GetSourceFileLineOffsetsWide([In][MarshalAs(UnmanagedType.LPWStr)] string File, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Buffer, [In] int BufferLines, [In] uint* FileLines);

	[PreserveSig]
	new unsafe int GetModuleVersionInformationWide([In] uint Index, [In] ulong Base, [In][MarshalAs(UnmanagedType.LPWStr)] string Item, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* VerInfoSize);

	[PreserveSig]
	new unsafe int GetModuleNameStringWide([In] DEBUG_MODNAME Which, [In] uint Index, [In] ulong Base, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new unsafe int GetConstantNameWide([In] ulong Module, [In] uint TypeId, [In] ulong Value, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new unsafe int GetFieldNameWide([In] ulong Module, [In] uint TypeId, [In] uint FieldIndex, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new int IsManagedModule([In] uint Index, [In] ulong Base);

	[PreserveSig]
	new unsafe int GetModuleByModuleName2([In][MarshalAs(UnmanagedType.LPStr)] string Name, [In] uint StartIndex, [In] DEBUG_GETMOD Flags, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new unsafe int GetModuleByModuleName2Wide([In][MarshalAs(UnmanagedType.LPWStr)] string Name, [In] uint StartIndex, [In] DEBUG_GETMOD Flags, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new unsafe int GetModuleByOffset2([In] ulong Offset, [In] uint StartIndex, [In] DEBUG_GETMOD Flags, [In] uint* Index, [In] ulong* Base);

	[PreserveSig]
	new int AddSyntheticModule([In] ulong Base, [In] uint Size, [In][MarshalAs(UnmanagedType.LPStr)] string ImagePath, [In][MarshalAs(UnmanagedType.LPStr)] string ModuleName, [In] DEBUG_ADDSYNTHMOD Flags);

	[PreserveSig]
	new int AddSyntheticModuleWide([In] ulong Base, [In] uint Size, [In][MarshalAs(UnmanagedType.LPWStr)] string ImagePath, [In][MarshalAs(UnmanagedType.LPWStr)] string ModuleName, [In] DEBUG_ADDSYNTHMOD Flags);

	[PreserveSig]
	new int RemoveSyntheticModule([In] ulong Base);

	[PreserveSig]
	new int GetCurrentScopeFrameIndex(out uint Index);

	[PreserveSig]
	new int SetScopeFrameByIndex([In] uint Index);

	[PreserveSig]
	new int SetScopeFromJitDebugInfo([In] uint OutputControl, [In] ulong InfoOffset);

	[PreserveSig]
	new int SetScopeFromStoredEvent();

	[PreserveSig]
	new int OutputSymbolByOffset([In] uint OutputControl, [In] DEBUG_OUTSYM Flags, [In] ulong Offset);

	[PreserveSig]
	new unsafe int GetFunctionEntryByOffset([In] ulong Offset, [In] DEBUG_GETFNENT Flags, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BufferNeeded);

	[PreserveSig]
	new unsafe int GetFieldTypeAndOffset([In] ulong Module, [In] uint ContainerTypeId, [In][MarshalAs(UnmanagedType.LPStr)] string Field, [In] uint* FieldTypeId, [In] uint* Offset);

	[PreserveSig]
	new unsafe int GetFieldTypeAndOffsetWide([In] ulong Module, [In] uint ContainerTypeId, [In][MarshalAs(UnmanagedType.LPWStr)] string Field, [In] uint* FieldTypeId, [In] uint* Offset);

	[PreserveSig]
	new unsafe int AddSyntheticSymbol([In] ulong Offset, [In] uint Size, [In][MarshalAs(UnmanagedType.LPStr)] string Name, [In] DEBUG_ADDSYNTHSYM Flags, [In] DEBUG_MODULE_AND_ID* Id);

	[PreserveSig]
	new unsafe int AddSyntheticSymbolWide([In] ulong Offset, [In] uint Size, [In][MarshalAs(UnmanagedType.LPWStr)] string Name, [In] DEBUG_ADDSYNTHSYM Flags, [In] DEBUG_MODULE_AND_ID* Id);

	[PreserveSig]
	new int RemoveSyntheticSymbol([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id);

	[PreserveSig]
	new unsafe int GetSymbolEntriesByOffset([In] ulong Offset, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_MODULE_AND_ID[] Ids, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Displacements, [In] uint IdsCount, [In] uint* Entries);

	[PreserveSig]
	new unsafe int GetSymbolEntriesByName([In][MarshalAs(UnmanagedType.LPStr)] string Symbol, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_MODULE_AND_ID[] Ids, [In] uint IdsCount, [In] uint* Entries);

	[PreserveSig]
	new unsafe int GetSymbolEntriesByNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Symbol, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_MODULE_AND_ID[] Ids, [In] uint IdsCount, [In] uint* Entries);

	[PreserveSig]
	new int GetSymbolEntryByToken([In] ulong ModuleBase, [In] uint Token, out DEBUG_MODULE_AND_ID Id);

	[PreserveSig]
	new int GetSymbolEntryInformation([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id, out DEBUG_SYMBOL_ENTRY Info);

	[PreserveSig]
	new unsafe int GetSymbolEntryString([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id, [In] uint Which, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	new unsafe int GetSymbolEntryStringWide([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id, [In] uint Which, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	new unsafe int GetSymbolEntryOffsetRegions([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_OFFSET_REGION[] Regions, [In] uint RegionsCount, [In] uint* RegionsAvail);

	[PreserveSig]
	new int GetSymbolEntryBySymbolEntry([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID FromId, [In] uint Flags, out DEBUG_MODULE_AND_ID ToId);

	[PreserveSig]
	new unsafe int GetSourceEntriesByOffset([In] ulong Offset, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_SYMBOL_SOURCE_ENTRY[] Entries, [In] uint EntriesCount, [In] uint* EntriesAvail);

	[PreserveSig]
	new unsafe int GetSourceEntriesByLine([In] uint Line, [In][MarshalAs(UnmanagedType.LPStr)] string File, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_SYMBOL_SOURCE_ENTRY[] Entries, [In] uint EntriesCount, [In] uint* EntriesAvail);

	[PreserveSig]
	new unsafe int GetSourceEntriesByLineWide([In] uint Line, [In][MarshalAs(UnmanagedType.LPWStr)] string File, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_SYMBOL_SOURCE_ENTRY[] Entries, [In] uint EntriesCount, [In] uint* EntriesAvail);

	[PreserveSig]
	new unsafe int GetSourceEntryString([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_SYMBOL_SOURCE_ENTRY Entry, [In] uint Which, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	new unsafe int GetSourceEntryStringWide([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_SYMBOL_SOURCE_ENTRY Entry, [In] uint Which, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* StringSize);

	[PreserveSig]
	new unsafe int GetSourceEntryOffsetRegions([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_SYMBOL_SOURCE_ENTRY Entry, [In] uint Flags, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_OFFSET_REGION[] Regions, [In] uint RegionsCount, [In] uint* RegionsAvail);

	[PreserveSig]
	new int GetSourceEntryBySourceEntry([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_SYMBOL_SOURCE_ENTRY FromEntry, [In] uint Flags, out DEBUG_SYMBOL_SOURCE_ENTRY ToEntry);

	[PreserveSig]
	new unsafe int GetScopeEx([In] ulong* InstructionOffset, [In] DEBUG_STACK_FRAME_EX* ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	new int SetScopeEx([In] ulong InstructionOffset, [In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_STACK_FRAME_EX ScopeFrame, [In] IntPtr ScopeContext, [In] uint ScopeContextSize);

	[PreserveSig]
	new unsafe int GetNameByInlineContext([In] ulong Offset, [In] uint InlineContext, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new unsafe int GetNameByInlineContextWide([In] ulong Offset, [In] uint InlineContext, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* Displacement);

	[PreserveSig]
	new unsafe int GetLineByInlineContext([In] ulong Offset, [In] uint InlineContext, [In] uint* Line, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder FileBuffer, [In] int FileBufferSize, [In] uint* FileSize, [In] ulong* Displacement);

	[PreserveSig]
	new unsafe int GetLineByInlineContextWide([In] ulong Offset, [In] uint InlineContext, [In] uint* Line, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder FileBuffer, [In] int FileBufferSize, [In] uint* FileSize, [In] ulong* Displacement);

	[PreserveSig]
	new int OutputSymbolByInlineContext([In] uint OutputControl, [In] uint Flags, [In] ulong Offset, [In] uint InlineContext);

	[PreserveSig]
	int GetCurrentScopeFrameIndexEx([In] DEBUG_FRAME Flags, out uint Index);

	[PreserveSig]
	int SetScopeFrameByIndexEx([In] DEBUG_FRAME Flags, [In] uint Index);
}
