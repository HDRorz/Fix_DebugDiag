namespace Microsoft.Diagnostics.Runtime.Interop;

public enum DEBUG_VALUE_TYPE : uint
{
	INVALID,
	INT8,
	INT16,
	INT32,
	INT64,
	FLOAT32,
	FLOAT64,
	FLOAT80,
	FLOAT82,
	FLOAT128,
	VECTOR64,
	VECTOR128,
	TYPES
}
