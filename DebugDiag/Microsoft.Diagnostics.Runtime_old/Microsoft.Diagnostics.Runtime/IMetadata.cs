using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime;

[Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMetadata
{
	[PreserveSig]
	void CloseEnum(IntPtr hEnum);

	void CountEnum(IntPtr hEnum, [ComAliasName("ULONG*")] out int pulCount);

	void ResetEnum(IntPtr hEnum, int ulPos);

	void EnumTypeDefs(ref IntPtr phEnum, [ComAliasName("mdTypeDef*")] out int rTypeDefs, uint cMax, [ComAliasName("ULONG*")] out uint pcTypeDefs);

	[PreserveSig]
	int EnumInterfaceImpls(ref IntPtr phEnum, int td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] rImpls, int cMax, out int pCount);

	void EnumTypeRefs_();

	void FindTypeDefByName([In][MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] int tkEnclosingClass, [ComAliasName("mdTypeDef*")] out int token);

	void GetScopeProps([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, [In] int cchName, [ComAliasName("ULONG*")] out int pchName, out Guid mvid);

	void GetModuleFromScope_();

	[PreserveSig]
	int GetTypeDefProps([In] int td, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szTypeDef, [In] int cchTypeDef, [ComAliasName("ULONG*")] out int pchTypeDef, [MarshalAs(UnmanagedType.U4)] out TypeAttributes pdwTypeDefFlags, [ComAliasName("mdToken*")] out int ptkExtends);

	[PreserveSig]
	int GetInterfaceImplProps(int mdImpl, out int mdClass, out int mdIFace);

	[PreserveSig]
	int GetTypeRefProps(int tr, [ComAliasName("mdToken*")] out int ptkResolutionScope, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, [In] int cchName, [ComAliasName("ULONG*")] out int pchName);

	void ResolveTypeRef(int tr, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object scope, out int typeDef);

	void EnumMembers_();

	void EnumMembersWithName_();

	void EnumMethods(ref IntPtr phEnum, int cl, [ComAliasName("mdMethodDef*")] out int mdMethodDef, int cMax, [ComAliasName("ULONG*")] out int pcTokens);

	void EnumMethodsWithName_();

	[PreserveSig]
	int EnumFields(ref IntPtr phEnum, int cl, [ComAliasName("mdFieldDef*")][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] mdFieldDef, int cMax, [ComAliasName("ULONG*")] out int pcTokens);

	void EnumFieldsWithName_();

	void EnumParams(ref IntPtr phEnum, int mdMethodDef, [ComAliasName("mdParamDef*")] out int mdParamDef, int cMax, [ComAliasName("ULONG*")] out uint pcTokens);

	void EnumMemberRefs_();

	void EnumMethodImpls_();

	void EnumPermissionSets_();

	void FindMember_();

	void FindMethod_();

	void FindField_();

	void FindMemberRef_();

	[PreserveSig]
	int GetMethodProps([In] uint md, [ComAliasName("mdTypeDef*")] out int pClass, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szMethod, [In] int cchMethod, [ComAliasName("ULONG*")] out int pchMethod, [ComAliasName("DWORD*")] out MethodAttributes pdwAttr, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out uint pcbSigBlob, [ComAliasName("ULONG*")] out uint pulCodeRVA, [ComAliasName("DWORD*")] out uint pdwImplFlags);

	void GetMemberRefProps([In] uint mr, [ComAliasName("mdMemberRef*")] out int ptk, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szMember, [In] int cchMember, [ComAliasName("ULONG*")] out uint pchMember, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pbSig);

	void EnumProperties(ref IntPtr phEnum, int mdTypeDef, [ComAliasName("mdPropertyDef*")] out int mdPropertyDef, int countMax, [ComAliasName("ULONG*")] out uint pcTokens);

	void EnumEvents_();

	void GetEventProps_();

	void EnumMethodSemantics_();

	void GetMethodSemantics_();

	void GetClassLayout_();

	void GetFieldMarshal_();

	void GetRVA_();

	void GetPermissionSetProps_();

	void GetSigFromToken_();

	void GetModuleRefProps_();

	void EnumModuleRefs_();

	[PreserveSig]
	int GetTypeSpecFromToken(uint token, out IntPtr sig, out int sigLen);

	void GetNameFromToken_();

	void EnumUnresolvedMethods_();

	void GetUserString([In] int stk, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szString, [In] int cchString, [ComAliasName("ULONG*")] out int pchString);

	void GetPinvokeMap_();

	void EnumSignatures_();

	void EnumTypeSpecs_();

	void EnumUserStrings_();

	void GetParamForMethodIndex_();

	void EnumCustomAttributes(ref IntPtr phEnum, int tk, int tkType, [ComAliasName("mdCustomAttribute*")] out int mdCustomAttribute, uint cMax, [ComAliasName("ULONG*")] out uint pcTokens);

	void GetCustomAttributeProps_();

	void FindTypeRef_();

	void GetMemberProps_();

	[PreserveSig]
	int GetFieldProps(int mb, [ComAliasName("mdTypeDef*")] out int mdTypeDef, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szField, int cchField, [ComAliasName("ULONG*")] out int pchField, [ComAliasName("DWORD*")] out FieldAttributes pdwAttr, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pcbSigBlob, [ComAliasName("DWORD*")] out int pdwCPlusTypeFlab, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppValue, [ComAliasName("ULONG*")] out int pcchValue);

	void GetPropertyProps(int mb, [ComAliasName("mdTypeDef*")] out int mdTypeDef, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szProperty, int cchProperty, [ComAliasName("ULONG*")] out int pchProperty, [ComAliasName("DWORD*")] out int pdwPropFlags, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pcbSigBlob, [ComAliasName("DWORD*")] out int pdwCPlusTypeFlag, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppDefaultValue, [ComAliasName("ULONG*")] out int pcchDefaultValue, [ComAliasName("mdMethodDef*")] out int mdSetter, [ComAliasName("mdMethodDef*")] out int mdGetter, [ComAliasName("mdMethodDef*")] out int rmdOtherMethod, [ComAliasName("ULONG")] int cMax, [ComAliasName("ULONG*")] out int pcOtherMethod);

	void GetParamProps(int tk, [ComAliasName("mdMethodDef*")] out int pmd, [ComAliasName("ULONG*")] out uint pulSequence, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, uint cchName, [ComAliasName("ULONG*")] out uint pchName, [ComAliasName("DWORD*")] out uint pdwAttr, [ComAliasName("DWORD*")] out uint pdwCPlusTypeFlag, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppValue, [ComAliasName("ULONG*")] out uint pcchValue);

	[PreserveSig]
	int GetCustomAttributeByName(int tkObj, [MarshalAs(UnmanagedType.LPWStr)] string szName, out IntPtr ppData, out uint pcbData);

	[PreserveSig]
	bool IsValidToken([In][MarshalAs(UnmanagedType.U4)] uint tk);

	[PreserveSig]
	int GetNestedClassProps(int tdNestedClass, [ComAliasName("mdTypeDef*")] out int tdEnclosingClass);

	void GetNativeCallConvFromSig_();

	void IsGlobal_();
}
