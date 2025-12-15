namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V45MethodDescData
{
	private uint _bHasNativeCode;

	private uint _bIsDynamic;

	private short _wSlotNumber;

	internal ulong NativeCodeAddr;

	private ulong _addressOfNativeCodeSlot;

	internal ulong MethodDescPtr;

	internal ulong MethodTablePtr;

	internal ulong ModulePtr;

	internal uint MDToken;

	private ulong _GCInfo;

	private ulong _GCStressCodeCopy;

	private ulong _managedDynamicMethodObject;

	private ulong _requestedIP;

	private V45ReJitData _rejitDataCurrent;

	private V45ReJitData _rejitDataRequested;

	private uint _cJittedRejitVersions;
}
