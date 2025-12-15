using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[StructLayout(LayoutKind.Explicit)]
public struct VectorRegisterArea
{
	public const int VectorRegisterSize = 26;

	[FieldOffset(0)]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
	public M128A[] VectorRegister;

	[FieldOffset(416)]
	public ulong VectorControl;

	public VectorRegisterArea(VectorRegisterArea other)
	{
		this = default(VectorRegisterArea);
		for (int i = 0; i < 26; i++)
		{
			VectorRegister[i] = other.VectorRegister[i];
		}
		VectorControl = other.VectorControl;
	}

	public void Clear()
	{
		for (int i = 0; i < 26; i++)
		{
			VectorRegister[i].Clear();
		}
		VectorControl = 0uL;
	}
}
