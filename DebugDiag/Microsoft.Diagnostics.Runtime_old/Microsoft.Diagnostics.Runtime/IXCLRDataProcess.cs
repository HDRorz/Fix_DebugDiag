using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7")]
internal interface IXCLRDataProcess
{
	void Flush();

	void StartEnumTasks_do_not_use();

	void EnumTask_do_not_use();

	void EndEnumTasks_do_not_use();

	[PreserveSig]
	int GetTaskByOSThreadID(uint id, [MarshalAs(UnmanagedType.IUnknown)] out object task);

	void GetTaskByUniqueID_do_not_use();

	void GetFlags_do_not_use();

	void IsSameObject_do_not_use();

	void GetManagedObject_do_not_use();

	void GetDesiredExecutionState_do_not_use();

	void SetDesiredExecutionState_do_not_use();

	void GetAddressType_do_not_use();

	void GetRuntimeNameByAddress_do_not_use();

	void StartEnumAppDomains_do_not_use();

	void EnumAppDomain_do_not_use();

	void EndEnumAppDomains_do_not_use();

	void GetAppDomainByUniqueID_do_not_use();

	void StartEnumAssemblie_do_not_uses();

	void EnumAssembly_do_not_use();

	void EndEnumAssemblies_do_not_use();

	void StartEnumModules_do_not_use();

	void EnumModule_do_not_use();

	void EndEnumModules_do_not_use();

	void GetModuleByAddress_do_not_use();

	[PreserveSig]
	int StartEnumMethodInstancesByAddress(ulong address, [In][MarshalAs(UnmanagedType.Interface)] object appDomain, out ulong handle);

	[PreserveSig]
	int EnumMethodInstanceByAddress(ref ulong handle, [MarshalAs(UnmanagedType.Interface)] out object method);

	[PreserveSig]
	int EndEnumMethodInstancesByAddress(ulong handle);

	void GetDataByAddress_do_not_use();

	void GetExceptionStateByExceptionRecord_do_not_use();

	void TranslateExceptionRecordToNotification_do_not_use();

	[PreserveSig]
	int Request(uint reqCode, uint inBufferSize, [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] inBuffer, uint outBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outBuffer);
}
