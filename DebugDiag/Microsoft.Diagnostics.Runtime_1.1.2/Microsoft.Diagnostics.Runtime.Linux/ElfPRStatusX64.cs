using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfPRStatusX64 : IElfPRStatus
{
	public ElfSignalInfo SignalInfo;

	public short CurrentSignal;

	private ushort Padding;

	public ulong SignalsPending;

	public ulong SignalsHeld;

	public uint Pid;

	public uint PPid;

	public uint PGrp;

	public uint Sid;

	public TimeVal64 UserTime;

	public TimeVal64 SystemTime;

	public TimeVal64 CUserTime;

	public TimeVal64 CSystemTime;

	public RegSetX64 RegisterSet;

	public int FPValid;

	public uint ProcessId => PGrp;

	public uint ThreadId => Pid;

	public unsafe bool CopyContext(uint contextFlags, uint contextSize, void* context)
	{
		return RegisterSet.CopyContext(contextFlags, contextSize, context);
	}
}
