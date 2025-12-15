using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct RegSetX64
{
	public ulong R15;

	public ulong R14;

	public ulong R13;

	public ulong R12;

	public ulong Rbp;

	public ulong Rbx;

	public ulong R11;

	public ulong R10;

	public ulong R8;

	public ulong R9;

	public ulong Rax;

	public ulong Rcx;

	public ulong Rdx;

	public ulong Rsi;

	public ulong Rdi;

	public ulong OrigRax;

	public ulong Rip;

	public ulong CS;

	public ulong EFlags;

	public ulong Rsp;

	public ulong SS;

	public ulong FSBase;

	public ulong GSBase;

	public ulong DS;

	public ulong ES;

	public ulong FS;

	public ulong GS;

	public unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context)
	{
		if (contextSize < AMD64Context.Size)
		{
			return false;
		}
		((AMD64Context*)context)->ContextFlags = 1048583u;
		((AMD64Context*)context)->R15 = R15;
		((AMD64Context*)context)->R14 = R14;
		((AMD64Context*)context)->R13 = R13;
		((AMD64Context*)context)->R12 = R12;
		((AMD64Context*)context)->Rbp = Rbp;
		((AMD64Context*)context)->Rbx = Rbx;
		((AMD64Context*)context)->R11 = R11;
		((AMD64Context*)context)->R10 = R10;
		((AMD64Context*)context)->R9 = R9;
		((AMD64Context*)context)->R8 = R8;
		((AMD64Context*)context)->Rax = Rax;
		((AMD64Context*)context)->Rcx = Rcx;
		((AMD64Context*)context)->Rdx = Rdx;
		((AMD64Context*)context)->Rsi = Rsi;
		((AMD64Context*)context)->Rdi = Rdi;
		((AMD64Context*)context)->Rip = Rip;
		((AMD64Context*)context)->Rsp = Rsp;
		((AMD64Context*)context)->Cs = (ushort)CS;
		((AMD64Context*)context)->Ds = (ushort)DS;
		((AMD64Context*)context)->Ss = (ushort)SS;
		((AMD64Context*)context)->Fs = (ushort)FS;
		((AMD64Context*)context)->Gs = (ushort)GS;
		return true;
	}
}
