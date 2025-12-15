namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class StaticVarRoot : ClrRoot
{
	private string _name;

	private ClrAppDomain _domain;

	private ClrType _type;

	public override ClrAppDomain AppDomain => _domain;

	public override GCRootKind Kind => GCRootKind.StaticVar;

	public override string Name => _name;

	public override ClrType Type => _type;

	public StaticVarRoot(ulong addr, ulong obj, ClrType type, string typeName, string variableName, ClrAppDomain appDomain)
	{
		Address = addr;
		Object = obj;
		_name = $"static var {typeName}.{variableName}";
		_domain = appDomain;
		_type = type;
	}
}
