using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDiag.DbgEng;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("d98ada1f-29e9-4ef5-a6c0-e53349883212")]
public interface IDebugDataSpaces4 : IDebugDataSpaces3, IDebugDataSpaces2, IDebugDataSpaces
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
	new int VirtualToPhysical([In] ulong Virtual, out ulong Physical);

	[PreserveSig]
	new unsafe int GetVirtualTranslationPhysicalOffsets([In] ulong Virtual, [Out][MarshalAs(UnmanagedType.LPArray)] ulong[] Offsets, [In] uint OffsetsSize, [In] uint* Levels);

	[PreserveSig]
	new unsafe int ReadHandleData([In] ulong Handle, [In] DEBUG_HANDLE_DATA_TYPE DataType, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* DataSize);

	[PreserveSig]
	new unsafe int FillVirtual([In] ulong Start, [In] uint Size, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint* Filled);

	[PreserveSig]
	new unsafe int FillPhysical([In] ulong Start, [In] uint Size, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint* Filled);

	[PreserveSig]
	new int QueryVirtual([In] ulong Offset, [In] IntPtr Info_Aligned_MEMORY_BASIC_INFORMATION64);

	[PreserveSig]
	new int ReadImageNtHeaders([In] ulong ImageBase, out IMAGE_NT_HEADERS64 Headers);

	[PreserveSig]
	new unsafe int ReadTagged([In][MarshalAs(UnmanagedType.LPStruct)] Guid Tag, [In] uint Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* TotalSize);

	[PreserveSig]
	new int StartEnumTagged(out ulong Handle);

	[PreserveSig]
	new int GetNextTagged([In] ulong Handle, out Guid Tag, out uint Size);

	[PreserveSig]
	new int EndEnumTagged([In] ulong Handle);

	[PreserveSig]
	unsafe int GetOffsetInformation([In] DEBUG_DATA_SPACE Space, [In] DEBUG_OFFSINFO Which, [In] ulong Offset, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* InfoSize);

	[PreserveSig]
	int GetNextDifferentlyValidOffsetVirtual([In] ulong Offset, out ulong NextOffset);

	[PreserveSig]
	int GetValidRegionVirtual([In] ulong Base, [In] uint Size, out ulong ValidBase, out uint ValidSize);

	[PreserveSig]
	int SearchVirtual2([In] ulong Offset, [In] ulong Length, [In] DEBUG_VSEARCH Flags, [In] IntPtr Pattern, [In] uint PatternSize, [In] uint PatternGranularity, out ulong MatchOffset);

	[PreserveSig]
	unsafe int ReadMultiByteStringVirtual([In] ulong Offset, [In] uint MaxBytes, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] uint BufferSize, [In] uint* StringBytes);

	[PreserveSig]
	unsafe int ReadMultiByteStringVirtualWide([In] ulong Offset, [In] uint MaxBytes, [In] CODE_PAGE CodePage, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] uint BufferSize, [In] uint* StringBytes);

	[PreserveSig]
	unsafe int ReadUnicodeStringVirtual([In] ulong Offset, [In] uint MaxBytes, [In] CODE_PAGE CodePage, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder Buffer, [In] uint BufferSize, [In] uint* StringBytes);

	[PreserveSig]
	unsafe int ReadUnicodeStringVirtualWide([In] ulong Offset, [In] uint MaxBytes, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder Buffer, [In] uint BufferSize, [In] uint* StringBytes);

	[PreserveSig]
	unsafe int ReadPhysical2([In] ulong Offset, [In] DEBUG_PHYSICAL Flags, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesRead);

	[PreserveSig]
	unsafe int WritePhysical2([In] ulong Offset, [In] DEBUG_PHYSICAL Flags, [In] IntPtr Buffer, [In] uint BufferSize, [In] uint* BytesWritten);
}
