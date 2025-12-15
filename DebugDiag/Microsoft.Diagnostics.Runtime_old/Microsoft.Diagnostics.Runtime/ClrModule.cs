using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrModule
{
	public abstract bool IsPdbLoaded { get; }

	public abstract object PdbInterface { get; }

	public abstract string AssemblyName { get; }

	public abstract ulong AssemblyId { get; }

	public abstract string Name { get; }

	public abstract bool IsDynamic { get; }

	public abstract bool IsFile { get; }

	public abstract string FileName { get; }

	public abstract ulong ImageBase { get; }

	public abstract ulong Size { get; }

	public abstract ulong MetadataAddress { get; }

	public abstract ulong MetadataLength { get; }

	public abstract object MetadataImport { get; }

	public abstract DebuggableAttribute.DebuggingModes DebuggingMode { get; }

	public abstract bool IsMatchingPdb(string pdbPath);

	public abstract void LoadPdb(string path);

	[Obsolete("Use TryDownloadPdb() instead.")]
	public virtual string TryDownloadPdb(ISymbolNotification notification)
	{
		return TryDownloadPdb();
	}

	public abstract string TryDownloadPdb();

	public abstract SourceLocation GetSourceInformation(uint mdMethodToken, int ilOffset);

	public abstract SourceLocation GetSourceInformation(ClrMethod method, int ilOffset);

	public abstract IEnumerable<ClrType> EnumerateTypes();

	public abstract ClrType GetTypeByName(string name);

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Name))
		{
			if (!string.IsNullOrEmpty(AssemblyName))
			{
				return AssemblyName;
			}
			if (IsDynamic)
			{
				return "dynamic";
			}
		}
		return Name;
	}
}
