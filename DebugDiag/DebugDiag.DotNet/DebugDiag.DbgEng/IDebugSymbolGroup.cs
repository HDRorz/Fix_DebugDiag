using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("f2528316-0f1a-4431-aeed-11d096e1e2ab")]
public interface IDebugSymbolGroup
{
	[PreserveSig]
	int GetNumberSymbols(out uint Number);

	[PreserveSig]
	int AddSymbol([In][MarshalAs(UnmanagedType.LPStr)] string Name, [In][Out] ref uint Index);

	[PreserveSig]
	int RemoveSymbolByName([In][MarshalAs(UnmanagedType.LPStr)] string Name);

	[PreserveSig]
	int RemoveSymbolsByIndex([In] uint Index);

	[PreserveSig]
	unsafe int GetSymbolName([In] uint Index, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* NameSize);

	[PreserveSig]
	int GetSymbolParameters([In] uint Start, [In] uint Count, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_SYMBOL_PARAMETERS[] Params);

	[PreserveSig]
	int ExpandSymbol([In] uint Index, [In][MarshalAs(UnmanagedType.Bool)] bool Expand);

	[PreserveSig]
	int OutputSymbols([In] DEBUG_OUTCTL OutputControl, [In] DEBUG_OUTPUT_SYMBOLS Flags, [In] uint Start, [In] uint Count);

	[PreserveSig]
	int WriteSymbol([In] uint Index, [In][MarshalAs(UnmanagedType.LPStr)] string Value);

	[PreserveSig]
	int OutputAsType([In] uint Index, [In][MarshalAs(UnmanagedType.LPStr)] string Type);
}
