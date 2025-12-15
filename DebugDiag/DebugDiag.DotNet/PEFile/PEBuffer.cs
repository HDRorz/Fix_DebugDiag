using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PEFile;

internal sealed class PEBuffer : IDisposable
{
	private int m_buffPos;

	private int m_buffLen;

	private byte[] m_buff;

	private unsafe byte* m_buffPtr;

	private GCHandle m_pinningHandle;

	private Stream m_stream;

	internal int Length => m_buffLen;

	internal PEBuffer(Stream stream, int buffSize = 512)
	{
		m_stream = stream;
		GetBuffer(buffSize);
	}

	internal unsafe byte* Fetch(int filePos, int size)
	{
		if (size > m_buff.Length)
		{
			GetBuffer(size);
		}
		if (m_buffPos > filePos || filePos + size > m_buffPos + m_buffLen)
		{
			m_buffPos = filePos;
			m_stream.Seek(m_buffPos, SeekOrigin.Begin);
			m_buffLen = 0;
			while (m_buffLen < m_buff.Length)
			{
				int num = m_stream.Read(m_buff, m_buffLen, size - m_buffLen);
				if (num == 0)
				{
					break;
				}
				m_buffLen += num;
			}
		}
		return m_buffPtr + (filePos - m_buffPos);
	}

	public void Dispose()
	{
		m_pinningHandle.Free();
	}

	private unsafe void GetBuffer(int buffSize)
	{
		m_buff = new byte[buffSize];
		fixed (byte* buff = m_buff)
		{
			m_buffPtr = buff;
		}
		m_buffLen = 0;
		m_pinningHandle = GCHandle.Alloc(m_buff, GCHandleType.Pinned);
	}
}
