namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_SPECIFIC_FILTER_PARAMETERS
{
	public DEBUG_FILTER_EXEC_OPTION ExecutionOption;

	public DEBUG_FILTER_CONTINUE_OPTION ContinueOption;

	public uint TextSize;

	public uint CommandSize;

	public uint ArgumentSize;
}
