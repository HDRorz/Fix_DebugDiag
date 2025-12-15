using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct RegSetArm
{
	public uint r0;

	public uint r1;

	public uint r2;

	public uint r3;

	public uint r4;

	public uint r5;

	public uint r6;

	public uint r7;

	public uint r8;

	public uint r9;

	public uint r10;

	public uint fp;

	public uint ip;

	public uint sp;

	public uint lr;

	public uint pc;

	public uint cpsr;

	public uint orig_r0;

	public unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context)
	{
		if (contextSize < ArmContext.Size)
		{
			return false;
		}
		((ArmContext*)context)->ContextFlags = 2097155u;
		((ArmContext*)context)->R0 = r0;
		((ArmContext*)context)->R1 = r1;
		((ArmContext*)context)->R2 = r2;
		((ArmContext*)context)->R3 = r3;
		((ArmContext*)context)->R4 = r4;
		((ArmContext*)context)->R5 = r5;
		((ArmContext*)context)->R6 = r6;
		((ArmContext*)context)->R7 = r7;
		((ArmContext*)context)->R8 = r8;
		((ArmContext*)context)->R9 = r9;
		((ArmContext*)context)->R10 = r10;
		((ArmContext*)context)->R11 = fp;
		((ArmContext*)context)->R12 = ip;
		((ArmContext*)context)->Sp = sp;
		((ArmContext*)context)->Lr = lr;
		((ArmContext*)context)->Pc = pc;
		((ArmContext*)context)->Cpsr = cpsr;
		return true;
	}
}
