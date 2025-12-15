using System;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("7a5e852f-96e9-468f-ac1b-0b3addc4a049")]
public interface IDebugDataSpaces2 : IDebugDataSpaces
{
	[PreserveSig]
	new unsafe int ReadVirtual([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteVirtual([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int SearchVirtual([In] ulong Offset, [In] ulong Length, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint PatternGranularity, out ulong MatchOffset);

	[PreserveSig]
	new unsafe int ReadVirtualUncached([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteVirtualUncached([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int ReadPointersVirtual([In] uint Count, [In] ulong Offset, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Ptrs);

	[PreserveSig]
	new int WritePointersVirtual([In] uint Count, [In] ulong Offset, [In][MarshalAs(UnmanagedType.LPArray)] ulong[] Ptrs);

	[PreserveSig]
	new unsafe int ReadPhysical([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WritePhysical([In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new unsafe int ReadControl([In] uint Processor, [In] ulong Offset, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteControl([In] uint Processor, [In] ulong Offset, [In] IntPtr Buffer, [In] int BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new unsafe int ReadIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteIo([In] INTERFACE_TYPE InterfaceType, [In] uint BusNumber, [In] uint AddressSpace, [In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int ReadMsr([In] uint Msr, out ulong MsrValue);

	[PreserveSig]
	new int WriteMsr([In] uint Msr, [In] ulong MsrValue);

	[PreserveSig]
	new unsafe int ReadBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	new unsafe int WriteBusData([In] BUS_DATA_TYPE BusDataType, [In] uint BusNumber, [In] uint SlotNumber, [In] uint Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);

	[PreserveSig]
	new int CheckLowMemory();

	[PreserveSig]
	new unsafe int ReadDebuggerData([In] RDD_DEBUG_DATA Index, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);

	[PreserveSig]
	new unsafe int ReadProcessorSystemData([In] uint Processor, [In] DEBUG_DATA Index, out IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);

	[PreserveSig]
	int VirtualToPhysical([In] ulong Virtual, out ulong Physical);

	[PreserveSig]
	unsafe int GetVirtualTranslationPhysicalOffsets([In] ulong Virtual, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Offsets, [In] uint OffsetsSize, [In] uint* Levels);

	[PreserveSig]
	unsafe int ReadHandleData([In] ulong Handle, [In] DEBUG_HANDLE_DATA_TYPE DataType, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);

	[PreserveSig]
	unsafe int FillVirtual([In] ulong Start, [In] uint Size, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint* Filled);

	[PreserveSig]
	unsafe int FillPhysical([In] ulong Start, [In] uint Size, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint* Filled);

	[PreserveSig]
	int QueryVirtual([In] ulong Offset, [In] IntPtr Info_Aligned_MEMORY_BASIC_INFORMATION64);
}
