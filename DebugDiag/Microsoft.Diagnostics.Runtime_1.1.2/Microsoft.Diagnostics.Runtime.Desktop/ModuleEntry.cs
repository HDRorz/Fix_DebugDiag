namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct ModuleEntry
{
	public readonly ClrModule Module;

	public readonly uint Token;

	public ModuleEntry(ClrModule module, uint token)
	{
		Module = module;
		Token = token;
	}
}
