using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public sealed class ClrStackWalk : CallableCOMWrapper
{
	private delegate int GetContextDelegate(IntPtr self, uint contextFlags, uint contextBufSize, out uint contextSize, byte[] buffer);

	private delegate int NextDelegate(IntPtr self);

	private delegate int RequestDelegate(IntPtr self, uint reqCode, uint inBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] inBuffer, uint outBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outBuffer);

	private static Guid IID_IXCLRDataStackWalk = new Guid("E59D8D22-ADA7-49a2-89B5-A415AFCFC95F");

	private readonly byte[] _ulongBuffer = new byte[8];

	private RequestDelegate _request;

	private NextDelegate _next;

	private GetContextDelegate _getContext;

	private unsafe IXCLRDataStackWalkVTable* VTable => (IXCLRDataStackWalkVTable*)base._vtable;

	public ClrStackWalk(DacLibrary library, IntPtr pUnk)
		: base(library.OwningLibrary, ref IID_IXCLRDataStackWalk, pUnk)
	{
	}

	public unsafe ulong GetFrameVtable()
	{
		CallableCOMWrapper.InitDelegate(ref _request, VTable->Request);
		if (_request(base.Self, 4026531840u, 0u, null, (uint)_ulongBuffer.Length, _ulongBuffer) == 0)
		{
			return BitConverter.ToUInt64(_ulongBuffer, 0);
		}
		return 0uL;
	}

	public unsafe bool Next()
	{
		CallableCOMWrapper.InitDelegate(ref _next, VTable->Next);
		return _next(base.Self) == 0;
	}

	public unsafe bool GetContext(uint contextFlags, uint contextBufSize, out uint contextSize, byte[] buffer)
	{
		CallableCOMWrapper.InitDelegate(ref _getContext, VTable->GetContext);
		return _getContext(base.Self, contextFlags, contextBufSize, out contextSize, buffer) == 0;
	}
}
