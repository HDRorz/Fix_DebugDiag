using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dia2Lib;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopModule : DesktopBaseModule
{
	private bool _reflection;

	private bool _isPE;

	private string _name;

	private string _assemblyName;

	private DesktopRuntimeBase _runtime;

	private IMetadata _metadata;

	private Dictionary<ClrAppDomain, ulong> _mapping = new Dictionary<ClrAppDomain, ulong>();

	private ulong _imageBase;

	private ulong _size;

	private ulong _metadataStart;

	private ulong _metadataLength;

	private DebuggableAttribute.DebuggingModes? _debugMode;

	private ulong _address;

	private ulong _assemblyAddress;

	private bool _typesLoaded;

	private SymbolModule _symbols;

	private PEFile _peFile;

	public override bool IsPdbLoaded => _symbols != null;

	public override object PdbInterface
	{
		get
		{
			if (_symbols == null)
			{
				return null;
			}
			return _symbols.Session;
		}
	}

	public override string AssemblyName => _assemblyName;

	public override string Name => _name;

	public override bool IsDynamic => _reflection;

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

	internal ulong ModuleIndex { get; private set; }

	public override ulong ImageBase => _imageBase;

	public override ulong Size => _size;

	public override ulong MetadataAddress => _metadataStart;

	public override ulong MetadataLength => _metadataLength;

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

	public override ulong AssemblyId => _assemblyAddress;

	public override SourceLocation GetSourceInformation(ClrMethod method, int ilOffset)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (method.Type != null && method.Type.Module != this)
		{
			throw new InvalidOperationException("Method not in this module.");
		}
		return GetSourceInformation(method.MetadataToken, ilOffset);
	}

	public override SourceLocation GetSourceInformation(uint token, int ilOffset)
	{
		if (_symbols == null)
		{
			return null;
		}
		return _symbols.SourceLocationForManagedCode(token, ilOffset);
	}

	public override bool IsMatchingPdb(string pdbPath)
	{
		if (_peFile == null)
		{
			_peFile = new PEFile(new ReadVirtualStream(_runtime.DataReader, (long)_imageBase, (long)_size), virt: true);
		}
		if (!_peFile.GetPdbSignature(out var _, out var pdbGuid, out var _))
		{
			throw new ClrDiagnosticsException("Failed to get PDB signature from module.", ClrDiagnosticsException.HR.DataRequestError);
		}
		IDiaDataSource diaSourceObject = DiaLoader.GetDiaSourceObject();
		diaSourceObject.loadDataFromPdb(pdbPath);
		IDiaSession val = default(IDiaSession);
		diaSourceObject.openSession(ref val);
		return pdbGuid == val.globalScope.guid;
	}

	public override void LoadPdb(string path)
	{
		_symbols = _runtime.DataTarget.SymbolLocator.LoadPdb(path);
	}

	public override string TryDownloadPdb()
	{
		_ = _runtime.DataTarget;
		if (!_peFile.GetPdbSignature(out var pdbName, out var pdbGuid, out var pdbAge))
		{
			throw new ClrDiagnosticsException("Failed to get PDB signature from module.", ClrDiagnosticsException.HR.DataRequestError);
		}
		return _runtime.DataTarget.SymbolLocator.FindPdb(pdbName, pdbGuid, pdbAge);
	}

	public DesktopModule(DesktopRuntimeBase runtime, ulong address, IModuleData data, string name, string assemblyName, ulong size)
	{
		base.Revision = runtime.Revision;
		_imageBase = data.ImageBase;
		_runtime = runtime;
		_assemblyName = assemblyName;
		_isPE = data.IsPEFile;
		_reflection = data.IsReflection || string.IsNullOrEmpty(name) || !name.Contains("\\");
		_name = name;
		base.ModuleId = data.ModuleId;
		ModuleIndex = data.ModuleIndex;
		_metadataStart = data.MetdataStart;
		_metadataLength = data.MetadataLength;
		_assemblyAddress = data.Assembly;
		_address = address;
		_size = size;
		if (!runtime.DataReader.IsMinidump)
		{
			_metadata = data.LegacyMetaDataImport as IMetadata;
		}
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		DesktopGCHeap heap = (DesktopGCHeap)_runtime.GetHeap();
		IList<ulong> mtList = _runtime.GetMethodTableList(_address);
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
		if (mtList != null)
		{
			foreach (ulong item2 in mtList)
			{
				if (item2 != _runtime.ArrayMethodTable)
				{
					ClrType gCHeapType = heap.GetGCHeapType(item2, 0uL, 0uL);
					if (gCHeapType != null)
					{
						yield return gCHeapType;
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
		_runtime.InitDomains();
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

	internal override IMetadata GetMetadataImport()
	{
		if (base.Revision != _runtime.Revision)
		{
			ClrDiagnosticsException.ThrowRevisionError(base.Revision, _runtime.Revision);
		}
		if (_metadata != null)
		{
			return _metadata;
		}
		ulong domainModule = GetDomainModule(null);
		if (domainModule == 0L)
		{
			return null;
		}
		_metadata = _runtime.GetMetadataImport(domainModule);
		return _metadata;
	}

	internal void SetImageSize(ulong size)
	{
		_size = size;
	}

	private unsafe void InitDebugAttributes()
	{
		IMetadata metadataImport = GetMetadataImport();
		if (metadataImport == null)
		{
			_debugMode = DebuggableAttribute.DebuggingModes.None;
			return;
		}
		try
		{
			if (metadataImport.GetCustomAttributeByName(536870913, "System.Diagnostics.DebuggableAttribute", out var ppData, out var pcbData) != 0 || pcbData <= 4)
			{
				_debugMode = DebuggableAttribute.DebuggingModes.None;
				return;
			}
			byte* ptr = (byte*)ppData.ToPointer();
			ushort num = ptr[2];
			ushort num2 = ptr[3];
			_debugMode = (DebuggableAttribute.DebuggingModes)((num2 << 8) | num);
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
