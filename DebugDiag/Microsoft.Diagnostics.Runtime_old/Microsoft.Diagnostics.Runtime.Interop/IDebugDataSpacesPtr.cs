using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("88f7dfab-3ea7-4c3a-aefb-c4e8106173aa")]
public interface IDebugDataSpacesPtr
{
	[PreserveSig]
	int ReadVirtual([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesRead);

	[PreserveSig]
	int WriteVirtual([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesWritten);

	[PreserveSig]
	int SearchVirtual([In] ulong Offset, [In] ulong Length, [In] IntPtr pattern, [In] uint PatternSize, [In] uint PatternGranularity, out ulong MatchOffset);

	[PreserveSig]
	int ReadVirtualUncached([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesRead);

	[PreserveSig]
	int WriteVirtualUncached([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesWritten);

	[PreserveSig]
	int ReadPointersVirtual([In] uint Count, [In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ulong[] Ptrs);

	[PreserveSig]
	int WritePointersVirtual([In] uint Count, [In] ulong Offset, [In][MarshalAs(UnmanagedType.LPArray)] ulong[] Ptrs);

	[PreserveSig]
	int ReadPhysical([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesRead);

	[PreserveSig]
	int WritePhysical([In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesWritten);

	[PreserveSig]
	int ReadControl([In] uint Processor, [In] ulong Offset, [In] IntPtr buffer, [In] int BufferSize, out uint BytesRead);

	[PreserveSig]
	int WriteControl([In] uint Processor, [In] ulong Offset, [In] IntPtr buffer, [In] int BufferSize, out uint BytesWritten);

	[PreserveSig]
	int ReadIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesRead);

	[PreserveSig]
	int WriteIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesWritten);

	[PreserveSig]
	int ReadMsr([In] uint Msr, out ulong MsrValue);

	[PreserveSig]
	int WriteMsr([In] uint Msr, [In] ulong MsrValue);

	[PreserveSig]
	int ReadBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesRead);

	[PreserveSig]
	int WriteBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr buffer, [In] uint BufferSize, out uint BytesWritten);

	[PreserveSig]
	int CheckLowMemory();

	[PreserveSig]
	int ReadDebuggerData([In] uint Index, [In] IntPtr buffer, [In] uint BufferSize, out uint DataSize);

	[PreserveSig]
	int ReadProcessorSystemData([In] uint Processor, [In] DEBUG_DATA Index, [In] IntPtr buffer, [In] uint BufferSize, out uint DataSize);
}
