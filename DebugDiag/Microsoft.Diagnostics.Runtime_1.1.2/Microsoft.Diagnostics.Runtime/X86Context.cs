using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct X86Context
{
	public const uint Context = 1048576u;

	public const uint ContextControl = 1048577u;

	public const uint ContextInteger = 1048578u;

	public const uint ContextSegments = 1048580u;

	public const uint ContextFloatingPoint = 1048584u;

	public const uint ContextDebugRegisters = 1048592u;

	[FieldOffset(0)]
	public uint ContextFlags;

	[FieldOffset(4)]
	[Register(RegisterType.Debug)]
	public uint Dr0;

	[FieldOffset(8)]
	[Register(RegisterType.Debug)]
	public uint Dr1;

	[FieldOffset(12)]
	[Register(RegisterType.Debug)]
	public uint Dr2;

	[FieldOffset(16)]
	[Register(RegisterType.Debug)]
	public uint Dr3;

	[FieldOffset(20)]
	[Register(RegisterType.Debug)]
	public uint Dr6;

	[FieldOffset(24)]
	[Register(RegisterType.Debug)]
	public uint Dr7;

	[FieldOffset(28)]
	[Register(RegisterType.FloatingPoint)]
	public uint ControlWord;

	[FieldOffset(32)]
	[Register(RegisterType.FloatingPoint)]
	public uint StatusWord;

	[FieldOffset(36)]
	[Register(RegisterType.FloatingPoint)]
	public uint TagWord;

	[FieldOffset(40)]
	[Register(RegisterType.FloatingPoint)]
	public uint ErrorOffset;

	[FieldOffset(44)]
	[Register(RegisterType.FloatingPoint)]
	public uint ErrorSelector;

	[FieldOffset(48)]
	[Register(RegisterType.FloatingPoint)]
	public uint DataOffset;

	[FieldOffset(52)]
	[Register(RegisterType.FloatingPoint)]
	public uint DataSelector;

	[FieldOffset(56)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST0;

	[FieldOffset(66)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST1;

	[FieldOffset(76)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST2;

	[FieldOffset(86)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST3;

	[FieldOffset(96)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST4;

	[FieldOffset(106)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST5;

	[FieldOffset(116)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST6;

	[FieldOffset(126)]
	[Register(RegisterType.FloatingPoint)]
	public Float80 ST7;

	[FieldOffset(136)]
	[Register(RegisterType.FloatingPoint)]
	public uint Cr0NpxState;

	[FieldOffset(140)]
	[Register(RegisterType.Segments)]
	public uint Gs;

	[FieldOffset(144)]
	[Register(RegisterType.Segments)]
	public uint Fs;

	[FieldOffset(148)]
	[Register(RegisterType.Segments)]
	public uint Es;

	[FieldOffset(152)]
	[Register(RegisterType.Segments)]
	public uint Ds;

	[FieldOffset(156)]
	[Register(RegisterType.General)]
	public uint Edi;

	[FieldOffset(160)]
	[Register(RegisterType.General)]
	public uint Esi;

	[FieldOffset(164)]
	[Register(RegisterType.General)]
	public uint Ebx;

	[FieldOffset(168)]
	[Register(RegisterType.General)]
	public uint Edx;

	[FieldOffset(172)]
	[Register(RegisterType.General)]
	public uint Ecx;

	[FieldOffset(176)]
	[Register(RegisterType.General)]
	public uint Eax;

	[FieldOffset(180)]
	[Register(RegisterType.Control | RegisterType.FramePointer)]
	public uint Ebp;

	[FieldOffset(184)]
	[Register(RegisterType.Control | RegisterType.ProgramCounter)]
	public uint Eip;

	[FieldOffset(188)]
	[Register(RegisterType.Segments)]
	public uint Cs;

	[FieldOffset(192)]
	[Register(RegisterType.General)]
	public uint EFlags;

	[FieldOffset(196)]
	[Register(RegisterType.Control | RegisterType.StackPointer)]
	public uint Esp;

	[FieldOffset(200)]
	[Register(RegisterType.Segments)]
	public uint Ss;

	[FieldOffset(204)]
	public unsafe fixed byte ExtendedRegisters[512];

	public static int Size => Marshal.SizeOf(typeof(X86Context));
}
