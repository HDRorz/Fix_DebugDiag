using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrMethod
{
	public abstract ulong MethodDesc { get; }

	public abstract string Name { get; }

	public abstract ulong NativeCode { get; }

	public abstract ILInfo IL { get; }

	public abstract HotColdRegions HotColdInfo { get; }

	public abstract MethodCompilationType CompilationType { get; }

	public abstract ILToNativeMap[] ILOffsetMap { get; }

	public abstract uint MetadataToken { get; }

	public abstract ClrType Type { get; }

	public abstract bool IsPublic { get; }

	public abstract bool IsPrivate { get; }

	public abstract bool IsInternal { get; }

	public abstract bool IsProtected { get; }

	public abstract bool IsStatic { get; }

	public abstract bool IsFinal { get; }

	public abstract bool IsPInvoke { get; }

	public abstract bool IsSpecialName { get; }

	public abstract bool IsRTSpecialName { get; }

	public abstract bool IsVirtual { get; }

	public abstract bool IsAbstract { get; }

	public abstract ulong GCInfo { get; }

	public virtual bool IsConstructor => Name == ".ctor";

	public virtual bool IsClassConstructor => Name == ".cctor";

	public abstract IEnumerable<ulong> EnumerateMethodDescs();

	public abstract string GetFullSignature();

	public abstract int GetILOffset(ulong addr);
}
