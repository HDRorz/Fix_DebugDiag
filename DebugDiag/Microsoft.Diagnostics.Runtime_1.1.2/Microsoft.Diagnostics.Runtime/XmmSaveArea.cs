using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit)]
public struct XmmSaveArea
{
	public const int HeaderSize = 2;

	public const int LegacySize = 8;

	[FieldOffset(0)]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
	public M128A[] Header;

	[FieldOffset(32)]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
	public M128A[] Legacy;

	[FieldOffset(160)]
	public M128A Xmm0;

	[FieldOffset(176)]
	public M128A Xmm1;

	[FieldOffset(192)]
	public M128A Xmm2;

	[FieldOffset(208)]
	public M128A Xmm3;

	[FieldOffset(224)]
	public M128A Xmm4;

	[FieldOffset(240)]
	public M128A Xmm5;

	[FieldOffset(256)]
	public M128A Xmm6;

	[FieldOffset(272)]
	public M128A Xmm7;

	[FieldOffset(288)]
	public M128A Xmm8;

	[FieldOffset(304)]
	public M128A Xmm9;

	[FieldOffset(320)]
	public M128A Xmm10;

	[FieldOffset(336)]
	public M128A Xmm11;

	[FieldOffset(352)]
	public M128A Xmm12;

	[FieldOffset(368)]
	public M128A Xmm13;

	[FieldOffset(384)]
	public M128A Xmm14;

	[FieldOffset(400)]
	public M128A Xmm15;
}
