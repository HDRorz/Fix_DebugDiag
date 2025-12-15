using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Interop;

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

	private uint _bPdbUnmatched;

	private uint _bDbgUnmatched;

	private uint _bLineNumbers;

	private uint _bGlobalSymbols;

	private uint _bTypeInfo;

	private uint _bSourceIndexed;

	private uint _bPublics;

	public bool PdbUnmatched
	{
		get
		{
			return _bPdbUnmatched != 0;
		}
		set
		{
			_bPdbUnmatched = (value ? 1u : 0u);
		}
	}

	public bool DbgUnmatched
	{
		get
		{
			return _bDbgUnmatched != 0;
		}
		set
		{
			_bDbgUnmatched = (value ? 1u : 0u);
		}
	}

	public bool LineNumbers
	{
		get
		{
			return _bLineNumbers != 0;
		}
		set
		{
			_bLineNumbers = (value ? 1u : 0u);
		}
	}

	public bool GlobalSymbols
	{
		get
		{
			return _bGlobalSymbols != 0;
		}
		set
		{
			_bGlobalSymbols = (value ? 1u : 0u);
		}
	}

	public bool TypeInfo
	{
		get
		{
			return _bTypeInfo != 0;
		}
		set
		{
			_bTypeInfo = (value ? 1u : 0u);
		}
	}

	public bool SourceIndexed
	{
		get
		{
			return _bSourceIndexed != 0;
		}
		set
		{
			_bSourceIndexed = (value ? 1u : 0u);
		}
	}

	public bool Publics
	{
		get
		{
			return _bPublics != 0;
		}
		set
		{
			_bPublics = (value ? 1u : 0u);
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
