using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopMethod : ClrMethod
{
	private readonly uint _token;

	private ILToNativeMap[] _ilMap;

	private readonly string _sig;

	private readonly ulong _ip;

	private readonly MethodAttributes _attrs;

	private readonly DesktopRuntimeBase _runtime;

	private readonly DesktopHeapType _type;

	private List<ulong> _methodHandles;

	private ILInfo _il;

	public override ulong MethodDesc
	{
		get
		{
			if (_methodHandles != null && _methodHandles[0] != 0L)
			{
				return _methodHandles[0];
			}
			return EnumerateMethodDescs().FirstOrDefault();
		}
	}

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

	public override MethodCompilationType CompilationType { get; }

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

	public override HotColdRegions HotColdInfo { get; }

	public override ILToNativeMap[] ILOffsetMap
	{
		get
		{
			if (_ilMap == null)
			{
				_ilMap = _runtime.GetILMap(_ip, HotColdInfo);
			}
			return _ilMap;
		}
	}

	public override uint MetadataToken => _token;

	public override ClrType Type => _type;

	public override ulong GCInfo { get; }

	public override ILInfo IL
	{
		get
		{
			if (_il == null)
			{
				InitILInfo();
			}
			return _il;
		}
	}

	public override string ToString()
	{
		return _sig;
	}

	internal static DesktopMethod Create(DesktopRuntimeBase runtime, MetaDataImport metadata, IMethodDescData mdData)
	{
		if (mdData == null)
		{
			return null;
		}
		MethodAttributes attrs = MethodAttributes.PrivateScope;
		if (metadata != null)
		{
			attrs = metadata.GetMethodAttributes((int)mdData.MDToken);
		}
		return new DesktopMethod(runtime, mdData.MethodDesc, mdData, attrs);
	}

	internal void AddMethodHandle(ulong methodDesc)
	{
		if (_methodHandles == null)
		{
			_methodHandles = new List<ulong>(1);
		}
		_methodHandles.Add(methodDesc);
	}

	public override IEnumerable<ulong> EnumerateMethodDescs()
	{
		if (_methodHandles == null)
		{
			_type?.InitMethodHandles();
		}
		if (_methodHandles == null)
		{
			_methodHandles = new List<ulong>();
		}
		return _methodHandles;
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
		CompilationType = mdData.JITType;
		_attrs = attrs;
		_token = mdData.MDToken;
		GCInfo = mdData.GCInfo;
		ClrHeap heap = runtime.Heap;
		_type = (DesktopHeapType)heap.GetTypeByMethodTable(mdData.MethodTable, 0uL);
		HotColdInfo = new HotColdRegions
		{
			HotStart = _ip,
			HotSize = mdData.HotSize,
			ColdStart = mdData.ColdStart,
			ColdSize = mdData.ColdSize
		};
	}

	public override string GetFullSignature()
	{
		return _sig;
	}

	public override int GetILOffset(ulong addr)
	{
		ILToNativeMap[] iLOffsetMap = ILOffsetMap;
		int result = 0;
		if (iLOffsetMap.Length > 1)
		{
			result = iLOffsetMap[1].ILOffset;
		}
		for (int i = 0; i < iLOffsetMap.Length; i++)
		{
			if (iLOffsetMap[i].StartAddress <= addr && addr <= iLOffsetMap[i].EndAddress)
			{
				return iLOffsetMap[i].ILOffset;
			}
		}
		return result;
	}

	private void InitILInfo()
	{
		ClrModule clrModule = Type?.Module;
		object obj = clrModule?.MetadataImport;
		uint pRva = 0u;
		if (obj is IMetadataImport metadataImport)
		{
			if (metadataImport.GetRVA(_token, out pRva, out var _) != 0)
			{
				return;
			}
		}
		else if (obj is MetaDataImport metaDataImport)
		{
			pRva = metaDataImport.GetRva((int)_token);
		}
		ulong iLForModule = _runtime.GetILForModule(clrModule, pRva);
		if (iLForModule == 0L)
		{
			return;
		}
		_il = new ILInfo();
		if (_runtime.ReadByte(iLForModule, out byte value))
		{
			uint value2;
			if ((value & 3) == 2)
			{
				_il.Address = iLForModule + 1;
				_il.Length = value >> 2;
				_il.LocalVarSignatureToken = 285212672u;
			}
			else if (_runtime.ReadDword(iLForModule, out value2))
			{
				_il.Flags = value2;
				_runtime.ReadDword(iLForModule + 4, out value2);
				_il.Length = (int)value2;
				_runtime.ReadDword(iLForModule + 8, out value2);
				_il.LocalVarSignatureToken = value2;
				_il.Address = iLForModule + 12;
			}
		}
	}
}
