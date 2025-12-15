using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class BaseDesktopHeapType : ClrType
{
	protected ClrElementType _elementType;

	protected uint _token;

	private IList<ClrInterface> _interfaces;

	public bool Shared { get; protected set; }

	internal DesktopGCHeap DesktopHeap { get; set; }

	internal DesktopBaseModule DesktopModule { get; set; }

	public override ClrElementType ElementType
	{
		get
		{
			return _elementType;
		}
		internal set
		{
			_elementType = value;
		}
	}

	public override uint MetadataToken => _token;

	public override IList<ClrInterface> Interfaces
	{
		get
		{
			if (_interfaces == null)
			{
				InitInterfaces();
			}
			return _interfaces;
		}
	}

	internal abstract ulong GetModuleAddress(ClrAppDomain domain);

	public BaseDesktopHeapType(DesktopGCHeap heap, DesktopBaseModule module, uint token)
	{
		DesktopHeap = heap;
		DesktopModule = module;
		_token = token;
	}

	public List<ClrInterface> InitInterfaces()
	{
		if (DesktopModule == null)
		{
			_interfaces = DesktopHeap.EmptyInterfaceList;
			return null;
		}
		List<ClrInterface> list = ((BaseType is BaseDesktopHeapType baseDesktopHeapType) ? new List<ClrInterface>(baseDesktopHeapType.Interfaces) : null);
		IMetadata metadataImport = DesktopModule.GetMetadataImport();
		if (metadataImport == null)
		{
			_interfaces = DesktopHeap.EmptyInterfaceList;
			return null;
		}
		IntPtr phEnum = IntPtr.Zero;
		int[] array = new int[32];
		int pCount;
		while (metadataImport.EnumInterfaceImpls(ref phEnum, (int)_token, array, array.Length, out pCount) >= 0)
		{
			for (int i = 0; i < pCount; i++)
			{
				metadataImport.GetInterfaceImplProps(array[i], out var _, out var mdIFace);
				if (list == null)
				{
					list = new List<ClrInterface>((pCount == array.Length) ? 64 : pCount);
				}
				ClrInterface @interface = GetInterface(metadataImport, mdIFace);
				if (@interface != null && !list.Contains(@interface))
				{
					list.Add(@interface);
				}
			}
			if (pCount != array.Length)
			{
				break;
			}
		}
		metadataImport.CloseEnum(phEnum);
		if (list == null)
		{
			_interfaces = DesktopHeap.EmptyInterfaceList;
		}
		else
		{
			_interfaces = list.ToArray();
		}
		return list;
	}

	private ClrInterface GetInterface(IMetadata import, int mdIFace)
	{
		StringBuilder stringBuilder = new StringBuilder(1024);
		int pchTypeDef;
		TypeAttributes pdwTypeDefFlags;
		int ptkExtends;
		int typeDefProps = import.GetTypeDefProps(mdIFace, stringBuilder, stringBuilder.Capacity, out pchTypeDef, out pdwTypeDefFlags, out ptkExtends);
		string text = null;
		ClrInterface value = null;
		switch (typeDefProps)
		{
		case 0:
			text = stringBuilder.ToString();
			break;
		case 1:
		{
			if (import.GetTypeRefProps(mdIFace, out var _, stringBuilder, stringBuilder.Capacity, out pchTypeDef) == 0)
			{
				text = stringBuilder.ToString();
			}
			else
			{
				_ = 1;
			}
			break;
		}
		}
		if (text != null && !DesktopHeap.Interfaces.TryGetValue(text, out value))
		{
			ClrInterface baseInterface = null;
			if (ptkExtends != 0 && ptkExtends != 16777216)
			{
				baseInterface = GetInterface(import, ptkExtends);
			}
			value = new DesktopHeapInterface(text, baseInterface);
			DesktopHeap.Interfaces[text] = value;
		}
		return value;
	}
}
