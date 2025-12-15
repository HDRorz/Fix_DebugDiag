using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopModule : DesktopBaseModule
{
	private static readonly PdbInfo s_failurePdb = new PdbInfo();

	private readonly bool _isPE;

	private readonly string _name;

	private MetaDataImport _metadata;

	private readonly Dictionary<ClrAppDomain, ulong> _mapping = new Dictionary<ClrAppDomain, ulong>();

	private readonly ulong _address;

	private readonly Lazy<ulong> _size;

	private DebuggableAttribute.DebuggingModes? _debugMode;

	private bool _typesLoaded;

	private ClrAppDomain[] _appDomainList;

	private PdbInfo _pdb;

	public override ulong Address => _address;

	public override PdbInfo Pdb
	{
		get
		{
			if (_pdb == null)
			{
				try
				{
					using ReadVirtualStream stream = new ReadVirtualStream(_runtime.DataReader, (long)ImageBase, (long)((Size != 0) ? Size : 4096));
					PEImage pEImage = new PEImage(stream, isVirtual: true);
					if (pEImage.IsValid)
					{
						_pdb = pEImage.DefaultPdb ?? s_failurePdb;
					}
				}
				catch
				{
				}
			}
			if (_pdb == s_failurePdb)
			{
				return null;
			}
			return _pdb;
		}
	}

	public override string AssemblyName { get; }

	public override string Name => _name;

	public override bool IsDynamic { get; }

	public override bool IsFile => _isPE;

	public override string FileName
	{
		get
		{
			if (!_isPE)
			{
				return null;
			}
			return _name;
		}
	}

	internal ulong ModuleIndex { get; }

	public override IList<ClrAppDomain> AppDomains
	{
		get
		{
			if (_appDomainList == null)
			{
				_appDomainList = new ClrAppDomain[_mapping.Keys.Count];
				_appDomainList = _mapping.Keys.ToArray();
				Array.Sort(_appDomainList, (ClrAppDomain d, ClrAppDomain d2) => d.Id.CompareTo(d2.Id));
			}
			return _appDomainList;
		}
	}

	public override ulong ImageBase { get; }

	public override ulong Size => _size.Value;

	public override ulong MetadataAddress { get; }

	public override ulong MetadataLength { get; }

	public override object MetadataImport => GetMetadataImport();

	public override DebuggableAttribute.DebuggingModes DebuggingMode
	{
		get
		{
			if (!_debugMode.HasValue)
			{
				InitDebugAttributes();
			}
			return _debugMode.Value;
		}
	}

	public override ulong AssemblyId { get; }

	public DesktopModule(DesktopRuntimeBase runtime, ulong address, IModuleData data, string name, string assemblyName)
		: base(runtime)
	{
		_address = address;
		base.Revision = runtime.Revision;
		ImageBase = data.ImageBase;
		AssemblyName = assemblyName;
		_isPE = data.IsPEFile;
		IsDynamic = data.IsReflection || string.IsNullOrEmpty(name);
		_name = name;
		base.ModuleId = data.ModuleId;
		ModuleIndex = data.ModuleIndex;
		MetadataAddress = data.MetdataStart;
		MetadataLength = data.MetadataLength;
		AssemblyId = data.Assembly;
		_size = new Lazy<ulong>(() => runtime.GetModuleSize(data.ImageBase));
	}

	internal ulong GetMTForDomain(ClrAppDomain domain, DesktopHeapType type)
	{
		DesktopGCHeap desktopGCHeap = null;
		IList<MethodTableTokenPair> methodTableList = _runtime.GetMethodTableList(_mapping[domain]);
		bool flag = type.MetadataToken != 0 && type.MetadataToken != uint.MaxValue;
		uint num = 0xFFFFFF & type.MetadataToken;
		foreach (MethodTableTokenPair item in methodTableList)
		{
			if (flag)
			{
				if (item.Token == num)
				{
					return item.MethodTable;
				}
				continue;
			}
			if (desktopGCHeap == null)
			{
				desktopGCHeap = (DesktopGCHeap)_runtime.Heap;
			}
			if (desktopGCHeap.GetTypeByMethodTable(item.MethodTable, 0uL) == type)
			{
				return item.MethodTable;
			}
		}
		return 0uL;
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		DesktopGCHeap heap = (DesktopGCHeap)_runtime.Heap;
		IList<MethodTableTokenPair> methodTableList = _runtime.GetMethodTableList(_address);
		if (_typesLoaded)
		{
			foreach (ClrType item in heap.EnumerateTypes())
			{
				if (item.Module == this)
				{
					yield return item;
				}
			}
			yield break;
		}
		if (methodTableList != null)
		{
			foreach (MethodTableTokenPair item2 in methodTableList)
			{
				ulong methodTable = item2.MethodTable;
				if (methodTable != _runtime.ArrayMethodTable)
				{
					ClrType typeByMethodTable = heap.GetTypeByMethodTable(methodTable, 0uL, 0uL);
					if (typeByMethodTable != null)
					{
						yield return typeByMethodTable;
					}
				}
			}
		}
		_typesLoaded = true;
	}

	internal void AddMapping(ClrAppDomain domain, ulong domainModule)
	{
		_ = (DesktopAppDomain)domain;
		_mapping[domain] = domainModule;
	}

	internal override ulong GetDomainModule(ClrAppDomain domain)
	{
		_ = _runtime.AppDomains;
		if (domain == null)
		{
			using (Dictionary<ClrAppDomain, ulong>.ValueCollection.Enumerator enumerator = _mapping.Values.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			return 0uL;
		}
		if (_mapping.TryGetValue(domain, out var value))
		{
			return value;
		}
		return 0uL;
	}

	internal override MetaDataImport GetMetadataImport()
	{
		RevisionValidator.Validate(base.Revision, _runtime.Revision);
		if (_metadata != null)
		{
			return _metadata;
		}
		_metadata = _runtime.GetMetadataImport(_address);
		return _metadata;
	}

	private unsafe void InitDebugAttributes()
	{
		MetaDataImport metadataImport = GetMetadataImport();
		if (metadataImport == null)
		{
			_debugMode = DebuggableAttribute.DebuggingModes.None;
			return;
		}
		try
		{
			if (metadataImport.GetCustomAttributeByName(536870913, "System.Diagnostics.DebuggableAttribute", out var data, out var cbData) && cbData >= 4)
			{
				byte* ptr = (byte*)data.ToPointer();
				ushort num = ptr[2];
				ushort num2 = ptr[3];
				_debugMode = (DebuggableAttribute.DebuggingModes)((num2 << 8) | num);
			}
			else
			{
				_debugMode = DebuggableAttribute.DebuggingModes.None;
			}
		}
		catch (SEHException)
		{
			_debugMode = DebuggableAttribute.DebuggingModes.None;
		}
	}

	public override ClrType GetTypeByName(string name)
	{
		foreach (ClrType item in EnumerateTypes())
		{
			if (item.Name == name)
			{
				return item;
			}
		}
		return null;
	}
}
