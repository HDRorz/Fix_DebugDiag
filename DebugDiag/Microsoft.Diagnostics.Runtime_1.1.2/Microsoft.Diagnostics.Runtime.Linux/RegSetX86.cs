namespace Microsoft.Diagnostics.Runtime.Linux;

internal readonly struct RegSetX86
{
	public readonly uint Ebx;

	public readonly uint Ecx;

	public readonly uint Edx;

	public readonly uint Esi;

	public readonly uint Edi;

	public readonly uint Ebp;

	public readonly uint Eax;

	public readonly uint Xds;

	public readonly uint Xes;

	public readonly uint Xfs;

	public readonly uint Xgs;

	public readonly uint OrigEax;

	public readonly uint Eip;

	public readonly uint Xcs;

	public readonly uint EFlags;

	public readonly uint Esp;

	public readonly uint Xss;

	public unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context)
	{
		if (contextSize < X86Context.Size)
		{
			return false;
		}
		((X86Context*)context)->ContextFlags = 1048579u;
		((X86Context*)context)->Ebp = Ebp;
		((X86Context*)context)->Eip = Eip;
		((X86Context*)context)->Ecx = Ecx;
		((X86Context*)context)->EFlags = EFlags;
		((X86Context*)context)->Esp = Esp;
		((X86Context*)context)->Ss = Xss;
		((X86Context*)context)->Edi = Edi;
		((X86Context*)context)->Esi = Esi;
		((X86Context*)context)->Ebx = Ebx;
		((X86Context*)context)->Edx = Edx;
		((X86Context*)context)->Ecx = Ecx;
		((X86Context*)context)->Eax = Eax;
		return true;
	}
}
