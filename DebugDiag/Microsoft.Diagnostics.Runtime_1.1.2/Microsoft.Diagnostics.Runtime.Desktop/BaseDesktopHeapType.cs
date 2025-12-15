using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class BaseDesktopHeapType : ClrType
{
	protected ClrElementType _elementType;

	protected uint _token;

	private IList<ClrInterface> _interfaces;

	private readonly Lazy<GCDesc> _gcDesc;

	protected ulong _constructedMT;

	protected internal override GCDesc GCDesc => _gcDesc.Value;

	public bool Shared { get; internal set; }

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

	public BaseDesktopHeapType(ulong mt, DesktopGCHeap heap, DesktopBaseModule module, uint token)
	{
		_constructedMT = mt;
		DesktopHeap = heap;
		DesktopModule = module;
		_token = token;
		_gcDesc = new Lazy<GCDesc>(FillGCDesc);
	}

	private GCDesc FillGCDesc()
	{
		DesktopRuntimeBase desktopRuntime = DesktopHeap.DesktopRuntime;
		if (!desktopRuntime.ReadDword(_constructedMT - (ulong)IntPtr.Size, out int value))
		{
			return null;
		}
		if (value < 0)
		{
			value = -value;
		}
		int num = 1 + value * 2;
		byte[] array = new byte[num * IntPtr.Size];
		if (!desktopRuntime.ReadMemory(_constructedMT - (ulong)(num * IntPtr.Size), array, array.Length, out var bytesRead) || bytesRead != array.Length)
		{
			return null;
		}
		return new GCDesc(array);
	}

	internal abstract ulong GetModuleAddress(ClrAppDomain domain);

	internal override ClrMethod GetMethod(uint token)
	{
		return null;
	}

	public List<ClrInterface> InitInterfaces()
	{
		if (DesktopModule == null)
		{
			_interfaces = DesktopHeap.EmptyInterfaceList;
			return null;
		}
		List<ClrInterface> list = ((BaseType is BaseDesktopHeapType baseDesktopHeapType) ? new List<ClrInterface>(baseDesktopHeapType.Interfaces) : null);
		MetaDataImport metadataImport = DesktopModule.GetMetadataImport();
		if (metadataImport == null)
		{
			_interfaces = DesktopHeap.EmptyInterfaceList;
			return null;
		}
		foreach (int item in metadataImport.EnumerateInterfaceImpls((int)_token))
		{
			if (metadataImport.GetInterfaceImplProps(item, out var _, out var mdInterface))
			{
				if (list == null)
				{
					list = new List<ClrInterface>();
				}
				ClrInterface @interface = GetInterface(metadataImport, mdInterface);
				if (@interface != null && !list.Contains(@interface))
				{
					list.Add(@interface);
				}
			}
		}
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

	private ClrInterface GetInterface(MetaDataImport import, int mdIFace)
	{
		ClrInterface value = null;
		if (!import.GetTypeDefProperties(mdIFace, out var name, out var _, out var mdParent))
		{
			name = import.GetTypeRefName(mdIFace);
		}
		if (name != null && !DesktopHeap.Interfaces.TryGetValue(name, out value))
		{
			ClrInterface baseInterface = null;
			if (mdParent != 0 && mdParent != 16777216)
			{
				baseInterface = GetInterface(import, mdParent);
			}
			value = new DesktopHeapInterface(name, baseInterface);
			DesktopHeap.Interfaces[name] = value;
		}
		return value;
	}
}
