using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Arm64Context
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
	public uint Cpsr;

	[FieldOffset(8)]
	[Register(RegisterType.General)]
	public ulong X0;

	[FieldOffset(16)]
	[Register(RegisterType.General)]
	public ulong X1;

	[FieldOffset(24)]
	[Register(RegisterType.General)]
	public ulong X2;

	[FieldOffset(32)]
	[Register(RegisterType.General)]
	public ulong X3;

	[FieldOffset(40)]
	[Register(RegisterType.General)]
	public ulong X4;

	[FieldOffset(48)]
	[Register(RegisterType.General)]
	public ulong X5;

	[FieldOffset(56)]
	[Register(RegisterType.General)]
	public ulong X6;

	[FieldOffset(64)]
	[Register(RegisterType.General)]
	public ulong X7;

	[FieldOffset(72)]
	[Register(RegisterType.General)]
	public ulong X8;

	[FieldOffset(80)]
	[Register(RegisterType.General)]
	public ulong X9;

	[FieldOffset(88)]
	[Register(RegisterType.General)]
	public ulong X10;

	[FieldOffset(96)]
	[Register(RegisterType.General)]
	public ulong X11;

	[FieldOffset(104)]
	[Register(RegisterType.General)]
	public ulong X12;

	[FieldOffset(112)]
	[Register(RegisterType.General)]
	public ulong X13;

	[FieldOffset(120)]
	[Register(RegisterType.General)]
	public ulong X14;

	[FieldOffset(128)]
	[Register(RegisterType.General)]
	public ulong X15;

	[FieldOffset(136)]
	[Register(RegisterType.General)]
	public ulong X16;

	[FieldOffset(144)]
	[Register(RegisterType.General)]
	public ulong X17;

	[FieldOffset(152)]
	[Register(RegisterType.General)]
	public ulong X18;

	[FieldOffset(160)]
	[Register(RegisterType.General)]
	public ulong X19;

	[FieldOffset(168)]
	[Register(RegisterType.General)]
	public ulong X20;

	[FieldOffset(176)]
	[Register(RegisterType.General)]
	public ulong X21;

	[FieldOffset(184)]
	[Register(RegisterType.General)]
	public ulong X22;

	[FieldOffset(192)]
	[Register(RegisterType.General)]
	public ulong X23;

	[FieldOffset(200)]
	[Register(RegisterType.General)]
	public ulong X24;

	[FieldOffset(208)]
	[Register(RegisterType.General)]
	public ulong X25;

	[FieldOffset(216)]
	[Register(RegisterType.General)]
	public ulong X26;

	[FieldOffset(224)]
	[Register(RegisterType.General)]
	public ulong X27;

	[FieldOffset(232)]
	[Register(RegisterType.General)]
	public ulong X28;

	[FieldOffset(240)]
	[Register(RegisterType.Control | RegisterType.FramePointer)]
	public ulong Fp;

	[FieldOffset(248)]
	[Register(RegisterType.Control)]
	public ulong Lr;

	[FieldOffset(256)]
	[Register(RegisterType.Control | RegisterType.StackPointer)]
	public ulong Sp;

	[FieldOffset(264)]
	[Register(RegisterType.Control | RegisterType.ProgramCounter)]
	public ulong Pc;

	[FieldOffset(272)]
	[Register(RegisterType.FloatingPoint)]
	public unsafe fixed ulong V[64];

	[FieldOffset(784)]
	[Register(RegisterType.FloatingPoint)]
	public uint Fpcr;

	[FieldOffset(788)]
	[Register(RegisterType.FloatingPoint)]
	public uint Fpsr;

	private const int ARM64_MAX_BREAKPOINTS = 8;

	private const int ARM64_MAX_WATCHPOINTS = 2;

	[FieldOffset(792)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Bcr[8];

	[FieldOffset(824)]
	[Register(RegisterType.Debug)]
	public unsafe fixed ulong Bvr[8];

	[FieldOffset(888)]
	[Register(RegisterType.Debug)]
	public unsafe fixed uint Wcr[2];

	[FieldOffset(896)]
	[Register(RegisterType.Debug)]
	public unsafe fixed ulong Wvr[2];

	public static int Size => Marshal.SizeOf(typeof(Arm64Context));
}
