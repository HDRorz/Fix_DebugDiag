using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime;

[Guid("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMetadataImport2 : IMetadata
{
	[PreserveSig]
	new void CloseEnum(IntPtr hEnum);

	new void CountEnum(IntPtr hEnum, [ComAliasName("ULONG*")] out int pulCount);

	new void ResetEnum(IntPtr hEnum, int ulPos);

	new void EnumTypeDefs(ref IntPtr phEnum, [ComAliasName("mdTypeDef*")] out int rTypeDefs, uint cMax, [ComAliasName("ULONG*")] out uint pcTypeDefs);

	new int EnumInterfaceImpls(ref IntPtr phEnum, int td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] rImpls, int cMax, out int pCount);

	new void EnumTypeRefs_();

	new void FindTypeDefByName([In][MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] int tkEnclosingClass, [ComAliasName("mdTypeDef*")] out int token);

	new void GetScopeProps([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, [In] int cchName, [ComAliasName("ULONG*")] out int pchName, out Guid mvid);

	new void GetModuleFromScope_();

	new void GetTypeDefProps([In] int td, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szTypeDef, [In] int cchTypeDef, [ComAliasName("ULONG*")] out int pchTypeDef, [MarshalAs(UnmanagedType.U4)] out TypeAttributes pdwTypeDefFlags, [ComAliasName("mdToken*")] out int ptkExtends);

	[PreserveSig]
	new int GetInterfaceImplProps(int mdImpl, out int mdClass, out int mdIFace);

	new void GetTypeRefProps(int tr, [ComAliasName("mdToken*")] out int ptkResolutionScope, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, [In] int cchName, [ComAliasName("ULONG*")] out int pchName);

	new void ResolveTypeRef(int tr, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object scope, out int typeDef);

	new void EnumMembers_();

	new void EnumMembersWithName_();

	new void EnumMethods(ref IntPtr phEnum, int cl, [ComAliasName("mdMethodDef*")] out int mdMethodDef, int cMax, [ComAliasName("ULONG*")] out int pcTokens);

	new void EnumMethodsWithName_();

	[PreserveSig]
	new int EnumFields(ref IntPtr phEnum, int cl, [ComAliasName("mdFieldDef*")][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] mdFieldDef, int cMax, [ComAliasName("ULONG*")] out int pcTokens);

	new void EnumFieldsWithName_();

	new void EnumParams(ref IntPtr phEnum, int mdMethodDef, [ComAliasName("mdParamDef*")] out int mdParamDef, int cMax, [ComAliasName("ULONG*")] out uint pcTokens);

	new void EnumMemberRefs_();

	new void EnumMethodImpls_();

	new void EnumPermissionSets_();

	new void FindMember_();

	new void FindMethod_();

	new void FindField_();

	new void FindMemberRef_();

	[PreserveSig]
	new int GetMethodProps([In] uint md, [ComAliasName("mdTypeDef*")] out int pClass, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szMethod, [In] int cchMethod, [ComAliasName("ULONG*")] out int pchMethod, [ComAliasName("DWORD*")] out MethodAttributes pdwAttr, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out uint pcbSigBlob, [ComAliasName("ULONG*")] out uint pulCodeRVA, [ComAliasName("DWORD*")] out uint pdwImplFlags);

	new void GetMemberRefProps([In] uint mr, [ComAliasName("mdMemberRef*")] out int ptk, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szMember, [In] int cchMember, [ComAliasName("ULONG*")] out uint pchMember, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pbSig);

	new void EnumProperties(ref IntPtr phEnum, int mdTypeDef, [ComAliasName("mdPropertyDef*")] out int mdPropertyDef, int countMax, [ComAliasName("ULONG*")] out uint pcTokens);

	new void EnumEvents_();

	new void GetEventProps_();

	new void EnumMethodSemantics_();

	new void GetMethodSemantics_();

	new void GetClassLayout_();

	new void GetFieldMarshal_();

	new void GetRVA_();

	new void GetPermissionSetProps_();

	new void GetSigFromToken_();

	new void GetModuleRefProps_();

	new void EnumModuleRefs_();

	[PreserveSig]
	new int GetTypeSpecFromToken(uint token, out IntPtr sig, out int sigLen);

	new void GetNameFromToken_();

	new void EnumUnresolvedMethods_();

	new void GetUserString([In] int stk, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szString, [In] int cchString, [ComAliasName("ULONG*")] out int pchString);

	new void GetPinvokeMap_();

	new void EnumSignatures_();

	new void EnumTypeSpecs_();

	new void EnumUserStrings_();

	new void GetParamForMethodIndex_();

	new void EnumCustomAttributes(ref IntPtr phEnum, int tk, int tkType, [ComAliasName("mdCustomAttribute*")] out int mdCustomAttribute, uint cMax, [ComAliasName("ULONG*")] out uint pcTokens);

	new void GetCustomAttributeProps_();

	new void FindTypeRef_();

	new void GetMemberProps_();

	new void GetFieldProps(int mb, [ComAliasName("mdTypeDef*")] out int mdTypeDef, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szField, int cchField, [ComAliasName("ULONG*")] out int pchField, [ComAliasName("DWORD*")] out FieldAttributes pdwAttr, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pcbSigBlob, [ComAliasName("DWORD*")] out int pdwCPlusTypeFlab, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppValue, [ComAliasName("ULONG*")] out int pcchValue);

	new void GetPropertyProps(int mb, [ComAliasName("mdTypeDef*")] out int mdTypeDef, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szProperty, int cchProperty, [ComAliasName("ULONG*")] out int pchProperty, [ComAliasName("DWORD*")] out int pdwPropFlags, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pcbSigBlob, [ComAliasName("DWORD*")] out int pdwCPlusTypeFlag, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppDefaultValue, [ComAliasName("ULONG*")] out int pcchDefaultValue, [ComAliasName("mdMethodDef*")] out int mdSetter, [ComAliasName("mdMethodDef*")] out int mdGetter, [ComAliasName("mdMethodDef*")] out int rmdOtherMethod, [ComAliasName("ULONG")] int cMax, [ComAliasName("ULONG*")] out int pcOtherMethod);

	new void GetParamProps(int tk, [ComAliasName("mdMethodDef*")] out int pmd, [ComAliasName("ULONG*")] out uint pulSequence, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, uint cchName, [ComAliasName("ULONG*")] out uint pchName, [ComAliasName("DWORD*")] out uint pdwAttr, [ComAliasName("DWORD*")] out uint pdwCPlusTypeFlag, [ComAliasName("UVCP_CONSTANT*")] out IntPtr ppValue, [ComAliasName("ULONG*")] out uint pcchValue);

	[PreserveSig]
	new int GetCustomAttributeByName(int tkObj, [MarshalAs(UnmanagedType.LPWStr)] string szName, out IntPtr ppData, out uint pcbData);

	[PreserveSig]
	new bool IsValidToken([In][MarshalAs(UnmanagedType.U4)] uint tk);

	new void GetNestedClassProps(int tdNestedClass, [ComAliasName("mdTypeDef*")] out int tdEnclosingClass);

	new void GetNativeCallConvFromSig_();

	new void IsGlobal_();

	void EnumGenericParams(ref IntPtr hEnum, int tk, [ComAliasName("mdGenericParam*")] out int rGenericParams, uint cMax, [ComAliasName("ULONG*")] out uint pcGenericParams);

	void GetGenericParamProps(int gp, [ComAliasName("ULONG*")] out uint pulParamSeq, [ComAliasName("DWORD*")] out int pdwParamFlags, [ComAliasName("mdToken*")] out int ptOwner, [ComAliasName("mdToken*")] out int ptkKind, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzName, ulong cchName, [ComAliasName("ULONG*")] out ulong pchName);

	void GetMethodSpecProps([ComAliasName("mdMethodSpec")] int mi, [ComAliasName("mdToken*")] out int tkParent, [ComAliasName("PCCOR_SIGNATURE*")] out IntPtr ppvSigBlob, [ComAliasName("ULONG*")] out int pcbSigBlob);

	void EnumGenericParamConstraints_();

	void GetGenericParamConstraintProps_();

	void GetPEKind_();

	void GetVersionString_();

	void EnumMethodSpecs_();
}
