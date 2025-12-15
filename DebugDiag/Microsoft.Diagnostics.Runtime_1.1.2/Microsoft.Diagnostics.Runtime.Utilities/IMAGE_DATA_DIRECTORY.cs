using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[Obsolete]
public struct IMAGE_DATA_DIRECTORY
{
	public int VirtualAddress;

	public int Size;
}
