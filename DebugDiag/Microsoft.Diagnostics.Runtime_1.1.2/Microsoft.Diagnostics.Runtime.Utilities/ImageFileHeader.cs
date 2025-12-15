using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class ImageFileHeader
{
	private IMAGE_FILE_HEADER _header;

	public IMAGE_FILE_MACHINE Machine => (IMAGE_FILE_MACHINE)_header.Machine;

	public ushort NumberOfSections => _header.NumberOfSections;

	public uint PointerToSymbolTable => _header.PointerToSymbolTable;

	public uint NumberOfSymbols => _header.NumberOfSymbols;

	public uint TimeDateStamp => _header.TimeDateStamp;

	public ushort SizeOfOptionalHeader => _header.SizeOfOptionalHeader;

	public IMAGE_FILE Characteristics => (IMAGE_FILE)_header.Characteristics;

	internal ImageFileHeader(IMAGE_FILE_HEADER header)
	{
		_header = header;
	}
}
