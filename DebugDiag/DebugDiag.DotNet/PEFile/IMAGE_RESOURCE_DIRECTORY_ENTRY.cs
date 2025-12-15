namespace PEFile;

internal struct IMAGE_RESOURCE_DIRECTORY_ENTRY
{
	private int NameOffsetAndFlag;

	private int DataOffsetAndFlag;

	internal bool IsStringName => NameOffsetAndFlag < 0;

	internal int NameOffset => NameOffsetAndFlag & 0x7FFFFFFF;

	internal bool IsLeaf => (0x80000000u & DataOffsetAndFlag) == 0;

	internal int DataOffset => DataOffsetAndFlag & 0x7FFFFFFF;

	internal int Id => 0xFFFF & NameOffsetAndFlag;

	internal unsafe string GetName(PEBuffer buff, int resourceStartFileOffset)
	{
		if (IsStringName)
		{
			int size = *(ushort*)buff.Fetch(NameOffset + resourceStartFileOffset, 2);
			char* value = (char*)buff.Fetch(NameOffset + resourceStartFileOffset + 2, size);
			return new string(value);
		}
		return Id.ToString();
	}

	internal static string GetTypeNameForTypeId(int typeId)
	{
		return typeId switch
		{
			1 => "Cursor", 
			2 => "BitMap", 
			3 => "Icon", 
			4 => "Menu", 
			5 => "Dialog", 
			6 => "String", 
			7 => "FontDir", 
			8 => "Font", 
			9 => "Accelerator", 
			10 => "RCData", 
			11 => "MessageTable", 
			12 => "GroupCursor", 
			14 => "GroupIcon", 
			16 => "Version", 
			19 => "PlugPlay", 
			20 => "Vxd", 
			21 => "Aniicursor", 
			22 => "Aniicon", 
			23 => "Html", 
			24 => "RT_MANIFEST", 
			_ => typeId.ToString(), 
		};
	}
}
