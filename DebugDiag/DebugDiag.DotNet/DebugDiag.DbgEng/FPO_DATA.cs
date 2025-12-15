namespace DebugDiag.DbgEng;

public struct FPO_DATA
{
	public uint ulOffStart;

	public uint cbProcSize;

	public uint cdwLocals;

	public ushort cdwParams;

	private ushort bitfield;

	public ushort cbProlog => (ushort)((bitfield & 0xFF00) >> 8);

	public ushort cbRegs => (ushort)((bitfield & 0xE0) >> 5);

	public ushort fHasSEH => (ushort)((bitfield & 0x10) >> 4);

	public ushort fUseBP => (ushort)((bitfield & 8) >> 3);

	public ushort reserved => (ushort)((bitfield & 4) >> 2);

	public ushort cbFrame => (ushort)(bitfield & 3);
}
