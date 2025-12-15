namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class ThreadStaticVarRoot : ClrRoot
{
	private string _name;

	private ClrAppDomain _domain;

	private ClrType _type;

	public override ClrAppDomain AppDomain => _domain;

	public override GCRootKind Kind => GCRootKind.ThreadStaticVar;

	public override string Name => _name;

	public override ClrType Type => _type;

	public ThreadStaticVarRoot(ulong addr, ulong obj, ClrType type, string typeName, string variableName, ClrAppDomain appDomain)
	{
		Address = addr;
		Object = obj;
		_name = $"thread static var {typeName}.{variableName}";
		_domain = appDomain;
		_type = type;
	}
}
