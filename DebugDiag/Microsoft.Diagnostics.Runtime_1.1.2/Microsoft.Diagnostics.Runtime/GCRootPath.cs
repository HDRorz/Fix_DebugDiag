namespace Microsoft.Diagnostics.Runtime;

public struct GCRootPath
{
	public ClrRoot Root { get; set; }

	public ClrObject[] Path { get; set; }
}
