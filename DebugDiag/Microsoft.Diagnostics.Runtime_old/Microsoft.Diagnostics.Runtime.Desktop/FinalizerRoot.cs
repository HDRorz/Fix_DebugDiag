namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class FinalizerRoot : ClrRoot
{
	private ClrType _type;

	public override GCRootKind Kind => GCRootKind.Finalizer;

	public override string Name => "finalization handle";

	public override ClrType Type => _type;

	public FinalizerRoot(ulong obj, ClrType type)
	{
		Object = obj;
		_type = type;
	}
}
