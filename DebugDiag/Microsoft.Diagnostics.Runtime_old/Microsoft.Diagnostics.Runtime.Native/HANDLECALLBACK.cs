using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Native;

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate void HANDLECALLBACK(IntPtr ptr, ulong HandleAddr, ulong DependentTarget, int HandleType, uint ulRefCount, int strong);
