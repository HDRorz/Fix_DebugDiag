namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class FinalizerRoot : ClrRoot
{
	public override GCRootKind Kind => GCRootKind.Finalizer;

	public override string Name => "finalization handle";

	public override ClrType Type { get; }

	public FinalizerRoot(ulong obj, ClrType type)
	{
		Object = obj;
		Type = type;
	}
}
