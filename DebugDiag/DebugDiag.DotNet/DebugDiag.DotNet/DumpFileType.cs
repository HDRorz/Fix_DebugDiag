using System;

namespace DebugDiag.DotNet;

/// <summary>
/// This enumeration has a Flags attribute.
/// </summary>
[Flags]
public enum DumpFileType
{
	/// <summary>
	/// Unrecognized dump file type
	/// </summary>
	None = 0,
	/// <summary>
	/// Target process was 32 bits and no CLR was found on the process
	/// </summary>
	_32bitWithoutClr = 1,
	/// <summary>
	/// Target process was 32 bits and CLR was found on the process
	/// </summary>
	_32bitWithClr = 2,
	/// <summary>
	/// Target process was 32 bits and CLR was found on the process
	/// </summary>
	_32bitAll = 3,
	/// <summary>
	/// Target process was 64 bits and no CLR was found on the process
	/// </summary>
	_64bitWithoutClr = 4,
	/// <summary>
	/// Target process was 64 bits and CLR was found on the process
	/// </summary>
	_64bitWithClr = 8,
	/// <summary>
	/// Target process was 32 bits and CLR was found on the process
	/// </summary>
	_64bitAll = 0xC
}
