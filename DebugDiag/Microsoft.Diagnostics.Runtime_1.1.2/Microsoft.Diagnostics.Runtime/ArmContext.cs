using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ArmContext
{
	public const uint Context = 2097152u;

	public const uint ContextControl = 2097153u;

	public const uint ContextInteger = 2097154u;

	public const uint ContextFloatingPoint = 2097156u;

	public const uint ContextDebugRegisters = 2097160u;

	[FieldOffset(0)]
	public uint ContextFlags;

	[FieldOffset(4)]
	[Register(RegisterType.General)]
	public uint R0;

	[FieldOffset(8)]
	[Register(RegisterType.General)]
	public uint R1;

	[FieldOffset(12)]
	[Register(RegisterType.General)]
	public uint R2;

	[FieldOffset(16)]
	[Register(RegisterType.General)]
	public uint R3;

	[FieldOffset(20)]
	[Register(RegisterType.General)]
	public uint R4;

	[FieldOffset(24)]
	[Register(RegisterType.General)]
	public uint R5;

	[FieldOffset(28)]
	[Register(RegisterType.General)]
	public uint R6;

	[FieldOffset(32)]
	[Register(RegisterType.General)]
	public uint R7;

	[FieldOffset(36)]
	[Register(RegisterType.General)]
	public uint R8;

	[FieldOffset(40)]
	[Register(RegisterType.General)]
	public uint R9;

	[FieldOffset(44)]
	[Register(RegisterType.General)]
	public uint R10;

	[FieldOffset(48)]
	[Register(RegisterType.General | RegisterType.FramePointer)]
	public uint R11;

	[FieldOffset(52)]
	[Register(RegisterType.General)]
	public uint R12;

	[FieldOffset(56)]
	[Register(RegisterType.Control | RegisterType.StackPointer)]
	public uint Sp;

	[FieldOffset(60)]
	[Register(RegisterType.Control)]
	public uint Lr;

	[FieldOffset(64)]
	[Register(RegisterType.Control | RegisterType.ProgramCounter)]
	public uint Pc;

	[FieldOffset(68)]
	[Register(RegisterType.General)]
	public uint Cpsr;

	[FieldOffset(72)]
	[Register(RegisterType.FloatingPoint)]
	public uint Fpscr;

	[FieldOffset(80)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q0;

	[FieldOffset(96)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q1;

	[FieldOffset(112)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q2;

	[FieldOffset(128)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q3;

	[FieldOffset(144)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q4;

	[FieldOffset(160)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q5;

	[FieldOffset(176)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q6;

	[FieldOffset(192)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q7;

	[FieldOffset(208)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q8;

	[FieldOffset(224)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q9;

	[FieldOffset(240)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q10;

	[FieldOffset(256)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q11;

	[FieldOffset(272)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q12;

	[FieldOffset(288)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q13;

	[FieldOffset(304)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q14;

	[FieldOffset(320)]
	[Register(RegisterType.FloatingPoint)]
	public M128A Q15;

	private const int ARM_MAX_BREAKPOINTS = 8;

	private const int ARM_MAX_WATCHPOINTS = 1;

	[FieldOffset(336)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Bvr[8];

	[FieldOffset(368)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Bcr[8];

	[FieldOffset(400)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Wvr[1];

	[FieldOffset(404)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Wcr[1];

	[FieldOffset(408)]
	public ulong Padding;

	public static int Size => Marshal.SizeOf(typeof(ArmContext));
}
