namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V35MethodDescData : IMethodDescData
{
	private int _bHasNativeCode;

	private int _bIsDynamic;

	private short _wSlotNumber;

	private ulong _nativeCodeAddr;

	private ulong _addressOfNativeCodeSlot;

	private ulong _methodDescPtr;

	private ulong _methodTablePtr;

	private ulong _EEClassPtr;

	private ulong _modulePtr;

	private uint _mdToken;

	private ulong _GCInfo;

	private short _JITType;

	private ulong _GCStressCodeCopy;

	private ulong _managedDynamicMethodObject;

	public ulong MethodTable => _methodTablePtr;

	public ulong MethodDesc => _methodDescPtr;

	public ulong Module => _modulePtr;

	public uint MDToken => _mdToken;

	ulong IMethodDescData.NativeCodeAddr => _nativeCodeAddr;

	MethodCompilationType IMethodDescData.JITType
	{
		get
		{
			if (_JITType == 1)
			{
				return MethodCompilationType.Jit;
			}
			if (_JITType == 2)
			{
				return MethodCompilationType.Ngen;
			}
			return MethodCompilationType.None;
		}
	}
}
