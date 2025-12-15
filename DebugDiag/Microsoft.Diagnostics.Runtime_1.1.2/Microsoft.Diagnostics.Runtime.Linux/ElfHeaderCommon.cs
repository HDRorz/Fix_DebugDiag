using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Linux;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ElfHeaderCommon
{
	private const int EI_NIDENT = 16;

	private const byte Magic0 = 127;

	private const byte Magic1 = 69;

	private const byte Magic2 = 76;

	private const byte Magic3 = 70;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
	private readonly byte[] _ident;

	private readonly ElfHeaderType _type;

	private readonly ushort _machine;

	private readonly uint _version;

	public bool IsValid
	{
		get
		{
			if (_ident[0] != 127 || _ident[1] != 69 || _ident[2] != 76 || _ident[3] != 70)
			{
				return false;
			}
			return Data == ElfData.LittleEndian;
		}
	}

	public ElfHeaderType Type => _type;

	public ElfMachine Architecture => (ElfMachine)_machine;

	public ElfClass Class => (ElfClass)_ident[4];

	public ElfData Data => (ElfData)_ident[5];

	public IElfHeader GetHeader(Reader reader, long position)
	{
		if (IsValid)
		{
			switch (Architecture)
			{
			case ElfMachine.EM_X86_64:
			case ElfMachine.EM_AARCH64:
				return reader.Read<ElfHeader64>(position);
			case ElfMachine.EM_386:
			case ElfMachine.EM_ARM:
				return reader.Read<ElfHeader32>(position);
			}
		}
		return null;
	}
}
