using System;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public sealed class PEHeader
{
	private unsafe readonly IMAGE_DOS_HEADER* _dosHeader;

	private unsafe readonly IMAGE_NT_HEADERS* _ntHeader;

	private unsafe readonly IMAGE_SECTION_HEADER* _sections;

	private readonly bool _virt;

	public unsafe int PEHeaderSize => VirtualAddressToRva(_sections) + sizeof(IMAGE_SECTION_HEADER) * _ntHeader->FileHeader.NumberOfSections;

	public unsafe bool IsPE64 => OptionalHeader32->Magic == 523;

	public bool IsManaged => ComDescriptorDirectory.VirtualAddress != 0;

	public unsafe uint Signature => _ntHeader->Signature;

	public unsafe MachineType Machine => (MachineType)_ntHeader->FileHeader.Machine;

	public unsafe ushort NumberOfSections => _ntHeader->FileHeader.NumberOfSections;

	public unsafe int TimeDateStampSec => (int)_ntHeader->FileHeader.TimeDateStamp;

	public DateTime TimeDateStamp => TimeDateStampToDate(TimeDateStampSec);

	public unsafe ulong PointerToSymbolTable => _ntHeader->FileHeader.PointerToSymbolTable;

	public unsafe ulong NumberOfSymbols => _ntHeader->FileHeader.NumberOfSymbols;

	public unsafe ushort SizeOfOptionalHeader => _ntHeader->FileHeader.SizeOfOptionalHeader;

	public unsafe ushort Characteristics => _ntHeader->FileHeader.Characteristics;

	public unsafe ushort Magic => OptionalHeader32->Magic;

	public unsafe byte MajorLinkerVersion => OptionalHeader32->MajorLinkerVersion;

	public unsafe byte MinorLinkerVersion => OptionalHeader32->MinorLinkerVersion;

	public unsafe uint SizeOfCode => OptionalHeader32->SizeOfCode;

	public unsafe uint SizeOfInitializedData => OptionalHeader32->SizeOfInitializedData;

	public unsafe uint SizeOfUninitializedData => OptionalHeader32->SizeOfUninitializedData;

	public unsafe uint AddressOfEntryPoint => OptionalHeader32->AddressOfEntryPoint;

	public unsafe uint BaseOfCode => OptionalHeader32->BaseOfCode;

	public unsafe ulong ImageBase
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->ImageBase;
			}
			return OptionalHeader32->ImageBase;
		}
	}

	public unsafe uint SectionAlignment
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SectionAlignment;
			}
			return OptionalHeader32->SectionAlignment;
		}
	}

	public unsafe uint FileAlignment
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->FileAlignment;
			}
			return OptionalHeader32->FileAlignment;
		}
	}

	public unsafe ushort MajorOperatingSystemVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MajorOperatingSystemVersion;
			}
			return OptionalHeader32->MajorOperatingSystemVersion;
		}
	}

	public unsafe ushort MinorOperatingSystemVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MinorOperatingSystemVersion;
			}
			return OptionalHeader32->MinorOperatingSystemVersion;
		}
	}

	public unsafe ushort MajorImageVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MajorImageVersion;
			}
			return OptionalHeader32->MajorImageVersion;
		}
	}

	public unsafe ushort MinorImageVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MinorImageVersion;
			}
			return OptionalHeader32->MinorImageVersion;
		}
	}

	public unsafe ushort MajorSubsystemVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MajorSubsystemVersion;
			}
			return OptionalHeader32->MajorSubsystemVersion;
		}
	}

	public unsafe ushort MinorSubsystemVersion
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->MinorSubsystemVersion;
			}
			return OptionalHeader32->MinorSubsystemVersion;
		}
	}

	public unsafe uint Win32VersionValue
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->Win32VersionValue;
			}
			return OptionalHeader32->Win32VersionValue;
		}
	}

	public unsafe uint SizeOfImage
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfImage;
			}
			return OptionalHeader32->SizeOfImage;
		}
	}

	public unsafe uint SizeOfHeaders
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfHeaders;
			}
			return OptionalHeader32->SizeOfHeaders;
		}
	}

	public unsafe uint CheckSum
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->CheckSum;
			}
			return OptionalHeader32->CheckSum;
		}
	}

	public unsafe ushort Subsystem
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->Subsystem;
			}
			return OptionalHeader32->Subsystem;
		}
	}

	public unsafe ushort DllCharacteristics
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->DllCharacteristics;
			}
			return OptionalHeader32->DllCharacteristics;
		}
	}

	public unsafe ulong SizeOfStackReserve
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfStackReserve;
			}
			return OptionalHeader32->SizeOfStackReserve;
		}
	}

	public unsafe ulong SizeOfStackCommit
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfStackCommit;
			}
			return OptionalHeader32->SizeOfStackCommit;
		}
	}

	public unsafe ulong SizeOfHeapReserve
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfHeapReserve;
			}
			return OptionalHeader32->SizeOfHeapReserve;
		}
	}

	public unsafe ulong SizeOfHeapCommit
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->SizeOfHeapCommit;
			}
			return OptionalHeader32->SizeOfHeapCommit;
		}
	}

	public unsafe uint LoaderFlags
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->LoaderFlags;
			}
			return OptionalHeader32->LoaderFlags;
		}
	}

	public unsafe uint NumberOfRvaAndSizes
	{
		get
		{
			if (IsPE64)
			{
				return OptionalHeader64->NumberOfRvaAndSizes;
			}
			return OptionalHeader32->NumberOfRvaAndSizes;
		}
	}

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ExportDirectory => Directory(0);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ImportDirectory => Directory(1);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ResourceDirectory => Directory(2);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ExceptionDirectory => Directory(3);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY CertificatesDirectory => Directory(4);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY BaseRelocationDirectory => Directory(5);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY DebugDirectory => Directory(6);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ArchitectureDirectory => Directory(7);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY GlobalPointerDirectory => Directory(8);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ThreadStorageDirectory => Directory(9);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY LoadConfigurationDirectory => Directory(10);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY BoundImportDirectory => Directory(11);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ImportAddressTableDirectory => Directory(12);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY DelayImportDirectory => Directory(13);

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ComDescriptorDirectory => Directory(14);

	internal int FileOffsetOfResources
	{
		get
		{
			if (ResourceDirectory.VirtualAddress == 0)
			{
				return 0;
			}
			return RvaToFileOffset((int)ResourceDirectory.VirtualAddress);
		}
	}

	private unsafe IMAGE_OPTIONAL_HEADER32* OptionalHeader32 => (IMAGE_OPTIONAL_HEADER32*)(_ntHeader + 1);

	private unsafe IMAGE_OPTIONAL_HEADER64* OptionalHeader64 => (IMAGE_OPTIONAL_HEADER64*)(_ntHeader + 1);

	private unsafe Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY* NTDirectories
	{
		get
		{
			if (IsPE64)
			{
				return (Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY*)((byte*)(_ntHeader + 1) + sizeof(IMAGE_OPTIONAL_HEADER64));
			}
			return (Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY*)((byte*)(_ntHeader + 1) + sizeof(IMAGE_OPTIONAL_HEADER32));
		}
	}

	internal unsafe static PEHeader FromBuffer(PEBuffer buffer, bool virt)
	{
		byte* ptr = buffer.Fetch(0, 768);
		IMAGE_DOS_HEADER* ptr2 = (IMAGE_DOS_HEADER*)ptr;
		int num = ptr2->e_lfanew + sizeof(IMAGE_NT_HEADERS);
		if (buffer.Length < num)
		{
			ptr = buffer.Fetch(0, num);
			if (buffer.Length < num)
			{
				return null;
			}
			ptr2 = (IMAGE_DOS_HEADER*)ptr;
		}
		IMAGE_NT_HEADERS* ptr3 = (IMAGE_NT_HEADERS*)((byte*)ptr2 + ptr2->e_lfanew);
		num += ptr3->FileHeader.SizeOfOptionalHeader + sizeof(IMAGE_SECTION_HEADER) * ptr3->FileHeader.NumberOfSections;
		if (buffer.Length < num)
		{
			ptr = buffer.Fetch(0, num);
			if (buffer.Length < num)
			{
				return null;
			}
		}
		return new PEHeader(buffer, virt);
	}

	private unsafe PEHeader(PEBuffer buffer, bool virt)
	{
		_virt = virt;
		_ntHeader = (IMAGE_NT_HEADERS*)((byte*)(_dosHeader = (IMAGE_DOS_HEADER*)buffer.Fetch(0, 768)) + _dosHeader->e_lfanew);
		_sections = (IMAGE_SECTION_HEADER*)((byte*)(_ntHeader + 1) + (int)_ntHeader->FileHeader.SizeOfOptionalHeader);
		if (buffer.Length < PEHeaderSize)
		{
			throw new BadImageFormatException();
		}
	}

	public unsafe int VirtualAddressToRva(void* ptr)
	{
		return (int)((byte*)ptr - (byte*)_dosHeader);
	}

	public unsafe void* RvaToVirtualAddress(int rva)
	{
		return (byte*)_dosHeader + rva;
	}

	public unsafe int RvaToFileOffset(int rva)
	{
		if (_virt)
		{
			return rva;
		}
		for (int i = 0; i < _ntHeader->FileHeader.NumberOfSections; i++)
		{
			if (_sections[i].VirtualAddress <= rva && rva < _sections[i].VirtualAddress + _sections[i].VirtualSize)
			{
				return (int)_sections[i].PointerToRawData + (rva - (int)_sections[i].VirtualAddress);
			}
		}
		throw new InvalidOperationException("Illegal RVA 0x" + rva.ToString("x"));
	}

	public unsafe bool TryGetFileOffsetFromRva(int rva, out int result)
	{
		if (_virt)
		{
			result = rva;
			return true;
		}
		if (rva < (int)((byte*)_sections - (byte*)_dosHeader))
		{
			result = rva;
			return true;
		}
		for (int i = 0; i < _ntHeader->FileHeader.NumberOfSections; i++)
		{
			if (_sections[i].VirtualAddress <= rva && rva < _sections[i].VirtualAddress + _sections[i].VirtualSize)
			{
				result = (int)_sections[i].PointerToRawData + (rva - (int)_sections[i].VirtualAddress);
				return true;
			}
		}
		result = 0;
		return false;
	}

	public unsafe Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY Directory(int idx)
	{
		if (idx >= NumberOfRvaAndSizes)
		{
			return default(Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY);
		}
		return NTDirectories[idx];
	}

	internal static DateTime TimeDateStampToDate(int timeDateStampSec)
	{
		DateTime result = new DateTime((long)timeDateStampSec * 10000000L + 621356004000000000L, DateTimeKind.Utc).ToLocalTime();
		if (result.IsDaylightSavingTime())
		{
			result = result.AddHours(-1.0);
		}
		return result;
	}
}
