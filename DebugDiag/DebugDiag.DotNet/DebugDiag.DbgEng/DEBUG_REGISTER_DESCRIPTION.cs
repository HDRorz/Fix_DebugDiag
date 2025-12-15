namespace DebugDiag.DbgEng;

public struct DEBUG_REGISTER_DESCRIPTION
{
	public DEBUG_VALUE_TYPE Type;

	public DEBUG_REGISTER Flags;

	public ulong SubregMaster;

	public ulong SubregLength;

	public ulong SubregMask;

	public ulong SubregShift;

	public ulong Reserved0;
}
