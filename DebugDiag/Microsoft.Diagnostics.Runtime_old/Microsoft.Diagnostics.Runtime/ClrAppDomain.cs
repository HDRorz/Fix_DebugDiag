using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrAppDomain
{
	public abstract ulong Address { get; }

	public abstract int Id { get; }

	public abstract string Name { get; }

	public abstract IList<ClrModule> Modules { get; }

	public abstract string ConfigurationFile { get; }

	public abstract string ApplicationBase { get; }

	[Obsolete("Use ApplicationBase instead.")]
	public virtual string AppBase => ApplicationBase;
}
