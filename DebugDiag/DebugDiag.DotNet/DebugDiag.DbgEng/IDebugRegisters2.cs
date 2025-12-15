using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("1656afa9-19c6-4e3a-97e7-5dc9160cf9c4")]
public interface IDebugRegisters2 : IDebugRegisters
{
	[PreserveSig]
	new int GetNumberRegisters(out uint Number);

	[PreserveSig]
	new unsafe int GetDescription([In] uint Register, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] DEBUG_REGISTER_DESCRIPTION* Desc);

	[PreserveSig]
	new int GetIndexByName([In][MarshalAs(UnmanagedType.LPStr)] string Name, out uint Index);

	[PreserveSig]
	new int GetValue([In] uint Register, out DEBUG_VALUE Value);

	[PreserveSig]
	new int SetValue([In] uint Register, [In] DEBUG_VALUE Value);

	[PreserveSig]
	new int GetValues([In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [In][Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	new int SetValues([In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [In][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	new int OutputRegisters([In] DEBUG_OUTCTL OutputControl, [In] DEBUG_REGISTERS Flags);

	[PreserveSig]
	new int GetInstructionOffset(out ulong Offset);

	[PreserveSig]
	new int GetStackOffset(out ulong Offset);

	[PreserveSig]
	new int GetFrameOffset(out ulong Offset);

	[PreserveSig]
	unsafe int GetDescriptionWide([In] uint Register, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] DEBUG_REGISTER_DESCRIPTION* Desc);

	[PreserveSig]
	int GetIndexByNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Name, out uint Index);

	[PreserveSig]
	int GetNumberPseudoRegisters(out uint Number);

	[PreserveSig]
	unsafe int GetPseudoDescription([In] uint Register, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* TypeModule, [In] uint* TypeId);

	[PreserveSig]
	unsafe int GetPseudoDescriptionWide([In] uint Register, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder NameBuffer, [In] int NameBufferSize, [In] uint* NameSize, [In] ulong* TypeModule, [In] uint* TypeId);

	[PreserveSig]
	int GetPseudoIndexByName([In][MarshalAs(UnmanagedType.LPStr)] string Name, out uint Index);

	[PreserveSig]
	int GetPseudoIndexByNameWide([In][MarshalAs(UnmanagedType.LPWStr)] string Name, out uint Index);

	[PreserveSig]
	int GetPseudoValues([In] uint Source, [In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	int SetPseudoValues([In] uint Source, [In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [In][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	int GetValues2([In] uint Source, [In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [Out][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	int SetValues2([In] uint Source, [In] uint Count, [In][MarshalAs(UnmanagedType.LPArray)] uint[] Indices, [In] uint Start, [In][MarshalAs(UnmanagedType.LPArray)] DEBUG_VALUE[] Values);

	[PreserveSig]
	int OutputRegisters2([In] uint OutputControl, [In] uint Source, [In] uint Flags);

	[PreserveSig]
	int GetInstructionOffset2([In] uint Source, out ulong Offset);

	[PreserveSig]
	int GetStackOffset2([In] uint Source, out ulong Offset);

	[PreserveSig]
	int GetFrameOffset2([In] uint Source, out ulong Offset);
}
