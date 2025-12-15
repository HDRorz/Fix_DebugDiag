using System.Collections.Generic;
using System.IO;

namespace PEFile;

internal class ResourceNode
{
	private PEFile m_file;

	private int m_nodeFileOffset;

	private List<ResourceNode> m_Children;

	private bool m_isTop;

	private int m_dataLen;

	private int m_dataFileOffset;

	internal string Name { get; private set; }

	internal bool IsLeaf { get; private set; }

	internal int DataLength => m_dataLen;

	internal unsafe List<ResourceNode> Children
	{
		get
		{
			if (m_Children == null && !IsLeaf)
			{
				PEBuffer pEBuffer = m_file.AllocBuff();
				int fileOffsetOfResources = m_file.Header.FileOffsetOfResources;
				IMAGE_RESOURCE_DIRECTORY* ptr = (IMAGE_RESOURCE_DIRECTORY*)pEBuffer.Fetch(m_nodeFileOffset, sizeof(IMAGE_RESOURCE_DIRECTORY));
				int num = ptr->NumberOfNamedEntries + ptr->NumberOfIdEntries;
				int size = num * sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY);
				IMAGE_RESOURCE_DIRECTORY_ENTRY* ptr2 = (IMAGE_RESOURCE_DIRECTORY_ENTRY*)pEBuffer.Fetch(m_nodeFileOffset + sizeof(IMAGE_RESOURCE_DIRECTORY), size);
				PEBuffer pEBuffer2 = m_file.AllocBuff();
				m_Children = new List<ResourceNode>();
				for (int i = 0; i < num; i++)
				{
					IMAGE_RESOURCE_DIRECTORY_ENTRY* ptr3 = ptr2 + i;
					string text = null;
					text = ((!m_isTop) ? ptr3->GetName(pEBuffer2, fileOffsetOfResources) : IMAGE_RESOURCE_DIRECTORY_ENTRY.GetTypeNameForTypeId(ptr3->Id));
					Children.Add(new ResourceNode(text, fileOffsetOfResources + ptr3->DataOffset, m_file, ptr3->IsLeaf));
				}
				m_file.FreeBuff(pEBuffer2);
				m_file.FreeBuff(pEBuffer);
			}
			return m_Children;
		}
	}

	internal unsafe byte* FetchData(int offsetInResourceData, int size, PEBuffer buff)
	{
		return buff.Fetch(m_dataFileOffset + offsetInResourceData, size);
	}

	internal unsafe FileVersionInfo GetFileVersionInfo()
	{
		PEBuffer pEBuffer = m_file.AllocBuff();
		FileVersionInfo result = new FileVersionInfo(FetchData(0, DataLength, pEBuffer), DataLength);
		m_file.FreeBuff(pEBuffer);
		return result;
	}

	public override string ToString()
	{
		StringWriter stringWriter = new StringWriter();
		ToString(stringWriter, "");
		return stringWriter.ToString();
	}

	internal static ResourceNode GetChild(ResourceNode node, string name)
	{
		if (node == null)
		{
			return null;
		}
		foreach (ResourceNode child in node.Children)
		{
			if (child.Name == name)
			{
				return child;
			}
		}
		return null;
	}

	private void ToString(StringWriter sw, string indent)
	{
		sw.Write("{0}<ResourceNode", indent);
		sw.Write(" Name=\"{0}\"", Name);
		sw.Write(" IsLeaf=\"{0}\"", IsLeaf);
		if (IsLeaf)
		{
			sw.Write("DataLength=\"{0}\"", DataLength);
			sw.WriteLine("/>");
			return;
		}
		sw.Write("ChildCount=\"{0}\"", Children.Count);
		sw.WriteLine(">");
		foreach (ResourceNode child in Children)
		{
			child.ToString(sw, indent + "  ");
		}
		sw.WriteLine("{0}</ResourceNode>", indent);
	}

	internal unsafe ResourceNode(string name, int nodeFileOffset, PEFile file, bool isLeaf, bool isTop = false)
	{
		m_file = file;
		m_nodeFileOffset = nodeFileOffset;
		m_isTop = isTop;
		IsLeaf = isLeaf;
		Name = name;
		if (isLeaf)
		{
			PEBuffer pEBuffer = m_file.AllocBuff();
			IMAGE_RESOURCE_DATA_ENTRY* ptr = (IMAGE_RESOURCE_DATA_ENTRY*)pEBuffer.Fetch(nodeFileOffset, sizeof(IMAGE_RESOURCE_DATA_ENTRY));
			m_dataLen = ptr->Size;
			m_dataFileOffset = file.Header.RvaToFileOffset(ptr->RvaToData);
			FetchData(0, m_dataLen, pEBuffer);
			m_file.FreeBuff(pEBuffer);
		}
	}
}
