namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct IMAGE_RESOURCE_DIRECTORY_ENTRY
{
	private int _nameOffsetAndFlag;

	private int _dataOffsetAndFlag;

	public bool IsStringName => _nameOffsetAndFlag < 0;

	public int NameOffset => _nameOffsetAndFlag & 0x7FFFFFFF;

	public bool IsLeaf => (0x80000000u & _dataOffsetAndFlag) == 0;

	public int DataOffset => _dataOffsetAndFlag & 0x7FFFFFFF;

	public int Id => 0xFFFF & _nameOffsetAndFlag;

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
