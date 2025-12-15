using System;
using System.IO;

namespace PEFile;

internal class PEFile : IDisposable
{
	private PEBuffer m_headerBuff;

	private PEBuffer m_freeBuff;

	private FileStream m_stream;

	/// <summary>
	/// The Header for the PE file.  This contains the infor in a link /dump /headers 
	/// </summary>
	internal PEHeader Header { get; private set; }

	internal unsafe PEFile(string filePath)
	{
		m_stream = File.OpenRead(filePath);
		m_headerBuff = new PEBuffer(m_stream);
		Header = new PEHeader(m_headerBuff.Fetch(0, 512));
		if (Header.Size > m_headerBuff.Length)
		{
			Header = new PEHeader(m_headerBuff.Fetch(0, Header.Size));
		}
		if (Header.Size > m_headerBuff.Length)
		{
			throw new InvalidOperationException("Bad PE Header in " + filePath);
		}
	}

	/// <summary>
	/// Looks up the debug signature information in the EXE.   Returns true and sets the parameters if it is found. 
	///
	/// If 'first' is true then the first entry is returned, otherwise (by default) the last entry is used 
	/// (this is what debuggers do today).   Thus NGEN images put the IL PDB last (which means debuggers 
	/// pick up that one), but we can set it to 'first' if we want the NGEN PDB.
	/// </summary>
	internal unsafe bool GetPdbSignature(out string pdbName, out Guid pdbGuid, out int pdbAge, bool first = false)
	{
		pdbName = null;
		pdbGuid = Guid.Empty;
		pdbAge = 0;
		bool result = false;
		if (Header.DebugDirectory.VirtualAddress != 0)
		{
			PEBuffer buffer = AllocBuff();
			IMAGE_DEBUG_DIRECTORY* ptr = (IMAGE_DEBUG_DIRECTORY*)FetchRVA(Header.DebugDirectory.VirtualAddress, Header.DebugDirectory.Size, buffer);
			int num = Header.DebugDirectory.Size / sizeof(IMAGE_DEBUG_DIRECTORY);
			for (int i = 0; i < num; i++)
			{
				if (ptr[i].Type != IMAGE_DEBUG_TYPE.CODEVIEW)
				{
					continue;
				}
				PEBuffer pEBuffer = AllocBuff();
				CV_INFO_PDB70* ptr2 = (CV_INFO_PDB70*)pEBuffer.Fetch(ptr[i].PointerToRawData, ptr[i].SizeOfData);
				if (ptr2->CvSignature == 1396986706)
				{
					pdbGuid = ptr2->Signature;
					pdbAge = ptr2->Age;
					pdbName = ptr2->PdbFileName;
					result = true;
					if (first)
					{
						break;
					}
				}
				FreeBuff(pEBuffer);
			}
			FreeBuff(buffer);
		}
		return result;
	}

	internal unsafe ResourceNode GetResources()
	{
		if (Header.ResourceDirectory.VirtualAddress == 0 || Header.ResourceDirectory.Size < sizeof(IMAGE_RESOURCE_DIRECTORY))
		{
			return null;
		}
		return new ResourceNode("", Header.FileOffsetOfResources, this, isLeaf: false, isTop: true);
	}

	internal unsafe string GetRT_MANIFEST()
	{
		ResourceNode resourceNode = ResourceNode.GetChild(ResourceNode.GetChild(GetResources(), "RT_MANIFEST"), "1");
		if (resourceNode == null)
		{
			return null;
		}
		if (!resourceNode.IsLeaf && resourceNode.Children.Count == 1)
		{
			resourceNode = resourceNode.Children[0];
		}
		PEBuffer pEBuffer = AllocBuff();
		byte* pointer = resourceNode.FetchData(0, resourceNode.DataLength, pEBuffer);
		string result = null;
		using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream(pointer, resourceNode.DataLength))
		{
			using StreamReader streamReader = new StreamReader(stream);
			result = streamReader.ReadToEnd();
		}
		FreeBuff(pEBuffer);
		return result;
	}

	internal unsafe FileVersionInfo GetFileVersionInfo()
	{
		ResourceNode resourceNode = ResourceNode.GetChild(ResourceNode.GetChild(GetResources(), "Version"), "1");
		if (resourceNode == null)
		{
			return null;
		}
		if (!resourceNode.IsLeaf && resourceNode.Children.Count == 1)
		{
			resourceNode = resourceNode.Children[0];
		}
		PEBuffer pEBuffer = AllocBuff();
		FileVersionInfo result = new FileVersionInfo(resourceNode.FetchData(0, resourceNode.DataLength, pEBuffer), resourceNode.DataLength);
		FreeBuff(pEBuffer);
		return result;
	}

	public void Dispose()
	{
		m_stream.Close();
		m_headerBuff.Dispose();
		if (m_freeBuff != null)
		{
			m_freeBuff.Dispose();
		}
	}

	internal unsafe byte* FetchRVA(int rva, int size, PEBuffer buffer)
	{
		return buffer.Fetch(Header.RvaToFileOffset(rva), size);
	}

	internal PEBuffer AllocBuff()
	{
		PEBuffer freeBuff = m_freeBuff;
		if (freeBuff == null)
		{
			return new PEBuffer(m_stream);
		}
		m_freeBuff = null;
		return freeBuff;
	}

	internal void FreeBuff(PEBuffer buffer)
	{
		m_freeBuff = buffer;
	}
}
