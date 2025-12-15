using System;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

[Obsolete("Use PEImage instead.")]
public sealed class PEFile : IDisposable
{
	private PdbInfo _pdb;

	private PEBuffer _headerBuff;

	private PEBuffer _freeBuff;

	private Stream _stream;

	private bool _virt;

	public PEHeader Header { get; private set; }

	public PdbInfo PdbInfo
	{
		get
		{
			if (_pdb == null && GetPdbSignature(out var pdbName, out var pdbGuid, out var pdbAge))
			{
				_pdb = new PdbInfo(pdbName, pdbGuid, pdbAge);
			}
			return _pdb;
		}
	}

	public bool Disposed { get; private set; }

	public static PEFile TryLoad(Stream stream, bool virt)
	{
		PEBuffer buffer = new PEBuffer(stream);
		PEHeader pEHeader = PEHeader.FromBuffer(buffer, virt);
		if (pEHeader == null)
		{
			return null;
		}
		PEFile pEFile = new PEFile();
		pEFile.Init(stream, "stream", virt, buffer, pEHeader);
		return pEFile;
	}

	private PEFile()
	{
	}

	public PEFile(string filePath)
	{
		Init(File.OpenRead(filePath), filePath, virt: false);
	}

	public PEFile(Stream stream, bool virt)
	{
		Init(stream, "stream", virt);
	}

	private void Init(Stream stream, string filePath, bool virt, PEBuffer buffer = null, PEHeader header = null)
	{
		if (buffer == null)
		{
			buffer = new PEBuffer(stream);
		}
		if (header == null)
		{
			header = PEHeader.FromBuffer(buffer, virt);
		}
		_virt = virt;
		_stream = stream;
		_headerBuff = buffer;
		Header = header;
		if (header != null && header.PEHeaderSize > _headerBuff.Length)
		{
			throw new InvalidOperationException("Bad PE Header in " + filePath);
		}
	}

	public unsafe bool GetPdbSignature(out string pdbName, out Guid pdbGuid, out int pdbAge, bool first = false)
	{
		pdbName = null;
		pdbGuid = Guid.Empty;
		pdbAge = 0;
		bool result = false;
		if (Header == null)
		{
			return false;
		}
		if (Header.DebugDirectory.VirtualAddress != 0)
		{
			PEBuffer buffer = AllocBuff();
			IMAGE_DEBUG_DIRECTORY* ptr = (IMAGE_DEBUG_DIRECTORY*)FetchRVA((int)Header.DebugDirectory.VirtualAddress, (int)Header.DebugDirectory.Size, buffer);
			if (Header.DebugDirectory.Size % sizeof(IMAGE_DEBUG_DIRECTORY) != 0L)
			{
				return false;
			}
			int num = (int)Header.DebugDirectory.Size / sizeof(IMAGE_DEBUG_DIRECTORY);
			for (int i = 0; i < num; i++)
			{
				if (ptr[i].Type != IMAGE_DEBUG_TYPE.CODEVIEW)
				{
					continue;
				}
				PEBuffer pEBuffer = AllocBuff();
				int filePos = (_virt ? ptr[i].AddressOfRawData : ptr[i].PointerToRawData);
				CV_INFO_PDB70* ptr2 = (CV_INFO_PDB70*)pEBuffer.Fetch(filePos, ptr[i].SizeOfData);
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

	internal static bool TryGetIndexProperties(string filename, out int timestamp, out int filesize)
	{
		try
		{
			using PEFile pEFile = new PEFile(filename);
			PEHeader header = pEFile.Header;
			timestamp = header.TimeDateStampSec;
			filesize = (int)header.SizeOfImage;
			return true;
		}
		catch
		{
			timestamp = 0;
			filesize = 0;
			return false;
		}
	}

	internal static bool TryGetIndexProperties(Stream stream, bool virt, out int timestamp, out int filesize)
	{
		try
		{
			using PEFile pEFile = new PEFile(stream, virt);
			PEHeader header = pEFile.Header;
			timestamp = header.TimeDateStampSec;
			filesize = (int)header.SizeOfImage;
			return true;
		}
		catch
		{
			timestamp = 0;
			filesize = 0;
			return false;
		}
	}

	public unsafe FileVersionInfo GetFileVersionInfo()
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

	public unsafe string GetSxSManfest()
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

	public void Dispose()
	{
		_stream.Dispose();
		_headerBuff.Dispose();
		if (_freeBuff != null)
		{
			_freeBuff.Dispose();
		}
		Disposed = true;
	}

	internal unsafe ResourceNode GetResources()
	{
		if (Header.ResourceDirectory.VirtualAddress == 0 || Header.ResourceDirectory.Size < sizeof(IMAGE_RESOURCE_DIRECTORY))
		{
			return null;
		}
		return new ResourceNode("", Header.FileOffsetOfResources, this, isLeaf: false, isTop: true);
	}

	internal unsafe byte* FetchRVA(int rva, int size, PEBuffer buffer)
	{
		int filePos = Header.RvaToFileOffset(rva);
		return buffer.Fetch(filePos, size);
	}

	internal unsafe IntPtr SafeFetchRVA(int rva, int size, PEBuffer buffer)
	{
		return new IntPtr(FetchRVA(rva, size, buffer));
	}

	internal PEBuffer AllocBuff()
	{
		PEBuffer freeBuff = _freeBuff;
		if (freeBuff == null)
		{
			return new PEBuffer(_stream);
		}
		_freeBuff = null;
		return freeBuff;
	}

	internal void FreeBuff(PEBuffer buffer)
	{
		_freeBuff = buffer;
	}
}
