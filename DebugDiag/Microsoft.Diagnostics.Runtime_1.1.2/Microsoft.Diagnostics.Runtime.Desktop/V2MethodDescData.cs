namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct V2MethodDescData : IMethodDescData
{
	public readonly int HasNativeCode;

	public readonly int IsDynamic;

	public readonly short SlotNumber;

	public readonly ulong NativeCodeAddr;

	public readonly ulong AddressOfNativeCodeSlot;

	public readonly ulong MethodDescPtr;

	public readonly ulong MethodTablePtr;

	public readonly ulong EEClassPtr;

	public readonly ulong ModulePtr;

	public readonly ulong PreStubAddr;

	public readonly uint MDToken;

	public readonly ulong GCInfo;

	public readonly short JITType;

	public readonly ulong GCStressCodeCopy;

	public readonly ulong ManagedDynamicMethodObject;

	ulong IMethodDescData.MethodDesc => MethodDescPtr;

	ulong IMethodDescData.Module => ModulePtr;

	uint IMethodDescData.MDToken => MDToken;

	ulong IMethodDescData.NativeCodeAddr => NativeCodeAddr;

	ulong IMethodDescData.MethodTable => MethodTablePtr;

	ulong IMethodDescData.GCInfo => GCInfo;

	ulong IMethodDescData.ColdStart => 0uL;

	uint IMethodDescData.ColdSize => 0u;

	uint IMethodDescData.HotSize => 0u;

	MethodCompilationType IMethodDescData.JITType
	{
		get
		{
			if (JITType == 1)
			{
				return MethodCompilationType.Jit;
			}
			if (JITType == 2)
			{
				return MethodCompilationType.Ngen;
			}
			return MethodCompilationType.None;
		}
	}
}
