using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Native;

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate void THREADROOTCALLBACK(IntPtr token, ulong symbol, ulong address, ulong obj, int pinned, int interior);
