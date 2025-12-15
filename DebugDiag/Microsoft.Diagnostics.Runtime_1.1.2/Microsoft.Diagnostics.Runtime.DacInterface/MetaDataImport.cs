using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class MetaDataImport : CallableCOMWrapper
{
	private delegate int CloseEnumDelegate(IntPtr self, IntPtr e);

	private delegate int EnumInterfaceImplsDelegate(IntPtr self, ref IntPtr phEnum, int td, [Out] int[] rImpls, int cMax, out int pCount);

	private delegate int GetInterfaceImplPropsDelegate(IntPtr self, int mdImpl, out int mdClass, out int mdIFace);

	private delegate int GetTypeRefPropsDelegate(IntPtr self, int token, out int resolutionScopeToken, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName, int bufferSize, out int needed);

	private delegate int GetTypeDefPropsDelegate(IntPtr self, int token, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szTypeDef, int cchTypeDef, out int pchTypeDef, out TypeAttributes pdwTypeDefFlags, out int ptkExtends);

	private delegate int EnumFieldsDelegate(IntPtr self, ref IntPtr phEnum, int cl, int[] mdFieldDef, int cMax, out int pcTokens);

	private delegate int GetRVADelegate(IntPtr self, int token, out uint pRva, out uint flags);

	private delegate int GetMethodPropsDelegate(IntPtr self, int md, out int pClass, StringBuilder szMethod, int cchMethod, out int pchMethod, out MethodAttributes pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags);

	private delegate int GetNestedClassPropsDelegate(IntPtr self, int tdNestedClass, out int tdEnclosingClass);

	private delegate int GetFieldPropsDelegate(IntPtr self, int mb, out int mdTypeDef, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder szField, int cchField, out int pchField, out FieldAttributes pdwAttr, out IntPtr ppvSigBlob, out int pcbSigBlob, out int pdwCPlusTypeFlab, out IntPtr ppValue, out int pcchValue);

	private delegate int GetCustomAttributeByNameDelegate(IntPtr self, int tkObj, [MarshalAs(UnmanagedType.LPWStr)] string szName, out IntPtr ppData, out uint pcbData);

	private static Guid IID_IMetaDataImport = new Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44");

	private EnumInterfaceImplsDelegate _enumInterfaceImpls;

	private CloseEnumDelegate _closeEnum;

	private GetInterfaceImplPropsDelegate _getInterfaceImplProps;

	private GetTypeRefPropsDelegate _getTypeRefProps;

	private GetTypeDefPropsDelegate _getTypeDefProps;

	private EnumFieldsDelegate _enumFields;

	private GetRVADelegate _getRVA;

	private GetMethodPropsDelegate _getMethodProps;

	private GetNestedClassPropsDelegate _getNestedClassProps;

	private GetFieldPropsDelegate _getFieldProps;

	private GetCustomAttributeByNameDelegate _getCustomAttributeByName;

	private int[] _tokens;

	private unsafe IMetaDataImportVTable* VTable => (IMetaDataImportVTable*)base._vtable;

	private unsafe IntPtr EnumInterfaceImpls => VTable->EnumInterfaceImpls;

	private unsafe IntPtr EnumFieldPtr => VTable->EnumFields;

	public MetaDataImport(DacLibrary library, IntPtr pUnknown)
		: base(library.OwningLibrary, ref IID_IMetaDataImport, pUnknown)
	{
	}

	public IEnumerable<int> EnumerateInterfaceImpls(int token)
	{
		CallableCOMWrapper.InitDelegate(ref _enumInterfaceImpls, EnumInterfaceImpls);
		IntPtr handle = IntPtr.Zero;
		int[] tokens = AcquireIntArray();
		int count;
		while (_enumInterfaceImpls(base.Self, ref handle, token, tokens, tokens.Length, out count) >= 0 && count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				yield return tokens[i];
			}
		}
		ReleaseIntArray(tokens);
		CloseEnum(handle);
	}

	public unsafe MethodAttributes GetMethodAttributes(int token)
	{
		CallableCOMWrapper.InitDelegate(ref _getMethodProps, VTable->GetMethodProps);
		if (_getMethodProps(base.Self, token, out var _, null, 0, out var _, out var pdwAttr, out var _, out var _, out var _, out var _) != 0)
		{
			return MethodAttributes.PrivateScope;
		}
		return pdwAttr;
	}

	public unsafe uint GetRva(int token)
	{
		CallableCOMWrapper.InitDelegate(ref _getRVA, VTable->GetRVA);
		if (_getRVA(base.Self, token, out var pRva, out var _) != 0)
		{
			return 0u;
		}
		return pRva;
	}

	public unsafe bool GetTypeDefProperties(int token, out string name, out TypeAttributes attributes, out int mdParent)
	{
		CallableCOMWrapper.InitDelegate(ref _getTypeDefProps, VTable->GetTypeDefProps);
		name = null;
		if (_getTypeDefProps(base.Self, token, null, 0, out var pchTypeDef, out attributes, out mdParent) < 0)
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder(pchTypeDef + 1);
		if (_getTypeDefProps(base.Self, token, stringBuilder, stringBuilder.Capacity, out pchTypeDef, out attributes, out mdParent) != 0)
		{
			return false;
		}
		name = stringBuilder.ToString();
		return true;
	}

	public unsafe bool GetCustomAttributeByName(int token, string name, out IntPtr data, out uint cbData)
	{
		CallableCOMWrapper.InitDelegate(ref _getCustomAttributeByName, VTable->GetCustomAttributeByName);
		return _getCustomAttributeByName(base.Self, token, name, out data, out cbData) == 0;
	}

	public unsafe bool GetFieldProps(int token, out string name, out FieldAttributes attrs, out IntPtr ppvSigBlob, out int pcbSigBlob, out int pdwCPlusTypeFlag, out IntPtr ppValue)
	{
		CallableCOMWrapper.InitDelegate(ref _getFieldProps, VTable->GetFieldProps);
		name = null;
		if (_getFieldProps(base.Self, token, out var mdTypeDef, null, 0, out var pchField, out attrs, out ppvSigBlob, out pcbSigBlob, out pdwCPlusTypeFlag, out ppValue, out var pcchValue) < 0)
		{
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder(pchField + 1);
		if (_getFieldProps(base.Self, token, out mdTypeDef, stringBuilder, stringBuilder.Capacity, out pchField, out attrs, out ppvSigBlob, out pcbSigBlob, out pdwCPlusTypeFlag, out ppValue, out pcchValue) >= 0)
		{
			name = stringBuilder.ToString();
			return true;
		}
		return false;
	}

	public IEnumerable<int> EnumerateFields(int token)
	{
		CallableCOMWrapper.InitDelegate(ref _enumFields, EnumFieldPtr);
		int[] tokens = AcquireIntArray();
		IntPtr handle = IntPtr.Zero;
		int count;
		while (_enumFields(base.Self, ref handle, token, tokens, tokens.Length, out count) >= 0 && count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				yield return tokens[i];
			}
		}
		CloseEnum(handle);
		ReleaseIntArray(tokens);
	}

	internal unsafe bool GetTypeDefAttributes(int token, out TypeAttributes attrs)
	{
		CallableCOMWrapper.InitDelegate(ref _getTypeDefProps, VTable->GetTypeDefProps);
		int pchTypeDef;
		int ptkExtends;
		return _getTypeDefProps(base.Self, token, null, 0, out pchTypeDef, out attrs, out ptkExtends) == 0;
	}

	public unsafe string GetTypeRefName(int token)
	{
		CallableCOMWrapper.InitDelegate(ref _getTypeRefProps, VTable->GetTypeRefProps);
		if (_getTypeRefProps(base.Self, token, out var resolutionScopeToken, null, 0, out var needed) < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(needed + 1);
		if (_getTypeRefProps(base.Self, token, out resolutionScopeToken, stringBuilder, stringBuilder.Capacity, out needed) != 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	public unsafe bool GetNestedClassProperties(int token, out int enclosing)
	{
		CallableCOMWrapper.InitDelegate(ref _getNestedClassProps, VTable->GetNestedClassProps);
		return _getNestedClassProps(base.Self, token, out enclosing) == 0;
	}

	public unsafe bool GetInterfaceImplProps(int token, out int mdClass, out int mdInterface)
	{
		CallableCOMWrapper.InitDelegate(ref _getInterfaceImplProps, VTable->GetInterfaceImplProps);
		return _getInterfaceImplProps(base.Self, token, out mdClass, out mdInterface) == 0;
	}

	private unsafe void CloseEnum(IntPtr handle)
	{
		if (handle != IntPtr.Zero)
		{
			CallableCOMWrapper.InitDelegate(ref _closeEnum, VTable->CloseEnum);
			_closeEnum(base.Self, handle);
		}
	}

	private void ReleaseIntArray(int[] tokens)
	{
		_tokens = tokens;
	}

	private int[] AcquireIntArray()
	{
		int[] array = _tokens;
		_tokens = null;
		if (array == null)
		{
			array = new int[32];
		}
		return array;
	}
}
