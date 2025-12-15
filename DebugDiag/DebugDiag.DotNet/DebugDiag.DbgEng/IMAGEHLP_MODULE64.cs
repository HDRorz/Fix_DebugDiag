using System;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgEng;

public struct IMAGEHLP_MODULE64
{
	private const int MAX_PATH = 260;

	public uint SizeOfStruct;

	public ulong BaseOfImage;

	public uint ImageSize;

	public uint TimeDateStamp;

	public uint CheckSum;

	public uint NumSyms;

	public DEBUG_SYMTYPE SymType;

	private unsafe fixed char _ModuleName[32];

	private unsafe fixed char _ImageName[256];

	private unsafe fixed char _LoadedImageName[256];

	private unsafe fixed char _LoadedPdbName[256];

	public uint CVSig;

	public unsafe fixed char CVData[780];

	public uint PdbSig;

	public Guid PdbSig70;

	public uint PdbAge;

	private uint bPdbUnmatched;

	private uint bDbgUnmatched;

	private uint bLineNumbers;

	private uint bGlobalSymbols;

	private uint bTypeInfo;

	private uint bSourceIndexed;

	private uint bPublics;

	public bool PdbUnmatched
	{
		get
		{
			return bPdbUnmatched != 0;
		}
		set
		{
			bPdbUnmatched = (value ? 1u : 0u);
		}
	}

	public bool DbgUnmatched
	{
		get
		{
			return bDbgUnmatched != 0;
		}
		set
		{
			bDbgUnmatched = (value ? 1u : 0u);
		}
	}

	public bool LineNumbers
	{
		get
		{
			return bLineNumbers != 0;
		}
		set
		{
			bLineNumbers = (value ? 1u : 0u);
		}
	}

	public bool GlobalSymbols
	{
		get
		{
			return bGlobalSymbols != 0;
		}
		set
		{
			bGlobalSymbols = (value ? 1u : 0u);
		}
	}

	public bool TypeInfo
	{
		get
		{
			return bTypeInfo != 0;
		}
		set
		{
			bTypeInfo = (value ? 1u : 0u);
		}
	}

	public bool SourceIndexed
	{
		get
		{
			return bSourceIndexed != 0;
		}
		set
		{
			bSourceIndexed = (value ? 1u : 0u);
		}
	}

	public bool Publics
	{
		get
		{
			return bPublics != 0;
		}
		set
		{
			bPublics = (value ? 1u : 0u);
		}
	}

	public unsafe string ModuleName
	{
		get
		{
			fixed (char* moduleName = _ModuleName)
			{
				return Marshal.PtrToStringUni((IntPtr)moduleName, 32);
			}
		}
	}

	public unsafe string ImageName
	{
		get
		{
			fixed (char* imageName = _ImageName)
			{
				return Marshal.PtrToStringUni((IntPtr)imageName, 256);
			}
		}
	}

	public unsafe string LoadedImageName
	{
		get
		{
			fixed (char* loadedImageName = _LoadedImageName)
			{
				return Marshal.PtrToStringUni((IntPtr)loadedImageName, 256);
			}
		}
	}

	public unsafe string LoadedPdbName
	{
		get
		{
			fixed (char* loadedPdbName = _LoadedPdbName)
			{
				return Marshal.PtrToStringUni((IntPtr)loadedPdbName, 256);
			}
		}
	}
}
