using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[Flags]
public enum SymbolReaderFlags
{
	None = 0,
	CacheOnly = 1,
	NoNGenPDB = 2
}
