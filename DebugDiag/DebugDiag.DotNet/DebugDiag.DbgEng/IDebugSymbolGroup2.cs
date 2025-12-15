using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6a7ccc5f-fb5e-4dcc-b41c-6c20307bccc7")]
public interface IDebugSymbolGroup2 : IDebugSymbolGroup
{
	[PreserveSig]
	new int GetNumberSymbols(out uint Number);

	[PreserveSig]
	new int AddSymbol([In][MarshalAs(UnmanagedType.LPStr)] string Name, [In][Out] ref uint Index);

	[PreserveSig]
	new int RemoveSymbolByName([In][MarshalAs(UnmanagedType.LPStr)] string Name);

	[PreserveSig]
	new int RemoveSymbolsByIndex([In] uint Index);

	[PreserveSig]
	new unsafe int GetSymbolName([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	new int GetSymbolParameters([In] uint Start, [In] uint Count, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_SYMBOL_PARAMETERS[] Params);

	[PreserveSig]
	new int ExpandSymbol([In] uint Index, [In][MarshalAs(UnmanagedType.Bool)] bool Expand);

	[PreserveSig]
	new int OutputSymbols([In] DEBUG_OUTCTL OutputControl, [In] DEBUG_OUTPUT_SYMBOLS Flags, [In] uint Start, [In] uint Count);

	[PreserveSig]
	new int WriteSymbol([In] uint Index, [In][MarshalAs(UnmanagedType.LPStr)] string Value);

	[PreserveSig]
	new int OutputAsType([In] uint Index, [In][MarshalAs(UnmanagedType.LPStr)] string Type);

	[PreserveSig]
	int AddSymbolWide([In][MarshalAs(UnmanagedType.LPWStr)] string Name, [In][Out] ref uint Index);

	[PreserveSig]
	int RemoveSymbolByNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Name);

	[PreserveSig]
	unsafe int GetSymbolNameWide([In] uint Index, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	int WriteSymbolWide([In] uint Index, [In][MarshalAs(UnmanagedType.LPWStr)] string Value);

	[PreserveSig]
	int OutputAsTypeWide([In] uint Index, [In][MarshalAs(UnmanagedType.LPWStr)] string Type);

	[PreserveSig]
	unsafe int GetSymbolTypeName([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	unsafe int GetSymbolTypeNameWide([In] uint Index, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	int GetSymbolSize([In] uint Index, out uint Size);

	[PreserveSig]
	int GetSymbolOffset([In] uint Index, out ulong Offset);

	[PreserveSig]
	int GetSymbolRegister([In] uint Index, out uint Register);

	[PreserveSig]
	unsafe int GetSymbolValueText([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	unsafe int GetSymbolValueTextWide([In] uint Index, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	int GetSymbolEntryInformation([In][MarshalAs(UnmanagedType.LPStruct)] DEBUG_MODULE_AND_ID Id, out DEBUG_SYMBOL_ENTRY Info);
}
