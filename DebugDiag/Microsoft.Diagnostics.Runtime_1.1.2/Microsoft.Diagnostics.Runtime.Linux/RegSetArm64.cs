using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct RegSetArm64
{
	public unsafe fixed ulong regs[31];

	public ulong sp;

	public ulong pc;

	public ulong pstate;

	public unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context)
	{
		if (contextSize < Arm64Context.Size)
		{
			return false;
		}
		((Arm64Context*)context)->ContextFlags = 2097155u;
		((Arm64Context*)context)->Cpsr = (uint)pstate;
		((Arm64Context*)context)->X0 = regs[0];
		((Arm64Context*)context)->X1 = regs[1];
		((Arm64Context*)context)->X2 = regs[2];
		((Arm64Context*)context)->X3 = regs[3];
		((Arm64Context*)context)->X4 = regs[4];
		((Arm64Context*)context)->X5 = regs[5];
		((Arm64Context*)context)->X6 = regs[6];
		((Arm64Context*)context)->X7 = regs[7];
		((Arm64Context*)context)->X8 = regs[8];
		((Arm64Context*)context)->X9 = regs[9];
		((Arm64Context*)context)->X10 = regs[10];
		((Arm64Context*)context)->X11 = regs[11];
		((Arm64Context*)context)->X12 = regs[12];
		((Arm64Context*)context)->X13 = regs[13];
		((Arm64Context*)context)->X14 = regs[14];
		((Arm64Context*)context)->X15 = regs[15];
		((Arm64Context*)context)->X16 = regs[16];
		((Arm64Context*)context)->X17 = regs[17];
		((Arm64Context*)context)->X18 = regs[18];
		((Arm64Context*)context)->X19 = regs[19];
		((Arm64Context*)context)->X20 = regs[20];
		((Arm64Context*)context)->X21 = regs[21];
		((Arm64Context*)context)->X22 = regs[22];
		((Arm64Context*)context)->X23 = regs[23];
		((Arm64Context*)context)->X24 = regs[24];
		((Arm64Context*)context)->X25 = regs[25];
		((Arm64Context*)context)->X26 = regs[26];
		((Arm64Context*)context)->X27 = regs[27];
		((Arm64Context*)context)->X28 = regs[28];
		((Arm64Context*)context)->Fp = regs[29];
		((Arm64Context*)context)->Lr = regs[30];
		((Arm64Context*)context)->Sp = sp;
		((Arm64Context*)context)->Pc = pc;
		return true;
	}
}
