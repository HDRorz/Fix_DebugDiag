namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class ThreadStaticVarRoot : ClrRoot
{
	public override ClrAppDomain AppDomain { get; }

	public override GCRootKind Kind => GCRootKind.ThreadStaticVar;

	public override string Name { get; }

	public override ClrType Type { get; }

	public ThreadStaticVarRoot(ulong addr, ulong obj, ClrType type, string typeName, string variableName, ClrAppDomain appDomain)
	{
		Address = addr;
		Object = obj;
		Name = $"thread static var {typeName}.{variableName}";
		AppDomain = appDomain;
		Type = type;
	}
}
