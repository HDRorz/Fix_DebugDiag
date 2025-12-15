using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopAppDomain : ClrAppDomain
{
	private ulong _address;

	private string _name;

	private int _id;

	private int _internalId;

	private List<ClrModule> _modules = new List<ClrModule>();

	private DesktopRuntimeBase _runtime;

	private static int s_internalId;

	public override ulong Address => _address;

	public override int Id => _id;

	public override string Name => _name;

	public override IList<ClrModule> Modules => _modules;

	internal int InternalId => _internalId;

	public override string ConfigurationFile => _runtime.GetConfigFile(_address);

	public override string ApplicationBase
	{
		get
		{
			string appBase = _runtime.GetAppBase(_address);
			if (string.IsNullOrEmpty(appBase))
			{
				return null;
			}
			Uri uri = new Uri(appBase);
			try
			{
				return uri.AbsolutePath.Replace('/', '\\');
			}
			catch (InvalidOperationException)
			{
				return appBase;
			}
		}
	}

	internal DesktopAppDomain(DesktopRuntimeBase runtime, IAppDomainData data, string name)
	{
		_address = data.Address;
		_id = data.Id;
		_name = name;
		_internalId = s_internalId++;
		_runtime = runtime;
	}

	internal void AddModule(ClrModule module)
	{
		_modules.Add(module);
	}
}
