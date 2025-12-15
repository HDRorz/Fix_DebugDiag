namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class StaticVarRoot : ClrRoot
{
	public override ClrAppDomain AppDomain { get; }

	public override GCRootKind Kind => GCRootKind.StaticVar;

	public override string Name { get; }

	public override ClrType Type { get; }

	public StaticVarRoot(ulong addr, ulong obj, ClrType type, string typeName, string variableName, ClrAppDomain appDomain)
	{
		Address = addr;
		Object = obj;
		Name = "static var " + typeName + "." + variableName;
		AppDomain = appDomain;
		Type = type;
	}
}
