using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfFile
{
	private readonly Reader _reader;

	private readonly long _position;

	private readonly bool _virtual;

	private Reader _virtualAddressReader;

	private ElfNote[] _notes;

	private ElfProgramHeader[] _programHeaders;

	private ElfSectionHeader[] _sections;

	private string[] _sectionNames;

	private byte[] _sectionNameTable;

	public IElfHeader Header { get; }

	public IReadOnlyCollection<ElfNote> Notes
	{
		get
		{
			LoadNotes();
			return (IReadOnlyCollection<ElfNote>)(object)_notes;
		}
	}

	public IReadOnlyList<ElfProgramHeader> ProgramHeaders
	{
		get
		{
			LoadProgramHeaders();
			return _programHeaders;
		}
	}

	public Reader VirtualAddressReader
	{
		get
		{
			CreateVirtualAddressReader();
			return _virtualAddressReader;
		}
	}

	public byte[] BuildId
	{
		get
		{
			if (Header.ProgramHeaderOffset != 0L && Header.ProgramHeaderEntrySize > 0 && Header.ProgramHeaderCount > 0)
			{
				try
				{
					foreach (ElfNote note in Notes)
					{
						if (note.Type == ElfNoteType.PrpsInfo && note.Name.Equals("GNU"))
						{
							return note.ReadContents(0L, (int)note.Header.ContentSize);
						}
					}
				}
				catch (IOException)
				{
				}
			}
			return null;
		}
	}

	public ElfFile(Reader reader, long position = 0L, bool isVirtual = false)
	{
		_reader = reader;
		_position = position;
		_virtual = isVirtual;
		if (isVirtual)
		{
			_virtualAddressReader = reader;
		}
		Header = reader.Read<ElfHeaderCommon>(position).GetHeader(reader, position);
		if (Header == null)
		{
			throw new InvalidDataException((reader.DataSource.Name ?? "This coredump") + " does not contain a valid ELF header.");
		}
	}

	internal ElfFile(IElfHeader header, Reader reader, long position = 0L, bool isVirtual = false)
	{
		_reader = reader;
		_position = position;
		_virtual = isVirtual;
		if (isVirtual)
		{
			_virtualAddressReader = reader;
		}
		Header = header;
	}

	private void CreateVirtualAddressReader()
	{
		if (_virtualAddressReader == null)
		{
			_virtualAddressReader = new Reader(new ELFVirtualAddressSpace(ProgramHeaders, _reader.DataSource));
		}
	}

	private void LoadNotes()
	{
		if (_notes != null)
		{
			return;
		}
		LoadProgramHeaders();
		List<ElfNote> list = new List<ElfNote>();
		ElfProgramHeader[] programHeaders = _programHeaders;
		foreach (ElfProgramHeader elfProgramHeader in programHeaders)
		{
			if (elfProgramHeader.Type == ElfProgramHeaderType.Note)
			{
				Reader reader = new Reader(elfProgramHeader.AddressSpace);
				ElfNote elfNote;
				for (long num = 0L; num < reader.DataSource.Length; num += elfNote.TotalSize)
				{
					elfNote = new ElfNote(reader, num);
					list.Add(elfNote);
				}
			}
		}
		_notes = list.ToArray();
	}

	private void LoadProgramHeaders()
	{
		if (_programHeaders == null)
		{
			_programHeaders = new ElfProgramHeader[Header.ProgramHeaderCount];
			for (int i = 0; i < _programHeaders.Length; i++)
			{
				_programHeaders[i] = new ElfProgramHeader(_reader, Header.Is64Bit, _position + Header.ProgramHeaderOffset + i * Header.ProgramHeaderEntrySize, _position, _virtual);
			}
		}
	}

	private string GetSectionName(int section)
	{
		LoadSections();
		if (section < 0 || section >= _sections.Length)
		{
			throw new ArgumentOutOfRangeException("section");
		}
		if (_sectionNames == null)
		{
			_sectionNames = new string[_sections.Length];
		}
		if (_sectionNames[section] != null)
		{
			return _sectionNames[section];
		}
		LoadSectionNameTable();
		ref ElfSectionHeader reference = ref _sections[section];
		int nameIndex = reference.NameIndex;
		if (reference.Type == ElfSectionHeaderType.Null || nameIndex == 0)
		{
			return _sectionNames[section] = string.Empty;
		}
		int num = 0;
		for (num = 0; nameIndex + num < _sectionNameTable.Length && _sectionNameTable[nameIndex + num] != 0; num++)
		{
		}
		string @string = Encoding.ASCII.GetString(_sectionNameTable, nameIndex, num);
		_sectionNames[section] = @string;
		return _sectionNames[section];
	}

	private void LoadSectionNameTable()
	{
		checked
		{
			if (_sectionNameTable == null)
			{
				int sectionHeaderStringIndex = Header.SectionHeaderStringIndex;
				if (Header.SectionHeaderOffset != 0L && Header.SectionHeaderCount > 0 && sectionHeaderStringIndex != 0)
				{
					ref ElfSectionHeader reference = ref _sections[sectionHeaderStringIndex];
					long offset = (long)reference.FileOffset;
					int size = (int)reference.FileSize;
					_sectionNameTable = _reader.ReadBytes(offset, size);
				}
			}
		}
	}

	private void LoadSections()
	{
		if (_sections == null)
		{
			_sections = new ElfSectionHeader[Header.SectionHeaderCount];
			for (int i = 0; i < _sections.Length; i++)
			{
				_sections[i] = new ElfSectionHeader(_reader, Header.Is64Bit, _position + Header.SectionHeaderOffset + i * Header.SectionHeaderEntrySize);
			}
		}
	}
}
