using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

internal struct SigParser
{
	private byte[] _sig;

	private int _len;

	private int _offs;

	private const int mdtModule = 0;

	private const int mdtTypeRef = 16777216;

	private const int mdtTypeDef = 33554432;

	private const int mdtFieldDef = 67108864;

	private const int mdtMethodDef = 100663296;

	private const int mdtParamDef = 134217728;

	private const int mdtInterfaceImpl = 150994944;

	private const int mdtMemberRef = 167772160;

	private const int mdtCustomAttribute = 201326592;

	private const int mdtPermission = 234881024;

	private const int mdtSignature = 285212672;

	private const int mdtEvent = 335544320;

	private const int mdtProperty = 385875968;

	private const int mdtMethodImpl = 419430400;

	private const int mdtModuleRef = 436207616;

	private const int mdtTypeSpec = 452984832;

	private const int mdtAssembly = 536870912;

	private const int mdtAssemblyRef = 587202560;

	private const int mdtFile = 637534208;

	private const int mdtExportedType = 654311424;

	private const int mdtManifestResource = 671088640;

	private const int mdtGenericParam = 704643072;

	private const int mdtMethodSpec = 721420288;

	private const int mdtGenericParamConstraint = 738197504;

	private const int mdtString = 1879048192;

	private const int mdtName = 1895825408;

	private const int mdtBaseType = 1912602624;

	private static readonly int[] s_tkCorEncodeToken = new int[4] { 33554432, 16777216, 452984832, 1912602624 };

	private const int IMAGE_CEE_CS_CALLCONV_DEFAULT = 0;

	public const int IMAGE_CEE_CS_CALLCONV_VARARG = 5;

	public const int IMAGE_CEE_CS_CALLCONV_FIELD = 6;

	public const int IMAGE_CEE_CS_CALLCONV_LOCAL_SIG = 7;

	public const int IMAGE_CEE_CS_CALLCONV_PROPERTY = 8;

	public const int IMAGE_CEE_CS_CALLCONV_UNMGD = 9;

	public const int IMAGE_CEE_CS_CALLCONV_GENERICINST = 10;

	public const int IMAGE_CEE_CS_CALLCONV_NATIVEVARARG = 11;

	public const int IMAGE_CEE_CS_CALLCONV_MAX = 12;

	public const int IMAGE_CEE_CS_CALLCONV_MASK = 15;

	public const int IMAGE_CEE_CS_CALLCONV_HASTHIS = 32;

	public const int IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 64;

	public const int IMAGE_CEE_CS_CALLCONV_GENERIC = 16;

	private const int ELEMENT_TYPE_END = 0;

	private const int ELEMENT_TYPE_VOID = 1;

	private const int ELEMENT_TYPE_BOOLEAN = 2;

	private const int ELEMENT_TYPE_CHAR = 3;

	private const int ELEMENT_TYPE_I1 = 4;

	private const int ELEMENT_TYPE_U1 = 5;

	private const int ELEMENT_TYPE_I2 = 6;

	private const int ELEMENT_TYPE_U2 = 7;

	private const int ELEMENT_TYPE_I4 = 8;

	private const int ELEMENT_TYPE_U4 = 9;

	private const int ELEMENT_TYPE_I8 = 10;

	private const int ELEMENT_TYPE_U8 = 11;

	private const int ELEMENT_TYPE_R4 = 12;

	private const int ELEMENT_TYPE_R8 = 13;

	private const int ELEMENT_TYPE_STRING = 14;

	private const int ELEMENT_TYPE_PTR = 15;

	private const int ELEMENT_TYPE_BYREF = 16;

	private const int ELEMENT_TYPE_VALUETYPE = 17;

	private const int ELEMENT_TYPE_CLASS = 18;

	private const int ELEMENT_TYPE_VAR = 19;

	private const int ELEMENT_TYPE_ARRAY = 20;

	private const int ELEMENT_TYPE_GENERICINST = 21;

	private const int ELEMENT_TYPE_TYPEDBYREF = 22;

	private const int ELEMENT_TYPE_I = 24;

	private const int ELEMENT_TYPE_U = 25;

	private const int ELEMENT_TYPE_FNPTR = 27;

	private const int ELEMENT_TYPE_OBJECT = 28;

	private const int ELEMENT_TYPE_SZARRAY = 29;

	private const int ELEMENT_TYPE_MVAR = 30;

	private const int ELEMENT_TYPE_CMOD_REQD = 31;

	private const int ELEMENT_TYPE_CMOD_OPT = 32;

	private const int ELEMENT_TYPE_INTERNAL = 33;

	private const int ELEMENT_TYPE_MAX = 34;

	private const int ELEMENT_TYPE_MODIFIER = 64;

	private const int ELEMENT_TYPE_SENTINEL = 65;

	private const int ELEMENT_TYPE_PINNED = 69;

	public SigParser(byte[] sig, int len)
	{
		_sig = sig;
		_len = len;
		_offs = 0;
	}

	public SigParser(SigParser rhs)
	{
		_sig = rhs._sig;
		_len = rhs._len;
		_offs = rhs._offs;
	}

	public SigParser(IntPtr sig, int len)
	{
		if (len != 0)
		{
			_sig = new byte[len];
			Marshal.Copy(sig, _sig, 0, _sig.Length);
		}
		else
		{
			_sig = null;
		}
		_len = len;
		_offs = 0;
	}

	public bool IsNull()
	{
		return _sig == null;
	}

	private void CopyFrom(SigParser rhs)
	{
		_sig = rhs._sig;
		_len = rhs._len;
		_offs = rhs._offs;
	}

	private void SkipBytes(int bytes)
	{
		_offs += bytes;
		_len -= bytes;
	}

	private bool SkipInt()
	{
		int data;
		return GetData(out data);
	}

	public bool GetData(out int data)
	{
		int pDataLen = 0;
		if (UncompressData(out data, out pDataLen))
		{
			SkipBytes(pDataLen);
			return true;
		}
		return false;
	}

	private bool GetByte(out byte data)
	{
		if (_len <= 0)
		{
			data = 204;
			return false;
		}
		data = _sig[_offs];
		SkipBytes(1);
		return true;
	}

	private bool PeekByte(out byte data)
	{
		if (_len <= 0)
		{
			data = 204;
			return false;
		}
		data = _sig[_offs];
		return true;
	}

	private bool GetElemTypeSlow(out int etype)
	{
		SigParser rhs = new SigParser(this);
		if (rhs.SkipCustomModifiers() && rhs.GetByte(out var data))
		{
			etype = data;
			CopyFrom(rhs);
			return true;
		}
		etype = 0;
		return false;
	}

	public bool GetElemType(out int etype)
	{
		if (_len > 0)
		{
			byte b = _sig[_offs];
			if (b < 31)
			{
				etype = b;
				SkipBytes(1);
				return true;
			}
		}
		return GetElemTypeSlow(out etype);
	}

	public bool PeekCallingConvInfo(out int data)
	{
		return PeekByte(out data);
	}

	public bool GetCallingConvInfo(out int data)
	{
		if (PeekByte(out data))
		{
			SkipBytes(1);
			return true;
		}
		return false;
	}

	private bool GetCallingConv(out int data)
	{
		if (GetCallingConvInfo(out data))
		{
			data &= 15;
			return true;
		}
		return false;
	}

	private bool PeekData(out int data)
	{
		int pDataLen;
		return UncompressData(out data, out pDataLen);
	}

	private bool PeekElemTypeSlow(out int etype)
	{
		return new SigParser(this).GetElemType(out etype);
	}

	public bool PeekElemType(out int etype)
	{
		if (_len > 0)
		{
			byte b = _sig[_offs];
			if (b < 31)
			{
				etype = b;
				return true;
			}
		}
		return PeekElemTypeSlow(out etype);
	}

	private bool PeekElemTypeSize(out int pSize)
	{
		pSize = 0;
		SigParser sigParser = new SigParser(this);
		if (!sigParser.SkipAnyVASentinel())
		{
			return false;
		}
		byte data = 0;
		if (!sigParser.GetByte(out data))
		{
			return false;
		}
		switch (data)
		{
		case 10:
		case 11:
		case 13:
			pSize = 8;
			break;
		case 8:
		case 9:
		case 12:
			pSize = 4;
			break;
		case 3:
		case 6:
		case 7:
			pSize = 2;
			break;
		case 2:
		case 4:
		case 5:
			pSize = 1;
			break;
		case 14:
		case 15:
		case 16:
		case 18:
		case 20:
		case 22:
		case 24:
		case 25:
		case 27:
		case 28:
		case 29:
			pSize = IntPtr.Size;
			break;
		case 0:
		case 17:
		case 31:
		case 32:
			return false;
		default:
			return false;
		case 1:
			break;
		}
		return true;
	}

	private bool AtSentinel()
	{
		if (_len > 0)
		{
			return _sig[_offs] == 65;
		}
		return false;
	}

	private bool GetToken(out int token)
	{
		if (UncompressToken(out token, out var size))
		{
			SkipBytes(size);
			return true;
		}
		return false;
	}

	public bool SkipCustomModifiers()
	{
		SigParser rhs = new SigParser(this);
		if (!rhs.SkipAnyVASentinel())
		{
			return false;
		}
		byte data = 0;
		if (!rhs.PeekByte(out data))
		{
			return false;
		}
		while (31 == data || 32 == data)
		{
			rhs.SkipBytes(1);
			if (!rhs.GetToken(out var _))
			{
				return false;
			}
			if (!rhs.PeekByte(out data))
			{
				return false;
			}
		}
		if (data >= 34 && data != 69)
		{
			return false;
		}
		CopyFrom(rhs);
		return true;
	}

	private bool SkipFunkyAndCustomModifiers()
	{
		SigParser rhs = new SigParser(this);
		if (!rhs.SkipAnyVASentinel())
		{
			return false;
		}
		byte data = 0;
		if (!rhs.PeekByte(out data))
		{
			return false;
		}
		while (31 == data || 32 == data || 64 == data || 69 == data)
		{
			rhs.SkipBytes(1);
			if (!rhs.GetToken(out var _))
			{
				return false;
			}
			if (!rhs.PeekByte(out data))
			{
				return false;
			}
		}
		if (data >= 34 && data != 69)
		{
			return false;
		}
		CopyFrom(rhs);
		return true;
	}

	private bool SkipAnyVASentinel()
	{
		byte data = 0;
		if (!PeekByte(out data))
		{
			return false;
		}
		if (data == 65)
		{
			SkipBytes(1);
		}
		return true;
	}

	public bool SkipExactlyOne()
	{
		if (!GetElemType(out var etype))
		{
			return false;
		}
		if (!ClrRuntime.IsPrimitive((ClrElementType)etype))
		{
			int token;
			switch (etype)
			{
			default:
				return false;
			case 19:
			case 30:
				if (!GetData(out token))
				{
					return false;
				}
				break;
			case 15:
			case 16:
			case 29:
			case 69:
				if (!SkipExactlyOne())
				{
					return false;
				}
				break;
			case 17:
			case 18:
				if (!GetToken(out token))
				{
					return false;
				}
				break;
			case 27:
				if (!SkipSignature())
				{
					return false;
				}
				break;
			case 20:
			{
				if (!SkipExactlyOne())
				{
					return false;
				}
				if (!GetData(out var data2))
				{
					return false;
				}
				if (data2 <= 0)
				{
					break;
				}
				if (!GetData(out var data3))
				{
					return false;
				}
				while (data3-- != 0)
				{
					if (!GetData(out token))
					{
						return false;
					}
				}
				if (!GetData(out var data4))
				{
					return false;
				}
				while (data4-- != 0)
				{
					if (!GetData(out token))
					{
						return false;
					}
				}
				break;
			}
			case 33:
				if (!GetData(out token))
				{
					return false;
				}
				break;
			case 21:
			{
				if (!SkipExactlyOne())
				{
					return false;
				}
				if (!GetData(out var data))
				{
					return false;
				}
				while (data-- != 0)
				{
					SkipExactlyOne();
				}
				break;
			}
			case 14:
			case 22:
			case 28:
			case 65:
				break;
			}
		}
		return true;
	}

	private bool SkipMethodHeaderSignature(out int pcArgs)
	{
		pcArgs = 0;
		if (!GetCallingConvInfo(out var data))
		{
			return false;
		}
		if (data == 6 || data == 7)
		{
			return false;
		}
		if ((data & 0x10) == 16 && !GetData(out var _))
		{
			return false;
		}
		if (!GetData(out pcArgs))
		{
			return false;
		}
		if (!SkipExactlyOne())
		{
			return false;
		}
		return true;
	}

	private bool SkipSignature()
	{
		if (!SkipMethodHeaderSignature(out var pcArgs))
		{
			return false;
		}
		while (pcArgs-- > 0)
		{
			if (!SkipExactlyOne())
			{
				return false;
			}
		}
		return false;
	}

	private bool UncompressToken(out int token, out int size)
	{
		if (!UncompressData(out token, out size))
		{
			return false;
		}
		int num = s_tkCorEncodeToken[token & 3];
		token = (token >> 2) | num;
		return true;
	}

	private byte GetSig(int offs)
	{
		return _sig[_offs + offs];
	}

	private bool UncompressData(out int pDataOut, out int pDataLen)
	{
		pDataOut = 0;
		pDataLen = 0;
		if (_len <= 0)
		{
			return false;
		}
		byte sig = GetSig(0);
		if ((sig & 0x80) == 0)
		{
			if (_len < 1)
			{
				return false;
			}
			pDataOut = sig;
			pDataLen = 1;
		}
		else if ((sig & 0xC0) == 128)
		{
			if (_len < 2)
			{
				return false;
			}
			pDataOut = ((sig & 0x3F) << 8) | GetSig(1);
			pDataLen = 2;
		}
		else
		{
			if ((sig & 0xE0) != 192)
			{
				return false;
			}
			if (_len < 4)
			{
				return false;
			}
			pDataOut = ((sig & 0x1F) << 24) | (GetSig(1) << 16) | (GetSig(2) << 8) | GetSig(3);
			pDataLen = 4;
		}
		return true;
	}

	private bool PeekByte(out int data)
	{
		if (!PeekByte(out byte data2))
		{
			data = 204;
			return false;
		}
		data = data2;
		return true;
	}
}
