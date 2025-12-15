namespace Microsoft.Diagnostics.Runtime.Desktop;

internal interface IMethodDescData
{
	ulong MethodDesc { get; }

	ulong Module { get; }

	uint MDToken { get; }

	ulong NativeCodeAddr { get; }

	ulong MethodTable { get; }

	MethodCompilationType JITType { get; }
}
