using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Native;

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate void STATICROOTCALLBACK(IntPtr token, ulong addr, ulong obj, int pinned, int interior);
