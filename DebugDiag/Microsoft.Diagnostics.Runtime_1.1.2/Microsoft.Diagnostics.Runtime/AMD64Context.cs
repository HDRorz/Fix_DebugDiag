using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AMD64Context
{
	public const uint Context = 1048576u;

	public const uint ContextControl = 1048577u;

	public const uint ContextInteger = 1048578u;

	public const uint ContextSegments = 1048580u;

	public const uint ContextFloatingPoint = 1048584u;

	public const uint ContextDebugRegisters = 1048592u;

	[FieldOffset(0)]
	public ulong P1Home;

	[FieldOffset(8)]
	public ulong P2Home;

	[FieldOffset(16)]
	public ulong P3Home;

	[FieldOffset(24)]
	public ulong P4Home;

	[FieldOffset(32)]
	public ulong P5Home;

	[FieldOffset(40)]
	public ulong P6Home;

	[FieldOffset(48)]
	public uint ContextFlags;

	[FieldOffset(52)]
	public uint MxCsr;

	[FieldOffset(56)]
	[Register(RegisterType.Segments)]
	public ushort Cs;

	[FieldOffset(58)]
	[Register(RegisterType.Segments)]
	public ushort Ds;

	[FieldOffset(60)]
	[Register(RegisterType.Segments)]
	public ushort Es;

	[FieldOffset(62)]
	[Register(RegisterType.Segments)]
	public ushort Fs;

	[FieldOffset(64)]
	[Register(RegisterType.Segments)]
	public ushort Gs;

	[FieldOffset(66)]
	[Register(RegisterType.Segments)]
	public ushort Ss;

	[FieldOffset(68)]
	[Register(RegisterType.General)]
	public int EFlags;

	[FieldOffset(72)]
	[Register(RegisterType.Debug)]
	public ulong Dr0;

	[FieldOffset(80)]
	[Register(RegisterType.Debug)]
	public ulong Dr1;

	[FieldOffset(88)]
	[Register(RegisterType.Debug)]
	public ulong Dr2;

	[FieldOffset(96)]
	[Register(RegisterType.Debug)]
	public ulong Dr3;

	[FieldOffset(104)]
	[Register(RegisterType.Debug)]
	public ulong Dr6;

	[FieldOffset(112)]
	[Register(RegisterType.Debug)]
	public ulong Dr7;

	[FieldOffset(120)]
	[Register(RegisterType.General)]
	public ulong Rax;

	[FieldOffset(128)]
	[Register(RegisterType.General)]
	public ulong Rcx;

	[FieldOffset(136)]
	[Register(RegisterType.General)]
	public ulong Rdx;

	[FieldOffset(144)]
	[Register(RegisterType.General)]
	public ulong Rbx;

	[FieldOffset(152)]
	[Register(RegisterType.Control | RegisterType.StackPointer)]
	public ulong Rsp;

	[FieldOffset(160)]
	[Register(RegisterType.Control | RegisterType.FramePointer)]
	public ulong Rbp;

	[FieldOffset(168)]
	[Register(RegisterType.General)]
	public ulong Rsi;

	[FieldOffset(176)]
	[Register(RegisterType.General)]
	public ulong Rdi;

	[FieldOffset(184)]
	[Register(RegisterType.General)]
	public ulong R8;

	[FieldOffset(192)]
	[Register(RegisterType.General)]
	public ulong R9;

	[FieldOffset(200)]
	[Register(RegisterType.General)]
	public ulong R10;

	[FieldOffset(208)]
	[Register(RegisterType.General)]
	public ulong R11;

	[FieldOffset(216)]
	[Register(RegisterType.General)]
	public ulong R12;

	[FieldOffset(224)]
	[Register(RegisterType.General)]
	public ulong R13;

	[FieldOffset(232)]
	[Register(RegisterType.General)]
	public ulong R14;

	[FieldOffset(240)]
	[Register(RegisterType.General)]
	public ulong R15;

	[FieldOffset(248)]
	[Register(RegisterType.Control | RegisterType.ProgramCounter)]
	public ulong Rip;

	[FieldOffset(1192)]
	[Register(RegisterType.Debug)]
	public ulong DebugControl;

	[FieldOffset(1200)]
	[Register(RegisterType.Debug)]
	public ulong LastBranchToRip;

	[FieldOffset(1208)]
	[Register(RegisterType.Debug)]
	public ulong LastBranchFromRip;

	[FieldOffset(1216)]
	[Register(RegisterType.Debug)]
	public ulong LastExceptionToRip;

	[FieldOffset(1224)]
	[Register(RegisterType.Debug)]
	public ulong LastExceptionFromRip;

	public static int Size => Marshal.SizeOf(typeof(AMD64Context));
}
