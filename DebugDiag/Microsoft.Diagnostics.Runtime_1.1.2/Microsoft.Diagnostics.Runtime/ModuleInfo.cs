using System;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class ModuleInfo
{
	[NonSerialized]
	protected readonly IDataReader _dataReader;

	private PdbInfo _pdb;

	protected bool _initialized;

	private bool _managed;

	private VersionInfo _version;

	private bool _versionInit;

	public virtual ulong ImageBase { get; set; }

	public virtual uint FileSize { get; set; }

	public virtual uint TimeStamp { get; set; }

	public virtual string FileName { get; set; }

	public byte[] BuildId { get; internal set; }

	public virtual bool IsManaged
	{
		get
		{
			InitData();
			return _managed;
		}
		internal set
		{
			_managed = value;
		}
	}

	public PdbInfo Pdb
	{
		get
		{
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
			InitVersion(out _version);
			_versionInit = true;
			return _version;
		}
		set
		{
			_version = value;
			_versionInit = true;
		}
	}

	public PEImage GetPEImage()
	{
		try
		{
			return new PEImage(new ReadVirtualStream(_dataReader, (long)ImageBase, FileSize), isVirtual: true);
		}
		catch
		{
			return null;
		}
	}

	public override string ToString()
	{
		return FileName;
	}

	private void InitData()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		if (_dataReader == null)
		{
			return;
		}
		try
		{
			PEImage pEImage = GetPEImage();
			if (pEImage != null && pEImage.IsValid)
			{
				_managed = pEImage.OptionalHeader.ComDescriptorDirectory.VirtualAddress != 0;
				_pdb = pEImage.DefaultPdb;
			}
		}
		catch
		{
		}
	}

	protected virtual void InitVersion(out VersionInfo version)
	{
		_dataReader.GetVersionInfo(ImageBase, out version);
	}

	public ModuleInfo()
	{
	}

	public ModuleInfo(IDataReader reader, VersionInfo? version = null)
	{
		_dataReader = reader;
		if (version.HasValue)
		{
			_versionInit = true;
			_version = version.Value;
		}
	}
}
