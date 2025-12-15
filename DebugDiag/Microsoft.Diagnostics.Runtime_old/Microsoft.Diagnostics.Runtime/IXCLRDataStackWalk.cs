using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("E59D8D22-ADA7-49a2-89B5-A415AFCFC95F")]
internal interface IXCLRDataStackWalk
{
	[PreserveSig]
	int GetContext(uint contextFlags, uint contextBufSize, out uint contextSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] buffer);

	void SetContext_do_not_use();

	[PreserveSig]
	int Next();

	void GetStackSizeSkipped_do_not_use();

	void GetFrameType_do_not_use();

	[PreserveSig]
	int GetFrame([MarshalAs(UnmanagedType.IUnknown)] out object frame);

	[PreserveSig]
	int Request(uint reqCode, uint inBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] inBuffer, uint outBufferSize, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] outBuffer);

	void SetContext2_do_not_use();
}
