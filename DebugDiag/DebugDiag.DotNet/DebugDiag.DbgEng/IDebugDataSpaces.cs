using System;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("88f7dfab-3ea7-4c3a-aefb-c4e8106173aa")]
public interface IDebugDataSpaces
{
	[PreserveSig]
	unsafe int ReadVirtual([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteVirtual([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int SearchVirtual([In] ulong Offset, [In] ulong Length, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint PatternGranularity, out ulong MatchOffset);

	[PreserveSig]
	unsafe int ReadVirtualUncached([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteVirtualUncached([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int ReadPointersVirtual([In] uint Count, [In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Ptrs);

	[PreserveSig]
	int WritePointersVirtual([In] uint Count, [In] ulong Offset, [In][MarshalAs(UnmanagedType.LPArray)] ulong[] Ptrs);

	[PreserveSig]
	unsafe int ReadPhysical([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WritePhysical([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	unsafe int ReadControl([In] uint Processor, [In] ulong Offset, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteControl([In] uint Processor, [In] ulong Offset, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	unsafe int ReadIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int ReadMsr([In] uint Msr, out ulong MsrValue);

	[PreserveSig]
	int WriteMsr([In] uint Msr, [In] ulong MsrValue);

	[PreserveSig]
	unsafe int ReadBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WriteBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	int CheckLowMemory();

	[PreserveSig]
	unsafe int ReadDebuggerData([In] RDD_DEBUG_DATA Index, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);

	[PreserveSig]
	unsafe int ReadProcessorSystemData([In] uint Processor, [In] DEBUG_DATA Index, out IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);
}
