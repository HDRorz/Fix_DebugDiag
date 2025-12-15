using System;
using System.Runtime.InteropServices;

namespace PEFile;

internal class PEHeader : IDisposable
{
	private unsafe IMAGE_DOS_HEADER* dosHeader;

	private unsafe IMAGE_NT_HEADERS* ntHeader;

	private unsafe IMAGE_SECTION_HEADER* sections;

	private GCHandle pinningHandle;

	/// <summary>
	/// The total size, including section array of the the PE header.  
	/// </summary>
	internal unsafe int Size => VirtualAddressToRva(sections) + sizeof(IMAGE_SECTION_HEADER) * ntHeader->FileHeader.NumberOfSections;

	internal unsafe bool IsPE64 => OptionalHeader32->Magic == 523;

	internal bool IsManaged => ComDescriptorDirectory.VirtualAddress != 0;

	internal unsafe uint Signature => ntHeader->Signature;

	internal unsafe MachineType Machine => (MachineType)ntHeader->FileHeader.Machine;

	internal unsafe ushort NumberOfSections => ntHeader->FileHeader.NumberOfSections;

	internal unsafe int TimeDateStampSec => (int)ntHeader->FileHeader.TimeDateStamp;

	internal DateTime TimeDateStamp => TimeDateStampToDate(TimeDateStampSec);

	internal unsafe ulong PointerToSymbolTable => ntHeader->FileHeader.PointerToSymbolTable;

	internal unsafe ulong NumberOfSymbols => ntHeader->FileHeader.NumberOfSymbols;

	internal unsafe ushort SizeOfOptionalHeader => ntHeader->FileHeader.SizeOfOptionalHeader;

	internal unsafe ushort Characteristics => ntHeader->FileHeader.Characteristics;

	internal unsafe ushort Magic => OptionalHeader32->Magic;

	internal unsafe byte MajorLinkerVersion => OptionalHeader32->MajorLinkerVersion;

	internal unsafe byte MinorLinkerVersion => OptionalHeader32->MinorLinkerVersion;

	internal unsafe uint SizeOfCode => OptionalHeader32->SizeOfCode;

	internal unsafe uint SizeOfInitializedData => OptionalHeader32->SizeOfInitializedData;

	internal unsafe uint SizeOfUninitializedData => OptionalHeader32->SizeOfUninitializedData;

	internal unsafe uint AddressOfEntryPoint => OptionalHeader32->AddressOfEntryPoint;

	internal unsafe uint BaseOfCode => OptionalHeader32->BaseOfCode;

	internal unsafe ulong ImageBase
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

	internal unsafe uint SectionAlignment
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

	internal unsafe uint FileAlignment
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

	internal unsafe ushort MajorOperatingSystemVersion
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

	internal unsafe ushort MinorOperatingSystemVersion
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

	internal unsafe ushort MajorImageVersion
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

	internal unsafe ushort MinorImageVersion
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

	internal unsafe ushort MajorSubsystemVersion
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

	internal unsafe ushort MinorSubsystemVersion
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

	internal unsafe uint Win32VersionValue
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

	internal unsafe uint SizeOfImage
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

	internal unsafe uint SizeOfHeaders
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

	internal unsafe uint CheckSum
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

	internal unsafe ushort Subsystem
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

	internal unsafe ushort DllCharacteristics
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

	internal unsafe ulong SizeOfStackReserve
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

	internal unsafe ulong SizeOfStackCommit
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

	internal unsafe ulong SizeOfHeapReserve
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

	internal unsafe ulong SizeOfHeapCommit
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

	internal unsafe uint LoaderFlags
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

	internal unsafe uint NumberOfRvaAndSizes
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

	internal IMAGE_DATA_DIRECTORY ExportDirectory => Directory(0);

	internal IMAGE_DATA_DIRECTORY ImportDirectory => Directory(1);

	internal IMAGE_DATA_DIRECTORY ResourceDirectory => Directory(2);

	internal IMAGE_DATA_DIRECTORY ExceptionDirectory => Directory(3);

	internal IMAGE_DATA_DIRECTORY CertificatesDirectory => Directory(4);

	internal IMAGE_DATA_DIRECTORY BaseRelocationDirectory => Directory(5);

	internal IMAGE_DATA_DIRECTORY DebugDirectory => Directory(6);

	internal IMAGE_DATA_DIRECTORY ArchitectureDirectory => Directory(7);

	internal IMAGE_DATA_DIRECTORY GlobalPointerDirectory => Directory(8);

	internal IMAGE_DATA_DIRECTORY ThreadStorageDirectory => Directory(9);

	internal IMAGE_DATA_DIRECTORY LoadConfigurationDirectory => Directory(10);

	internal IMAGE_DATA_DIRECTORY BoundImportDirectory => Directory(11);

	internal IMAGE_DATA_DIRECTORY ImportAddressTableDirectory => Directory(12);

	internal IMAGE_DATA_DIRECTORY DelayImportDirectory => Directory(13);

	internal IMAGE_DATA_DIRECTORY ComDescriptorDirectory => Directory(14);

	internal int FileOffsetOfResources
	{
		get
		{
			if (ResourceDirectory.VirtualAddress == 0)
			{
				return 0;
			}
			return RvaToFileOffset(ResourceDirectory.VirtualAddress);
		}
	}

	private unsafe IMAGE_OPTIONAL_HEADER32* OptionalHeader32 => (IMAGE_OPTIONAL_HEADER32*)(ntHeader + 1);

	private unsafe IMAGE_OPTIONAL_HEADER64* OptionalHeader64 => (IMAGE_OPTIONAL_HEADER64*)(ntHeader + 1);

	private unsafe IMAGE_DATA_DIRECTORY* ntDirectories
	{
		get
		{
			if (IsPE64)
			{
				return (IMAGE_DATA_DIRECTORY*)((byte*)(ntHeader + 1) + sizeof(IMAGE_OPTIONAL_HEADER64));
			}
			return (IMAGE_DATA_DIRECTORY*)((byte*)(ntHeader + 1) + sizeof(IMAGE_OPTIONAL_HEADER32));
		}
	}

	/// <summary>
	/// Returns a PEHeader for pointer in memory.  It does NO validity checking. 
	/// </summary>
	/// <param name="startOfPEFile"></param>
	internal unsafe PEHeader(IntPtr startOfPEFile)
		: this((void*)startOfPEFile)
	{
	}

	internal unsafe PEHeader(void* startOfPEFile)
	{
		dosHeader = (IMAGE_DOS_HEADER*)startOfPEFile;
		ntHeader = (IMAGE_NT_HEADERS*)((byte*)startOfPEFile + dosHeader->e_lfanew);
		sections = (IMAGE_SECTION_HEADER*)((byte*)(ntHeader + 1) + (int)ntHeader->FileHeader.SizeOfOptionalHeader);
	}

	internal unsafe int VirtualAddressToRva(void* ptr)
	{
		return (int)((byte*)ptr - (byte*)dosHeader);
	}

	internal unsafe void* RvaToVirtualAddress(int rva)
	{
		return (byte*)dosHeader + rva;
	}

	internal unsafe int RvaToFileOffset(int rva)
	{
		for (int i = 0; i < ntHeader->FileHeader.NumberOfSections; i++)
		{
			if (sections[i].VirtualAddress <= rva && rva < sections[i].VirtualAddress + sections[i].VirtualSize)
			{
				return (int)sections[i].PointerToRawData + (rva - (int)sections[i].VirtualAddress);
			}
		}
		throw new InvalidOperationException("Illegal RVA 0x" + rva.ToString("x"));
	}

	/// <summary>
	/// PEHeader pins a buffer, if you wish to eagerly dispose of this, it can be done here.  
	/// </summary>
	public unsafe void Dispose()
	{
		if (pinningHandle.IsAllocated)
		{
			pinningHandle.Free();
		}
		dosHeader = null;
		ntHeader = null;
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

	internal unsafe IMAGE_DATA_DIRECTORY Directory(int idx)
	{
		if (idx >= NumberOfRvaAndSizes)
		{
			return default(IMAGE_DATA_DIRECTORY);
		}
		return ntDirectories[idx];
	}
}
