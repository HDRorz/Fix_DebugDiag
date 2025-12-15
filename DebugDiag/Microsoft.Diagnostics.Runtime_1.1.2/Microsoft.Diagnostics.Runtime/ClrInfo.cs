using System;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class ClrInfo : IComparable
{
	private readonly DataTarget _dataTarget;

	public VersionInfo Version => ModuleInfo.Version;

	public ClrFlavor Flavor { get; }

	public DacInfo DacInfo { get; }

	public ModuleInfo ModuleInfo { get; }

	public string LocalMatchingDac { get; }

	internal ClrInfo(DataTarget dt, ClrFlavor flavor, ModuleInfo module, DacInfo dacInfo, string dacLocation)
	{
		_dataTarget = dt ?? throw new ArgumentNullException("dt");
		Flavor = flavor;
		DacInfo = dacInfo ?? throw new ArgumentNullException("dacInfo");
		ModuleInfo = module ?? throw new ArgumentNullException("module");
		LocalMatchingDac = dacLocation;
	}

	public override string ToString()
	{
		return Version.ToString();
	}

	public int CompareTo(object obj)
	{
		if (this == obj)
		{
			return 0;
		}
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is ClrInfo clrInfo))
		{
			throw new InvalidOperationException("Object is not a ClrInfo.");
		}
		if (Flavor != clrInfo.Flavor)
		{
			return clrInfo.Flavor.CompareTo(Flavor);
		}
		return Version.CompareTo(clrInfo.Version);
	}

	public ClrRuntime CreateRuntime()
	{
		return _dataTarget.CreateRuntime(this);
	}

	public ClrRuntime CreateRuntime(object clrDataProcess)
	{
		return _dataTarget.CreateRuntime(this, clrDataProcess);
	}

	public ClrRuntime CreateRuntime(string dacFilename, bool ignoreMismatch = false)
	{
		return _dataTarget.CreateRuntime(this, dacFilename, ignoreMismatch);
	}
}
