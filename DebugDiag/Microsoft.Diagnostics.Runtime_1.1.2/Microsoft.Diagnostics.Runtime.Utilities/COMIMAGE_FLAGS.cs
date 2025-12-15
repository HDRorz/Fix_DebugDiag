using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[Flags]
public enum COMIMAGE_FLAGS
{
	ILONLY = 1,
	_32BITREQUIRED = 2,
	IL_LIBRARY = 4,
	STRONGNAMESIGNED = 8,
	NATIVE_ENTRYPOINT = 0x10,
	TRACKDEBUGDATA = 0x10000,
	_32BITPREFERRED = 0x20000
}
