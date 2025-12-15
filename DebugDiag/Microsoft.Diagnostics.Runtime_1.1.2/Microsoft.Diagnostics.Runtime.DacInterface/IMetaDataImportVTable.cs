using System;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal struct IMetaDataImportVTable
{
	public readonly IntPtr CloseEnum;

	private readonly IntPtr CountEnum;

	private readonly IntPtr ResetEnum;

	private readonly IntPtr EnumTypeDefs;

	public readonly IntPtr EnumInterfaceImpls;

	private readonly IntPtr EnumTypeRefs;

	private readonly IntPtr FindTypeDefByName;

	private readonly IntPtr GetScopeProps;

	private readonly IntPtr GetModuleFromScope;

	public readonly IntPtr GetTypeDefProps;

	public readonly IntPtr GetInterfaceImplProps;

	public readonly IntPtr GetTypeRefProps;

	private readonly IntPtr ResolveTypeRef;

	private readonly IntPtr EnumMembers;

	private readonly IntPtr EnumMembersWithName;

	private readonly IntPtr EnumMethods;

	private readonly IntPtr EnumMethodsWithName;

	public readonly IntPtr EnumFields;

	private readonly IntPtr EnumFieldsWithName;

	private readonly IntPtr EnumParams;

	private readonly IntPtr EnumMemberRefs;

	private readonly IntPtr EnumMethodImpls;

	private readonly IntPtr EnumPermissionSets;

	private readonly IntPtr FindMember;

	private readonly IntPtr FindMethod;

	private readonly IntPtr FindField;

	private readonly IntPtr FindMemberRef;

	public readonly IntPtr GetMethodProps;

	private readonly IntPtr GetMemberRefProps;

	private readonly IntPtr EnumProperties;

	private readonly IntPtr EnumEvents;

	private readonly IntPtr GetEventProps;

	private readonly IntPtr EnumMethodSemantics;

	private readonly IntPtr GetMethodSemantics;

	private readonly IntPtr GetClassLayout;

	private readonly IntPtr GetFieldMarshal;

	public readonly IntPtr GetRVA;

	private readonly IntPtr GetPermissionSetProps;

	private readonly IntPtr GetSigFromToken;

	private readonly IntPtr GetModuleRefProps;

	private readonly IntPtr EnumModuleRefs;

	private readonly IntPtr GetTypeSpecFromToken;

	private readonly IntPtr GetNameFromToken;

	private readonly IntPtr EnumUnresolvedMethods;

	private readonly IntPtr GetUserString;

	private readonly IntPtr GetPinvokeMap;

	private readonly IntPtr EnumSignatures;

	private readonly IntPtr EnumTypeSpecs;

	private readonly IntPtr EnumUserStrings;

	private readonly IntPtr GetParamForMethodIndex;

	private readonly IntPtr EnumCustomAttributes;

	private readonly IntPtr GetCustomAttributeProps;

	private readonly IntPtr FindTypeRef;

	private readonly IntPtr GetMemberProps;

	public readonly IntPtr GetFieldProps;

	private readonly IntPtr GetPropertyProps;

	private readonly IntPtr GetParamProps;

	public readonly IntPtr GetCustomAttributeByName;

	private readonly IntPtr IsValidToken;

	public readonly IntPtr GetNestedClassProps;

	private readonly IntPtr GetNativeCallConvFromSig;

	private readonly IntPtr IsGlobal;
}
