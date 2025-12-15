using System;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class ModuleInfo
{
	[NonSerialized]
	private IDataReader _dataReader;

	private PdbInfo _pdb;

	private bool? _managed;

	private VersionInfo _version;

	private bool _versionInit;

	public virtual ulong ImageBase { get; set; }

	public virtual uint FileSize { get; set; }

	public virtual uint TimeStamp { get; set; }

	public virtual string FileName { get; set; }

	public bool IsRuntime { get; internal set; }

	public virtual bool IsManaged
	{
		get
		{
			InitData();
			return _managed ?? false;
		}
	}

	public PdbInfo Pdb
	{
		get
		{
			if (_pdb != null || _dataReader == null)
			{
				return _pdb;
			}
			InitData();
			return _pdb;
		}
		set
		{
			_pdb = value;
		}
	}

	public VersionInfo Version
	{
		get
		{
			if (_versionInit || _dataReader == null)
			{
				return _version;
			}
			_dataReader.GetVersionInfo(ImageBase, out _version);
			_versionInit = true;
			return _version;
		}
		set
		{
			_version = value;
			_versionInit = true;
		}
	}

	public PEFile GetPEFile()
	{
		return PEFile.TryLoad(new ReadVirtualStream(_dataReader, (long)ImageBase, FileSize), virt: true);
	}

	public override string ToString()
	{
		return FileName;
	}

	private void InitData()
	{
		if (_dataReader == null || (_pdb != null && _managed.HasValue))
		{
			return;
		}
		PdbInfo pdbInfo = null;
		PEFile pEFile = null;
		try
		{
			pEFile = PEFile.TryLoad(new ReadVirtualStream(_dataReader, (long)ImageBase, FileSize), virt: true);
			if (pEFile != null)
			{
				_managed = pEFile.Header.ComDescriptorDirectory.VirtualAddress != 0;
				if (pEFile.GetPdbSignature(out var pdbName, out var pdbGuid, out var pdbAge))
				{
					pdbInfo = new PdbInfo();
					pdbInfo.FileName = pdbName;
					pdbInfo.Guid = pdbGuid;
					pdbInfo.Revision = pdbAge;
					_pdb = pdbInfo;
				}
			}
		}
		catch
		{
		}
		finally
		{
			pEFile?.Dispose();
		}
	}

	public ModuleInfo()
	{
	}

	public ModuleInfo(IDataReader reader)
	{
		_dataReader = reader;
	}
}
