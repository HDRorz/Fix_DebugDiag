using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ElfNote
{
	private readonly Reader _reader;

	private readonly long _position;

	private string _name;

	public ElfNoteHeader Header { get; }

	public ElfNoteType Type => Header.Type;

	public string Name
	{
		get
		{
			if (_name != null)
			{
				return _name;
			}
			long position = _position + HeaderSize;
			_name = _reader.ReadNullTerminatedAscii(position, (int)Header.NameSize);
			return _name;
		}
	}

	public long TotalSize => HeaderSize + Align4(Header.NameSize) + Align4(Header.ContentSize);

	private int HeaderSize => Marshal.SizeOf(typeof(ElfNoteHeader));

	public byte[] ReadContents(long position, int length)
	{
		long num = _position + HeaderSize + Align4(Header.NameSize);
		return _reader.ReadBytes(position + num, length);
	}

	public T ReadContents<T>(long position, uint nameSize) where T : struct
	{
		long num = _position + HeaderSize + Align4(Header.NameSize);
		return _reader.Read<T>(num + position);
	}

	public T ReadContents<T>(ref long position) where T : struct
	{
		long num = _position + HeaderSize + Align4(Header.NameSize) + position;
		long position2 = num;
		T result = _reader.Read<T>(ref position2);
		position += position2 - num;
		return result;
	}

	public T ReadContents<T>(long position) where T : struct
	{
		long position2 = _position + HeaderSize + Align4(Header.NameSize) + position;
		return _reader.Read<T>(position2);
	}

	public ElfNote(Reader reader, long position)
	{
		_position = position;
		_reader = reader;
		Header = _reader.Read<ElfNoteHeader>(_position);
	}

	private uint Align4(uint x)
	{
		return (x + 3) & 0xFFFFFFFCu;
	}
}
