namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct MethodDescData
{
	public readonly uint HasNativeCode;

	public readonly uint IsDynamic;

	public readonly short SlotNumber;

	public readonly ulong NativeCodeAddr;

	public readonly ulong AddressOfNativeCodeSlot;

	public readonly ulong MethodDesc;

	public readonly ulong MethodTable;

	public readonly ulong Module;

	public readonly uint MDToken;

	public readonly ulong GCInfo;

	public readonly ulong GCStressCodeCopy;

	public readonly ulong ManagedDynamicMethodObject;

	public readonly ulong RequestedIP;

	public readonly RejitData RejitDataCurrent;

	public readonly RejitData RejitDataRequested;

	public readonly uint JittedRejitVersions;
}
