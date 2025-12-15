using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopAppDomain : ClrAppDomain
{
	private readonly ulong _address;

	private readonly List<ClrModule> _modules = new List<ClrModule>();

	private readonly DesktopRuntimeBase _runtime;

	private static int s_internalId;

	public override ClrRuntime Runtime => _runtime;

	public override ulong Address => _address;

	public override int Id { get; }

	public override string Name { get; }

	public override IList<ClrModule> Modules => _modules;

	internal int InternalId { get; }

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
		Id = data.Id;
		Name = name;
		InternalId = s_internalId++;
		_runtime = runtime;
	}

	internal void AddModule(ClrModule module)
	{
		_modules.Add(module);
	}
}
