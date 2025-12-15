namespace Microsoft.Diagnostics.Runtime;

public enum ClrElementType
{
	Unknown = 0,
	Boolean = 2,
	Char = 3,
	Int8 = 4,
	UInt8 = 5,
	Int16 = 6,
	UInt16 = 7,
	Int32 = 8,
	UInt32 = 9,
	Int64 = 10,
	UInt64 = 11,
	Float = 12,
	Double = 13,
	String = 14,
	Pointer = 15,
	Struct = 17,
	Class = 18,
	Array = 20,
	NativeInt = 24,
	NativeUInt = 25,
	FunctionPointer = 27,
	Object = 28,
	SZArray = 29
}
