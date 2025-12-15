using System.Reflection;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopMethod : ClrMethod
{
	private uint _token;

	private ILToNativeMap[] _ilMap;

	private string _sig;

	private ulong _ip;

	private MethodCompilationType _jit;

	private MethodAttributes _attrs;

	private DesktopRuntimeBase _runtime;

	private ClrType _type;

	public override string Name
	{
		get
		{
			if (_sig == null)
			{
				return null;
			}
			int num = _sig.LastIndexOf('(');
			if (num > 0)
			{
				int num2 = _sig.LastIndexOf('.', num - 1);
				if (num2 != -1 && _sig[num2 - 1] == '.')
				{
					num2--;
				}
				return _sig.Substring(num2 + 1, num - num2 - 1);
			}
			return "{error}";
		}
	}

	public override ulong NativeCode => _ip;

	public override MethodCompilationType CompilationType => _jit;

	public override bool IsStatic => (_attrs & MethodAttributes.Static) == MethodAttributes.Static;

	public override bool IsFinal => (_attrs & MethodAttributes.Final) == MethodAttributes.Final;

	public override bool IsPInvoke => (_attrs & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl;

	public override bool IsVirtual => (_attrs & MethodAttributes.Virtual) == MethodAttributes.Virtual;

	public override bool IsAbstract => (_attrs & MethodAttributes.Abstract) == MethodAttributes.Abstract;

	public override bool IsPublic => (_attrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

	public override bool IsPrivate => (_attrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

	public override bool IsInternal
	{
		get
		{
			MethodAttributes methodAttributes = _attrs & MethodAttributes.MemberAccessMask;
			if (methodAttributes != MethodAttributes.Assembly)
			{
				return methodAttributes == MethodAttributes.FamANDAssem;
			}
			return true;
		}
	}

	public override bool IsProtected
	{
		get
		{
			MethodAttributes methodAttributes = _attrs & MethodAttributes.MemberAccessMask;
			if (methodAttributes != MethodAttributes.Family && methodAttributes != MethodAttributes.FamANDAssem)
			{
				return methodAttributes == MethodAttributes.FamORAssem;
			}
			return true;
		}
	}

	public override bool IsSpecialName => (_attrs & MethodAttributes.SpecialName) == MethodAttributes.SpecialName;

	public override bool IsRTSpecialName => (_attrs & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;

	public override ILToNativeMap[] ILOffsetMap
	{
		get
		{
			if (_ilMap == null)
			{
				_ilMap = _runtime.GetILMap(_ip);
			}
			return _ilMap;
		}
	}

	public override uint MetadataToken => _token;

	public override ClrType Type => _type;

	public override string ToString()
	{
		return $"<ClrMethod signature='{_sig}' />";
	}

	internal static DesktopMethod Create(DesktopRuntimeBase runtime, IMetadata metadata, IMethodDescData mdData)
	{
		if (mdData == null)
		{
			return null;
		}
		MethodAttributes pdwAttr = MethodAttributes.PrivateScope;
		if (metadata != null && metadata.GetMethodProps(mdData.MDToken, out var _, null, 0, out var _, out pdwAttr, out var _, out var _, out var _, out var _) < 0)
		{
			pdwAttr = MethodAttributes.PrivateScope;
		}
		return new DesktopMethod(runtime, mdData.MethodDesc, mdData, pdwAttr);
	}

	internal static ClrMethod Create(DesktopRuntimeBase runtime, IMethodDescData mdData)
	{
		if (mdData == null)
		{
			return null;
		}
		return Create(runtime, runtime.GetModule(mdData.Module)?.GetMetadataImport(), mdData);
	}

	public DesktopMethod(DesktopRuntimeBase runtime, ulong md, IMethodDescData mdData, MethodAttributes attrs)
	{
		_runtime = runtime;
		_sig = runtime.GetNameForMD(md);
		_ip = mdData.NativeCodeAddr;
		_jit = mdData.JITType;
		_attrs = attrs;
		_token = mdData.MDToken;
		DesktopGCHeap desktopGCHeap = (DesktopGCHeap)runtime.GetHeap();
		_type = desktopGCHeap.GetGCHeapType(mdData.MethodTable, 0uL);
	}

	public override string GetFullSignature()
	{
		return _sig;
	}

	public override SourceLocation GetSourceLocationForOffset(ulong nativeOffset)
	{
		ClrType type = Type;
		if (type == null)
		{
			return null;
		}
		DesktopModule desktopModule = (DesktopModule)type.Module;
		if (desktopModule == null)
		{
			return null;
		}
		if (!desktopModule.IsPdbLoaded)
		{
			string text = desktopModule.TryDownloadPdb();
			if (text == null)
			{
				return null;
			}
			desktopModule.LoadPdb(text);
			if (!desktopModule.IsPdbLoaded)
			{
				return null;
			}
		}
		ILToNativeMap[] iLOffsetMap = ILOffsetMap;
		if (iLOffsetMap == null)
		{
			return null;
		}
		int ilOffset = 0;
		if (iLOffsetMap.Length > 1)
		{
			ilOffset = iLOffsetMap[1].ILOffset;
		}
		for (int i = 0; i < iLOffsetMap.Length; i++)
		{
			if (iLOffsetMap[i].StartAddress <= _ip && _ip <= iLOffsetMap[i].EndAddress)
			{
				ilOffset = iLOffsetMap[i].ILOffset;
				break;
			}
		}
		return desktopModule.GetSourceInformation(MetadataToken, ilOffset);
	}
}
