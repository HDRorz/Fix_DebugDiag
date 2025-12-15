namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct MethodTableTokenPair
{
	public ulong MethodTable { get; }

	public uint Token { get; }

	public MethodTableTokenPair(ulong methodTable, uint token)
	{
		MethodTable = methodTable;
		Token = token;
	}
}
