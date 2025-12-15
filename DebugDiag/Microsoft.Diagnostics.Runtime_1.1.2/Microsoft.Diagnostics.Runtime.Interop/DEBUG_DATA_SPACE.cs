namespace Microsoft.Diagnostics.Runtime.Interop;

public enum DEBUG_DATA_SPACE : uint
{
	VIRTUAL,
	PHYSICAL,
	CONTROL,
	IO,
	MSR,
	BUS_DATA,
	DEBUGGER_DATA
}
