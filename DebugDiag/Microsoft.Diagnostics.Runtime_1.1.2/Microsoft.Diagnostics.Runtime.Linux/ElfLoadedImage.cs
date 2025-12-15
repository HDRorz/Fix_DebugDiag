using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfLoadedImage
{
	private readonly List<ElfFileTableEntryPointers64> _fileTable = new List<ElfFileTableEntryPointers64>(4);

	private readonly Reader _vaReader;

	private readonly bool _is64bit;

	private long _end;

	public string Path { get; }

	public long BaseAddress { get; private set; }

	public long Size => _end - BaseAddress;

	public ElfLoadedImage(Reader virtualAddressReader, bool is64bit, string path)
	{
		_vaReader = virtualAddressReader;
		_is64bit = is64bit;
		Path = path;
	}

	public ElfFile Open()
	{
		IElfHeader elfHeader = (IElfHeader)((!_is64bit) ? ((object)_vaReader.TryRead<ElfHeader32>(BaseAddress)) : ((object)_vaReader.TryRead<ElfHeader64>(BaseAddress)));
		if (elfHeader == null || !elfHeader.IsValid)
		{
			return null;
		}
		return new ElfFile(elfHeader, _vaReader, BaseAddress, isVirtual: true);
	}

	public PEImage OpenAsPEImage()
	{
		return new PEImage(new ReaderStream(BaseAddress, _vaReader), isVirtual: false);
	}

	internal void AddTableEntryPointers(ElfFileTableEntryPointers64 pointers)
	{
		_fileTable.Add(pointers);
		checked
		{
			long num = (long)pointers.Start;
			if (BaseAddress == 0L || num < BaseAddress)
			{
				BaseAddress = num;
			}
			long num2 = (long)pointers.Stop;
			if (_end < num2)
			{
				_end = num2;
			}
		}
	}

	public override string ToString()
	{
		return Path;
	}
}
