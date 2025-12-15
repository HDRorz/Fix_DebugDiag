namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrMethod
{
	public abstract string Name { get; }

	public abstract ulong NativeCode { get; }

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

	public abstract string GetFullSignature();

	public abstract SourceLocation GetSourceLocationForOffset(ulong nativeOffset);
}
