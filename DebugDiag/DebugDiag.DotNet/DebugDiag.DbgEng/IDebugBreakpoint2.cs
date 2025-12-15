using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("1b278d20-79f2-426e-a3f9-c1ddf375d48e")]
public interface IDebugBreakpoint2 : IDebugBreakpoint
{
	[PreserveSig]
	new int GetId(out uint Id);

	[PreserveSig]
	new int GetType(out DEBUG_BREAKPOINT_TYPE BreakType, out uint ProcType);

	[PreserveSig]
	new int GetAdder([MarshalAs(UnmanagedType.Interface)] out IDebugClient Adder);

	[PreserveSig]
	new int GetFlags(out DEBUG_BREAKPOINT_FLAG Flags);

	[PreserveSig]
	new int AddFlags([In] DEBUG_BREAKPOINT_FLAG Flags);

	[PreserveSig]
	new int RemoveFlags([In] DEBUG_BREAKPOINT_FLAG Flags);

	[PreserveSig]
	new int SetFlags([In] DEBUG_BREAKPOINT_FLAG Flags);

	[PreserveSig]
	new int GetOffset(out ulong Offset);

	[PreserveSig]
	new int SetOffset([In] ulong Offset);

	[PreserveSig]
	new int GetDataParameters(out uint Size, out DEBUG_BREAKPOINT_ACCESS_TYPE AccessType);

	[PreserveSig]
	new int SetDataParameters([In] uint Size, [In] DEBUG_BREAKPOINT_ACCESS_TYPE AccessType);

	[PreserveSig]
	new int GetPassCount(out uint Count);

	[PreserveSig]
	new int SetPassCount([In] uint Count);

	[PreserveSig]
	new int GetCurrentPassCount(out uint Count);

	[PreserveSig]
	new int GetMatchThreadId(out uint Id);

	[PreserveSig]
	new int SetMatchThreadId([In] uint Thread);

	[PreserveSig]
	new unsafe int GetCommand([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* CommandSize);

	[PreserveSig]
	new int SetCommand([In][MarshalAs(UnmanagedType.LPStr)] string Command);

	[PreserveSig]
	new unsafe int GetOffsetExpression([Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* ExpressionSize);

	[PreserveSig]
	new int SetOffsetExpression([In][MarshalAs(UnmanagedType.LPStr)] string Expression);

	[PreserveSig]
	new int GetParameters(out DEBUG_BREAKPOINT_PARAMETERS Params);

	[PreserveSig]
	unsafe int GetCommandWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* CommandSize);

	[PreserveSig]
	int SetCommandWide([In][MarshalAs(UnmanagedType.LPWStr)] string Command);

	[PreserveSig]
	unsafe int GetOffsetExpressionWide([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] int BufferSize, [In] uint* ExpressionSize);

	[PreserveSig]
	int SetOffsetExpressionWide([In][MarshalAs(UnmanagedType.LPWStr)] string Command);
}
