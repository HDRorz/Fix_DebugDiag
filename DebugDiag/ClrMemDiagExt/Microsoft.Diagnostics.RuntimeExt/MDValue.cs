using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDValue : IMDValue
{
	private object m_value;

	private ClrElementType m_cet;

	public MDValue(object value, ClrElementType cet)
	{
		m_value = value;
		m_cet = cet;
		if (m_value == null)
		{
			m_cet = ClrElementType.Unknown;
		}
		switch (m_cet)
		{
		case ClrElementType.Pointer:
		case ClrElementType.NativeUInt:
		case ClrElementType.FunctionPointer:
			m_cet = ClrElementType.UInt64;
			break;
		case ClrElementType.String:
			if (m_value == null)
			{
				m_cet = ClrElementType.Unknown;
			}
			break;
		case ClrElementType.Class:
		case ClrElementType.Array:
		case ClrElementType.SZArray:
			m_cet = ClrElementType.Object;
			break;
		}
	}

	public void IsNull(out int pNull)
	{
		pNull = ((m_cet == ClrElementType.Unknown || m_value == null) ? 1 : 0);
	}

	public void GetElementType(out int pCET)
	{
		pCET = (int)m_cet;
	}

	public void GetInt32(out int pValue)
	{
		ulong value = GetValue64();
		pValue = (int)value;
	}

	private ulong GetValue64()
	{
		if (m_value is int)
		{
			return (ulong)(int)m_value;
		}
		if (m_value is uint)
		{
			return (uint)m_value;
		}
		if (m_value is long)
		{
			return (ulong)(long)m_value;
		}
		return (ulong)m_value;
	}

	public void GetUInt32(out uint pValue)
	{
		ulong value = GetValue64();
		pValue = (uint)value;
	}

	public void GetInt64(out long pValue)
	{
		ulong value = GetValue64();
		pValue = (long)value;
	}

	public void GetUInt64(out ulong pValue)
	{
		ulong value = GetValue64();
		pValue = value;
	}

	public void GetString(out string pValue)
	{
		pValue = (string)m_value;
	}

	public void GetBool(out int pBool)
	{
		if (m_value is bool)
		{
			pBool = (((bool)m_value) ? 1 : 0);
		}
		else
		{
			pBool = (int)GetValue64();
		}
	}
}
