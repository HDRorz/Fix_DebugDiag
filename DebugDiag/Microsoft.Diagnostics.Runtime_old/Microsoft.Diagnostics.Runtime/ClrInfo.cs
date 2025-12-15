using System;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class ClrInfo : IComparable
{
	private string _dacLocation;

	private DataTarget _dataTarget;

	public VersionInfo Version => ModuleInfo.Version;

	public ClrFlavor Flavor { get; private set; }

	public DacInfo DacInfo { get; private set; }

	public ModuleInfo ModuleInfo { get; private set; }

	public string TryGetDacLocation()
	{
		return _dacLocation;
	}

	[Obsolete]
	public string TryDownloadDac(ISymbolNotification notification)
	{
		if (_dacLocation != null)
		{
			return _dacLocation;
		}
		return _dataTarget.SymbolLocator.DownloadBinary(DacInfo, checkProperties: false);
	}

	public string TryDownloadDac()
	{
		if (_dacLocation != null)
		{
			return _dacLocation;
		}
		SymbolLocator symbolLocator = _dataTarget.SymbolLocator;
		ModuleInfo dacInfo = DacInfo;
		return symbolLocator.DownloadBinary(dacInfo.FileName, dacInfo.TimeStamp, dacInfo.FileSize, checkProperties: false);
	}

	public override string ToString()
	{
		return Version.ToString();
	}

	internal ClrInfo(DataTarget dt, ClrFlavor flavor, ModuleInfo module, DacInfo dacInfo, string dacLocation)
	{
		Flavor = flavor;
		DacInfo = dacInfo;
		ModuleInfo = module;
		module.IsRuntime = true;
		_dataTarget = dt;
		_dacLocation = dacLocation;
	}

	internal ClrInfo()
	{
	}

	public int CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is ClrInfo))
		{
			throw new InvalidOperationException("Object not ClrInfo.");
		}
		ClrFlavor flavor = ((ClrInfo)obj).Flavor;
		if (flavor != Flavor)
		{
			return flavor.CompareTo(Flavor);
		}
		VersionInfo version = ((ClrInfo)obj).Version;
		if (Version.Major != version.Major)
		{
			return Version.Major.CompareTo(version.Major);
		}
		if (Version.Minor != version.Minor)
		{
			return Version.Minor.CompareTo(version.Minor);
		}
		if (Version.Revision != version.Revision)
		{
			return Version.Revision.CompareTo(version.Revision);
		}
		return Version.Patch.CompareTo(version.Patch);
	}
}
