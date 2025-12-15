namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class V45MethodDescDataWrapper : IMethodDescData
{
	private MethodCompilationType _jitType;

	private ulong _md;

	private ulong _module;

	private ulong _ip;

	private uint _token;

	private ulong _mt;

	public ulong MethodDesc => _md;

	public ulong Module => _module;

	public uint MDToken => _token;

	public ulong NativeCodeAddr => _ip;

	public MethodCompilationType JITType => _jitType;

	public ulong MethodTable => _mt;

	public bool Init(ISOSDac sos, ulong md)
	{
		ulong pcNeededRevertedRejitData = 0uL;
		V45MethodDescData data = default(V45MethodDescData);
		if (sos.GetMethodDescData(md, 0uL, out data, 0u, null, out pcNeededRevertedRejitData) < 0)
		{
			return false;
		}
		_md = data.MethodDescPtr;
		_ip = data.NativeCodeAddr;
		_module = data.ModulePtr;
		_token = data.MDToken;
		_mt = data.MethodTablePtr;
		if (sos.GetCodeHeaderData(data.NativeCodeAddr, out var data2) >= 0)
		{
			if (data2.JITType == 1)
			{
				_jitType = MethodCompilationType.Jit;
			}
			else if (data2.JITType == 2)
			{
				_jitType = MethodCompilationType.Ngen;
			}
			else
			{
				_jitType = MethodCompilationType.None;
			}
		}
		else
		{
			_jitType = MethodCompilationType.None;
		}
		return true;
	}
}
