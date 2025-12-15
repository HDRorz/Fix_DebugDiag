using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.ICorDebug;

[ComImport]
[ComConversionLoss]
[InterfaceType(1)]
[Guid("DBA2D8C1-E5C5-4069-8C13-10A7C6ABF43D")]
public interface ICorDebugModule
{
	void GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);

	void GetBaseAddress(out ulong pAddress);

	void GetAssembly([MarshalAs(UnmanagedType.Interface)] out ICorDebugAssembly ppAssembly);

	void GetName([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.LPArray)] char[] szName);

	void EnableJITDebugging([In] int bTrackJITInfo, [In] int bAllowJitOpts);

	void EnableClassLoadCallbacks([In] int bClassLoadCallbacks);

	void GetFunctionFromToken([In] uint methodDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetFunctionFromRVA([In] ulong rva, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);

	void GetClassFromToken([In] uint typeDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);

	void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleBreakpoint ppBreakpoint);

	void GetEditAndContinueSnapshot([MarshalAs(UnmanagedType.Interface)] out ICorDebugEditAndContinueSnapshot ppEditAndContinueSnapshot);

	void GetMetaDataInterface([In][ComAliasName("REFIID")] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IMetadataImport ppObj);

	void GetToken(out uint pToken);

	void IsDynamic(out int pDynamic);

	void GetGlobalVariableValue([In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);

	void GetSize(out uint pcBytes);

	void IsInMemory(out int pInMemory);
}
