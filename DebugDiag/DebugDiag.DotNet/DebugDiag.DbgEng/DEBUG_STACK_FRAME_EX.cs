namespace DebugDiag.DbgEng;

public struct DEBUG_STACK_FRAME_EX
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

	public uint InlineFrameContext;

	public uint Reserved1;

	public unsafe DEBUG_STACK_FRAME_EX(DEBUG_STACK_FRAME dsf)
	{
		InstructionOffset = dsf.InstructionOffset;
		ReturnOffset = dsf.ReturnOffset;
		FrameOffset = dsf.FrameOffset;
		StackOffset = dsf.StackOffset;
		FuncTableEntry = dsf.FuncTableEntry;
		fixed (ulong* @params = Params)
		{
			for (int i = 0; i < 4; i++)
			{
				@params[i] = dsf.Params[i];
			}
		}
		fixed (ulong* reserved = Reserved)
		{
			for (int j = 0; j < 6; j++)
			{
				reserved[j] = dsf.Reserved[j];
			}
		}
		Virtual = dsf.Virtual;
		FrameNumber = dsf.FrameNumber;
		InlineFrameContext = uint.MaxValue;
		Reserved1 = 0u;
	}
}
