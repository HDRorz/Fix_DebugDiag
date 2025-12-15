using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class PEImage
{
	private const ushort ExpectedDosHeaderMagic = 23117;

	private const int PESignatureOffsetLocation = 60;

	private const uint ExpectedPESignature = 17744u;

	private const int ImageDataDirectoryCount = 15;

	private const int ComDataDirectory = 14;

	private const int DebugDataDirectory = 6;

	private readonly bool _virt;

	private readonly byte[] _buffer = new byte[260];

	private int _offset;

	private readonly int _peHeaderOffset;

	private readonly Lazy<ImageFileHeader> _imageFileHeader;

	private readonly Lazy<ImageOptionalHeader> _imageOptionalHeader;

	private readonly Lazy<CorHeader> _corHeader;

	private readonly Lazy<List<SectionHeader>> _sections;

	private readonly Lazy<List<PdbInfo>> _pdbs;

	private readonly Lazy<Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY[]> _directories;

	private readonly Lazy<ResourceEntry> _resources;

	private int HeaderOffset => _peHeaderOffset + 4;

	private unsafe int OptionalHeaderOffset => HeaderOffset + sizeof(IMAGE_FILE_HEADER);

	private unsafe int SpecificHeaderOffset => OptionalHeaderOffset + sizeof(IMAGE_OPTIONAL_HEADER_AGNOSTIC);

	private int DataDirectoryOffset => SpecificHeaderOffset + (IsPE64 ? 40 : 24);

	private unsafe int ImageDataDirectoryOffset => DataDirectoryOffset + 15 * sizeof(Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY);

	internal int ResourceVirtualAddress => (int)GetDirectory(2).VirtualAddress;

	public ResourceEntry Resources => _resources.Value;

	public Stream Stream { get; }

	public bool IsValid { get; private set; }

	public bool IsPE64
	{
		get
		{
			if (OptionalHeader == null)
			{
				return false;
			}
			return OptionalHeader.Magic != 267;
		}
	}

	public bool IsManaged => GetDirectory(14).VirtualAddress != 0;

	public int IndexTimeStamp => (int)(Header?.TimeDateStamp ?? 0);

	public int IndexFileSize => (int)(OptionalHeader?.SizeOfImage ?? 0);

	public CorHeader CorHeader => _corHeader.Value;

	public ImageFileHeader Header => _imageFileHeader.Value;

	public ImageOptionalHeader OptionalHeader => _imageOptionalHeader.Value;

	public ReadOnlyCollection<SectionHeader> Sections => _sections.Value.AsReadOnly();

	public ReadOnlyCollection<PdbInfo> Pdbs => _pdbs.Value.AsReadOnly();

	public PdbInfo DefaultPdb => Pdbs.LastOrDefault();

	private Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY GetDirectory(int index)
	{
		return _directories.Value[index];
	}

	public PEImage(Stream stream)
		: this(stream, isVirtual: false)
	{
	}

	public PEImage(Stream stream, bool isVirtual)
	{
		if (!stream.CanSeek)
		{
			throw new ArgumentException("stream is not seekable.");
		}
		_virt = isVirtual;
		Stream = stream;
		if (TryRead<ushort>(0) != 23117)
		{
			IsValid = false;
		}
		else
		{
			_peHeaderOffset = TryRead<int>(60).GetValueOrDefault();
			uint? num = null;
			if (_peHeaderOffset != 0)
			{
				num = TryRead<uint>(_peHeaderOffset);
			}
			IsValid = num.HasValue && num.Value == 17744;
		}
		_imageFileHeader = new Lazy<ImageFileHeader>(ReadImageFileHeader);
		_imageOptionalHeader = new Lazy<ImageOptionalHeader>(ReadImageOptionalHeader);
		_corHeader = new Lazy<CorHeader>(ReadCorHeader);
		_directories = new Lazy<Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY[]>(ReadDataDirectories);
		_sections = new Lazy<List<SectionHeader>>(ReadSections);
		_pdbs = new Lazy<List<PdbInfo>>(ReadPdbs);
		_resources = new Lazy<ResourceEntry>(CreateResourceRoot);
	}

	public int RvaToOffset(int virtualAddress)
	{
		if (_virt)
		{
			return virtualAddress;
		}
		List<SectionHeader> value = _sections.Value;
		for (int i = 0; i < value.Count; i++)
		{
			if (value[i].VirtualAddress <= virtualAddress && virtualAddress < value[i].VirtualAddress + value[i].VirtualSize)
			{
				return (int)value[i].PointerToRawData + (virtualAddress - (int)value[i].VirtualAddress);
			}
		}
		return -1;
	}

	public int Read(IntPtr dest, int virtualAddress, int bytesRequested)
	{
		byte[] buffer = GetBuffer(bytesRequested);
		int num = RvaToOffset(virtualAddress);
		if (num == -1)
		{
			return 0;
		}
		SeekTo(num);
		int num2 = Stream.Read(buffer, 0, bytesRequested);
		if (num2 > 0)
		{
			Marshal.Copy(buffer, 0, dest, num2);
		}
		return num2;
	}

	public int Read(byte[] dest, int virtualAddress, int bytesRequested)
	{
		int num = RvaToOffset(virtualAddress);
		if (num == -1)
		{
			return 0;
		}
		SeekTo(num);
		return Stream.Read(dest, 0, bytesRequested);
	}

	public FileVersionInfo GetFileVersionInfo()
	{
		ResourceEntry resourceEntry = Resources.Children.FirstOrDefault((ResourceEntry r) => r.Name == "Version");
		if (resourceEntry == null || resourceEntry.Children.Count != 1)
		{
			return null;
		}
		resourceEntry = resourceEntry.Children[0];
		if (!resourceEntry.IsLeaf && resourceEntry.Children.Count == 1)
		{
			resourceEntry = resourceEntry.Children[0];
		}
		if (resourceEntry.Size < 16)
		{
			return null;
		}
		return new FileVersionInfo(resourceEntry.GetData(), resourceEntry.Size);
	}

	private ResourceEntry CreateResourceRoot()
	{
		return new ResourceEntry(this, null, "root", leaf: false, RvaToOffset(ResourceVirtualAddress));
	}

	private List<SectionHeader> ReadSections()
	{
		List<SectionHeader> list = new List<SectionHeader>();
		if (!IsValid)
		{
			return list;
		}
		ImageFileHeader header = Header;
		if (header == null)
		{
			return list;
		}
		SeekTo(ImageDataDirectoryOffset);
		if (TryRead<ulong>() != 0)
		{
			return list;
		}
		for (int i = 0; i < header.NumberOfSections; i++)
		{
			IMAGE_SECTION_HEADER? iMAGE_SECTION_HEADER = TryRead<IMAGE_SECTION_HEADER>();
			if (iMAGE_SECTION_HEADER.HasValue)
			{
				list.Add(new SectionHeader(iMAGE_SECTION_HEADER.Value));
			}
		}
		return list;
	}

	private unsafe List<PdbInfo> ReadPdbs()
	{
		_ = _offset;
		List<PdbInfo> list = new List<PdbInfo>();
		Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY directory = GetDirectory(6);
		if (directory.VirtualAddress != 0 && directory.Size != 0)
		{
			if (directory.Size % sizeof(IMAGE_DEBUG_DIRECTORY) != 0L)
			{
				return list;
			}
			int num = RvaToOffset((int)directory.VirtualAddress);
			if (num == -1)
			{
				return list;
			}
			int num2 = (int)directory.Size / sizeof(IMAGE_DEBUG_DIRECTORY);
			List<Tuple<int, int>> list2 = new List<Tuple<int, int>>(num2);
			SeekTo(num);
			for (int i = 0; i < num2; i++)
			{
				IMAGE_DEBUG_DIRECTORY? iMAGE_DEBUG_DIRECTORY = TryRead<IMAGE_DEBUG_DIRECTORY>();
				if (iMAGE_DEBUG_DIRECTORY.HasValue)
				{
					IMAGE_DEBUG_DIRECTORY value = iMAGE_DEBUG_DIRECTORY.Value;
					if (value.Type == IMAGE_DEBUG_TYPE.CODEVIEW && value.SizeOfData >= sizeof(CV_INFO_PDB70))
					{
						list2.Add(Tuple.Create(_virt ? value.AddressOfRawData : value.PointerToRawData, value.SizeOfData));
					}
				}
			}
			foreach (Tuple<int, int> item4 in list2.OrderBy((Tuple<int, int> e) => e.Item1))
			{
				int item = item4.Item1;
				int item2 = item4.Item2;
				int? num3 = TryRead<int>(item);
				if (num3.HasValue && num3.Value == 1396986706)
				{
					Guid valueOrDefault = TryRead<Guid>().GetValueOrDefault();
					int rev = TryRead<int>() ?? (-1);
					int len = item2 - 24 - 1;
					PdbInfo item3 = new PdbInfo(ReadString(len), valueOrDefault, rev);
					list.Add(item3);
				}
			}
		}
		return list;
	}

	private string ReadString(int len)
	{
		return ReadString(_offset, len);
	}

	private string ReadString(int offset, int len)
	{
		if (len > 4096)
		{
			len = 4096;
		}
		SeekTo(offset);
		byte[] buffer = GetBuffer(len);
		if (Stream.Read(buffer, 0, len) != len)
		{
			return null;
		}
		for (int i = 0; i < len; i++)
		{
			if (buffer[i] == 0)
			{
				len = i;
				break;
			}
		}
		return Encoding.ASCII.GetString(buffer, 0, len);
	}

	private T? TryRead<T>() where T : struct
	{
		return TryRead<T>(_offset);
	}

	private unsafe T? TryRead<T>(int offset) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		byte[] buffer = GetBuffer(num);
		SeekTo(offset);
		int num2 = Stream.Read(buffer, 0, num);
		_offset = offset + num2;
		if (num2 != num)
		{
			return null;
		}
		fixed (byte* value = buffer)
		{
			return (T)Marshal.PtrToStructure(new IntPtr(value), typeof(T));
		}
	}

	internal unsafe T Read<T>(ref int offset) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		byte[] buffer = GetBuffer(num);
		SeekTo(offset);
		int num2 = Stream.Read(buffer, 0, num);
		_offset = offset + num2;
		offset += num2;
		if (num2 != num)
		{
			return default(T);
		}
		fixed (byte* value = buffer)
		{
			return (T)Marshal.PtrToStructure(new IntPtr(value), typeof(T));
		}
	}

	internal T Read<T>(int offset) where T : struct
	{
		return Read<T>(ref offset);
	}

	private byte[] GetBuffer(int size)
	{
		if (size <= _buffer.Length)
		{
			return _buffer;
		}
		return new byte[size];
	}

	private void SeekTo(int offset)
	{
		if (offset != _offset)
		{
			Stream.Seek(offset, SeekOrigin.Begin);
			_offset = offset;
		}
	}

	private ImageFileHeader ReadImageFileHeader()
	{
		if (!IsValid)
		{
			return null;
		}
		IMAGE_FILE_HEADER? iMAGE_FILE_HEADER = TryRead<IMAGE_FILE_HEADER>(HeaderOffset);
		if (!iMAGE_FILE_HEADER.HasValue)
		{
			return null;
		}
		return new ImageFileHeader(iMAGE_FILE_HEADER.Value);
	}

	private Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY[] ReadDataDirectories()
	{
		Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY[] array = new Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY[15];
		if (!IsValid)
		{
			return array;
		}
		SeekTo(DataDirectoryOffset);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = TryRead<Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY>().GetValueOrDefault();
		}
		return array;
	}

	private ImageOptionalHeader ReadImageOptionalHeader()
	{
		if (!IsValid)
		{
			return null;
		}
		IMAGE_OPTIONAL_HEADER_AGNOSTIC? iMAGE_OPTIONAL_HEADER_AGNOSTIC = TryRead<IMAGE_OPTIONAL_HEADER_AGNOSTIC>(OptionalHeaderOffset);
		if (!iMAGE_OPTIONAL_HEADER_AGNOSTIC.HasValue)
		{
			return null;
		}
		bool is32Bit = iMAGE_OPTIONAL_HEADER_AGNOSTIC.Value.Magic == 267;
		Lazy<IMAGE_OPTIONAL_HEADER_SPECIFIC> specific = new Lazy<IMAGE_OPTIONAL_HEADER_SPECIFIC>(delegate
		{
			SeekTo(SpecificHeaderOffset);
			IMAGE_OPTIONAL_HEADER_SPECIFIC result = default(IMAGE_OPTIONAL_HEADER_SPECIFIC);
			result.SizeOfStackReserve = ((!is32Bit) ? TryRead<ulong>() : TryRead<uint>()).GetValueOrDefault();
			result.SizeOfStackCommit = ((!is32Bit) ? TryRead<ulong>() : TryRead<uint>()).GetValueOrDefault();
			result.SizeOfHeapReserve = ((!is32Bit) ? TryRead<ulong>() : TryRead<uint>()).GetValueOrDefault();
			result.SizeOfHeapCommit = ((!is32Bit) ? TryRead<ulong>() : TryRead<uint>()).GetValueOrDefault();
			result.LoaderFlags = TryRead<uint>().GetValueOrDefault();
			result.NumberOfRvaAndSizes = TryRead<uint>().GetValueOrDefault();
			return result;
		});
		return new ImageOptionalHeader(iMAGE_OPTIONAL_HEADER_AGNOSTIC.Value, specific, _directories, is32Bit);
	}

	private CorHeader ReadCorHeader()
	{
		int num = RvaToOffset((int)GetDirectory(14).VirtualAddress);
		if (num == -1)
		{
			return null;
		}
		IMAGE_COR20_HEADER? iMAGE_COR20_HEADER = TryRead<IMAGE_COR20_HEADER>(num);
		if (!iMAGE_COR20_HEADER.HasValue)
		{
			return null;
		}
		return new CorHeader(iMAGE_COR20_HEADER.Value);
	}
}
