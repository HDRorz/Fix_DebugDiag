namespace Microsoft.Diagnostics.Runtime.ICorDebug;

public enum CorDebugMappingResult
{
	MAPPING_APPROXIMATE = 32,
	MAPPING_EPILOG = 2,
	MAPPING_EXACT = 16,
	MAPPING_NO_INFO = 4,
	MAPPING_PROLOG = 1,
	MAPPING_UNMAPPED_ADDRESS = 8
}
