using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct IXCLRDataProcessVtable
{
	public readonly IntPtr Flush;

	private readonly IntPtr Unused_StartEnumTasks;

	private readonly IntPtr EnumTask;

	private readonly IntPtr EndEnumTasks;

	public readonly IntPtr GetTaskByOSThreadID;

	private readonly IntPtr GetTaskByUniqueID;

	private readonly IntPtr GetFlags;

	private readonly IntPtr IsSameObject;

	private readonly IntPtr GetManagedObject;

	private readonly IntPtr GetDesiredExecutionState;

	private readonly IntPtr SetDesiredExecutionState;

	private readonly IntPtr GetAddressType;

	private readonly IntPtr GetRuntimeNameByAddress;

	private readonly IntPtr StartEnumAppDomains;

	private readonly IntPtr EnumAppDomain;

	private readonly IntPtr EndEnumAppDomains;

	private readonly IntPtr GetAppDomainByUniqueID;

	private readonly IntPtr StartEnumAssemblie;

	private readonly IntPtr EnumAssembly;

	private readonly IntPtr EndEnumAssemblies;

	private readonly IntPtr StartEnumModules;

	private readonly IntPtr EnumModule;

	private readonly IntPtr EndEnumModules;

	private readonly IntPtr GetModuleByAddress;

	public readonly IntPtr StartEnumMethodInstancesByAddress;

	public readonly IntPtr EnumMethodInstanceByAddress;

	public readonly IntPtr EndEnumMethodInstancesByAddress;

	private readonly IntPtr GetDataByAddress;

	private readonly IntPtr GetExceptionStateByExceptionRecord;

	private readonly IntPtr TranslateExceptionRecordToNotification;

	public readonly IntPtr Request;
}
