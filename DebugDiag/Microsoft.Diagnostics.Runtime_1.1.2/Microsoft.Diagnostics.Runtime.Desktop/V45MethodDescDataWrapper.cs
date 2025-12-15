using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class V45MethodDescDataWrapper : IMethodDescData
{
	public ulong GCInfo { get; private set; }

	public ulong MethodDesc { get; private set; }

	public ulong Module { get; private set; }

	public uint MDToken { get; private set; }

	public ulong NativeCodeAddr { get; private set; }

	public MethodCompilationType JITType { get; private set; }

	public ulong MethodTable { get; private set; }

	public ulong ColdStart { get; private set; }

	public uint ColdSize { get; private set; }

	public uint HotSize { get; private set; }

	public bool Init(SOSDac sos, ulong md)
	{
		if (!sos.GetMethodDescData(md, 0uL, out var data))
		{
			return false;
		}
		MethodDesc = data.MethodDesc;
		NativeCodeAddr = data.NativeCodeAddr;
		Module = data.Module;
		MDToken = data.MDToken;
		MethodTable = data.MethodTable;
		if (sos.GetCodeHeaderData(data.NativeCodeAddr, out var codeHeaderData))
		{
			if (codeHeaderData.JITType == 1)
			{
				JITType = MethodCompilationType.Jit;
			}
			else if (codeHeaderData.JITType == 2)
			{
				JITType = MethodCompilationType.Ngen;
			}
			else
			{
				JITType = MethodCompilationType.None;
			}
			GCInfo = codeHeaderData.GCInfo;
			ColdStart = codeHeaderData.ColdRegionStart;
			ColdSize = codeHeaderData.ColdRegionSize;
			HotSize = codeHeaderData.HotRegionSize;
		}
		else
		{
			JITType = MethodCompilationType.None;
		}
		return true;
	}
}
