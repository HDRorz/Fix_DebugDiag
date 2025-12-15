using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfCoreFile
{
	private readonly Reader _reader;

	private ElfLoadedImage[] _loadedImages;

	private Dictionary<ulong, ulong> _auxvEntries;

	private ELFVirtualAddressSpace _virtualAddressSpace;

	public ElfFile ElfFile { get; }

	public IReadOnlyCollection<ElfLoadedImage> LoadedImages
	{
		get
		{
			LoadFileTable();
			return (IReadOnlyCollection<ElfLoadedImage>)(object)_loadedImages;
		}
	}

	public IEnumerable<IElfPRStatus> EnumeratePRStatus()
	{
		ElfMachine architecture = ElfFile.Header.Architecture;
		return from r in GetNotes(ElfNoteType.PrpsStatus)
			select architecture switch
			{
				ElfMachine.EM_X86_64 => r.ReadContents<ElfPRStatusX64>(0L), 
				ElfMachine.EM_ARM => r.ReadContents<ElfPRStatusArm>(0L), 
				ElfMachine.EM_AARCH64 => r.ReadContents<ElfPRStatusArm64>(0L), 
				ElfMachine.EM_386 => r.ReadContents<ElfPRStatusX86>(0L), 
				_ => throw new NotSupportedException($"Invalid architecture {architecture}"), 
			};
	}

	public ulong GetAuxvValue(ElfAuxvType type)
	{
		LoadAuxvTable();
		_auxvEntries.TryGetValue((ulong)type, out var value);
		return value;
	}

	public ElfCoreFile(Stream stream)
	{
		_reader = new Reader(new StreamAddressSpace(stream));
		ElfFile = new ElfFile(_reader, 0L);
		if (ElfFile.Header.Type != ElfHeaderType.Core)
		{
			throw new InvalidDataException((stream.GetFilename() ?? "The given stream") + " is not a coredump");
		}
	}

	public int ReadMemory(long address, byte[] buffer, int bytesRequested)
	{
		if (_virtualAddressSpace == null)
		{
			_virtualAddressSpace = new ELFVirtualAddressSpace(ElfFile.ProgramHeaders, _reader.DataSource);
		}
		return _virtualAddressSpace.Read(address, buffer, 0, bytesRequested);
	}

	private IEnumerable<ElfNote> GetNotes(ElfNoteType type)
	{
		return ElfFile.Notes.Where((ElfNote n) => n.Type == type);
	}

	private void LoadAuxvTable()
	{
		if (_auxvEntries != null)
		{
			return;
		}
		_auxvEntries = new Dictionary<ulong, ulong>();
		ElfNote elfNote = GetNotes(ElfNoteType.Aux).SingleOrDefault();
		if (elfNote == null)
		{
			throw new BadImageFormatException("No auxv entries in coredump");
		}
		long position = 0L;
		while (true)
		{
			ulong num;
			ulong value;
			if (ElfFile.Header.Is64Bit)
			{
				ElfAuxv64 elfAuxv = elfNote.ReadContents<ElfAuxv64>(ref position);
				num = elfAuxv.Type;
				value = elfAuxv.Value;
			}
			else
			{
				ElfAuxv32 elfAuxv2 = elfNote.ReadContents<ElfAuxv32>(ref position);
				num = elfAuxv2.Type;
				value = elfAuxv2.Value;
			}
			if (num != 0L)
			{
				_auxvEntries.Add(num, value);
				continue;
			}
			break;
		}
	}

	private void LoadFileTable()
	{
		if (_loadedImages != null)
		{
			return;
		}
		ElfNote elfNote = GetNotes(ElfNoteType.File).Single();
		long position = 0L;
		ulong num = 0uL;
		num = ((!ElfFile.Header.Is64Bit) ? elfNote.ReadContents<ElfFileTableHeader32>(ref position).EntryCount : elfNote.ReadContents<ElfFileTableHeader64>(ref position).EntryCount);
		ElfFileTableEntryPointers64[] array = new ElfFileTableEntryPointers64[num];
		Dictionary<string, ElfLoadedImage> dictionary = new Dictionary<string, ElfLoadedImage>(array.Length);
		for (int j = 0; j < array.Length; j++)
		{
			if (ElfFile.Header.Is64Bit)
			{
				array[j] = elfNote.ReadContents<ElfFileTableEntryPointers64>(ref position);
				continue;
			}
			ElfFileTableEntryPointers32 elfFileTableEntryPointers = elfNote.ReadContents<ElfFileTableEntryPointers32>(ref position);
			array[j].Start = elfFileTableEntryPointers.Start;
			array[j].Stop = elfFileTableEntryPointers.Stop;
			array[j].PageOffset = elfFileTableEntryPointers.PageOffset;
		}
		long num2 = elfNote.Header.ContentSize - position;
		byte[] array2 = elfNote.ReadContents(position, (int)num2);
		int num3 = 0;
		for (int k = 0; k < array.Length; k++)
		{
			int l;
			for (l = num3; array2[l] != 0; l++)
			{
			}
			string @string = Encoding.ASCII.GetString(array2, num3, l - num3);
			num3 = l + 1;
			if (!dictionary.TryGetValue(@string, out var value))
			{
				ElfLoadedImage elfLoadedImage2 = (dictionary[@string] = new ElfLoadedImage(ElfFile.VirtualAddressReader, ElfFile.Header.Is64Bit, @string));
				value = elfLoadedImage2;
			}
			value.AddTableEntryPointers(array[k]);
		}
		_loadedImages = dictionary.Values.OrderBy((ElfLoadedImage i) => i.BaseAddress).ToArray();
	}
}
