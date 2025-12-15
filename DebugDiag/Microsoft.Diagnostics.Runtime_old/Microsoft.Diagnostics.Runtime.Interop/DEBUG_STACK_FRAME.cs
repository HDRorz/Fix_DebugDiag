namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_STACK_FRAME
{
	public ulong InstructionOffset;

	public ulong ReturnOffset;

	public ulong FrameOffset;

	public ulong StackOffset;

	public ulong FuncTableEntry;

	public unsafe fixed ulong Params[4];

	public unsafe fixed ulong Reserved[6];

	public uint Virtual;

	public uint FrameNumber;
}
