namespace Microsoft.Diagnostics.Runtime.Desktop;

internal interface IModuleData
{
	ulong ImageBase { get; }

	ulong PEFile { get; }

	ulong LookupTableHeap { get; }

	ulong ThunkHeap { get; }

	ulong ModuleId { get; }

	ulong ModuleIndex { get; }

	ulong Assembly { get; }

	bool IsReflection { get; }

	bool IsPEFile { get; }

	ulong MetdataStart { get; }

	ulong MetadataLength { get; }
}
