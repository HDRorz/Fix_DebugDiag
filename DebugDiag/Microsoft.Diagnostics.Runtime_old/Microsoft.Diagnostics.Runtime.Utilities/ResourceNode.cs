using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class ResourceNode
{
	private PEFile _file;

	private int _nodeFileOffset;

	private List<ResourceNode> _children;

	private bool _isTop;

	private int _dataLen;

	private int _dataFileOffset;

	public string Name { get; private set; }

	public bool IsLeaf { get; private set; }

	public int DataLength => _dataLen;

	public unsafe List<ResourceNode> Children
	{
		get
		{
			if (_children == null && !IsLeaf)
			{
				PEBuffer pEBuffer = _file.AllocBuff();
				int fileOffsetOfResources = _file.Header.FileOffsetOfResources;
				IMAGE_RESOURCE_DIRECTORY* ptr = (IMAGE_RESOURCE_DIRECTORY*)pEBuffer.Fetch(_nodeFileOffset, sizeof(IMAGE_RESOURCE_DIRECTORY));
				int num = ptr->NumberOfNamedEntries + ptr->NumberOfIdEntries;
				int size = num * sizeof(IMAGE_RESOURCE_DIRECTORY_ENTRY);
				IMAGE_RESOURCE_DIRECTORY_ENTRY* ptr2 = (IMAGE_RESOURCE_DIRECTORY_ENTRY*)pEBuffer.Fetch(_nodeFileOffset + sizeof(IMAGE_RESOURCE_DIRECTORY), size);
				PEBuffer pEBuffer2 = _file.AllocBuff();
				_children = new List<ResourceNode>();
				for (int i = 0; i < num; i++)
				{
					IMAGE_RESOURCE_DIRECTORY_ENTRY* ptr3 = ptr2 + i;
					string text = null;
					text = ((!_isTop) ? ptr3->GetName(pEBuffer2, fileOffsetOfResources) : IMAGE_RESOURCE_DIRECTORY_ENTRY.GetTypeNameForTypeId(ptr3->Id));
					Children.Add(new ResourceNode(text, fileOffsetOfResources + ptr3->DataOffset, _file, ptr3->IsLeaf));
				}
				_file.FreeBuff(pEBuffer2);
				_file.FreeBuff(pEBuffer);
			}
			return _children;
		}
	}

	public unsafe byte* FetchData(int offsetInResourceData, int size, PEBuffer buff)
	{
		return buff.Fetch(_dataFileOffset + offsetInResourceData, size);
	}

	public unsafe FileVersionInfo GetFileVersionInfo()
	{
		PEBuffer pEBuffer = _file.AllocBuff();
		FileVersionInfo result = new FileVersionInfo(FetchData(0, DataLength, pEBuffer), DataLength);
		_file.FreeBuff(pEBuffer);
		return result;
	}

	public override string ToString()
	{
		StringWriter stringWriter = new StringWriter();
		ToString(stringWriter, "");
		return stringWriter.ToString();
	}

	public static ResourceNode GetChild(ResourceNode node, string name)
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
		_file = file;
		_nodeFileOffset = nodeFileOffset;
		_isTop = isTop;
		IsLeaf = isLeaf;
		Name = name;
		if (isLeaf)
		{
			PEBuffer pEBuffer = _file.AllocBuff();
			IMAGE_RESOURCE_DATA_ENTRY* ptr = (IMAGE_RESOURCE_DATA_ENTRY*)pEBuffer.Fetch(nodeFileOffset, sizeof(IMAGE_RESOURCE_DATA_ENTRY));
			_dataLen = ptr->Size;
			_dataFileOffset = file.Header.RvaToFileOffset(ptr->RvaToData);
			FetchData(0, _dataLen, pEBuffer);
			_file.FreeBuff(pEBuffer);
		}
	}
}
