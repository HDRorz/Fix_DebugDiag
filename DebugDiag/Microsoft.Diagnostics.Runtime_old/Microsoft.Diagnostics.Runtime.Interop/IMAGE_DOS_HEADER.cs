using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct IMAGE_DOS_HEADER
{
	[FieldOffset(0)]
	public ushort e_magic;

	[FieldOffset(2)]
	public ushort e_cblp;

	[FieldOffset(4)]
	public ushort e_cp;

	[FieldOffset(6)]
	public ushort e_crlc;

	[FieldOffset(8)]
	public ushort e_cparhdr;

	[FieldOffset(10)]
	public ushort e_minalloc;

	[FieldOffset(12)]
	public ushort e_maxalloc;

	[FieldOffset(14)]
	public ushort e_ss;

	[FieldOffset(16)]
	public ushort e_sp;

	[FieldOffset(18)]
	public ushort e_csum;

	[FieldOffset(20)]
	public ushort e_ip;

	[FieldOffset(22)]
	public ushort e_cs;

	[FieldOffset(24)]
	public ushort e_lfarlc;

	[FieldOffset(26)]
	public ushort e_ovno;

	[FieldOffset(28)]
	public unsafe fixed ushort e_res[4];

	[FieldOffset(36)]
	public ushort e_oemid;

	[FieldOffset(38)]
	public ushort e_oeminfo;

	[FieldOffset(40)]
	public unsafe fixed ushort e_res2[10];

	[FieldOffset(60)]
	public uint e_lfanew;
}
