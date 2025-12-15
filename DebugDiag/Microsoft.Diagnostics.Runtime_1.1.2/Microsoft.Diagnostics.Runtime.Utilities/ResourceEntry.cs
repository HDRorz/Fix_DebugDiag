using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class ResourceEntry
{
	private const int MaxPath = 1024;

	private static readonly ResourceEntry[] s_emptyChildren = new ResourceEntry[0];

	private ResourceEntry[] _children;

	private readonly int _offset;

	public PEImage Image { get; }

	public ResourceEntry Parent { get; }

	public string Name { get; }

	public bool IsLeaf { get; }

	public int Size
	{
		get
		{
			GetDataVaAndSize(out var _, out var size);
			return size;
		}
	}

	public int Count => Children.Count;

	public ResourceEntry this[int i] => Children[i];

	public ResourceEntry this[string name] => Children.SingleOrDefault((ResourceEntry c) => c.Name == name);

	public IReadOnlyList<ResourceEntry> Children => GetChildren();

	internal ResourceEntry(PEImage image, ResourceEntry parent, string name, bool leaf, int offset)
	{
		Image = image;
		Parent = parent;
		Name = name;
		IsLeaf = leaf;
		_offset = offset;
	}

	public byte[] GetData()
	{
		GetDataVaAndSize(out var va, out var size);
		if (size == 0 || va == 0)
		{
			return new byte[0];
		}
		byte[] array = new byte[size];
		int num = Image.Read(array, va, size);
		if (num < size)
		{
			Array.Resize(ref array, num);
		}
		return array;
	}

	public T GetData<T>(int offset = 0) where T : struct
	{
		byte[] data = GetData();
		if (Marshal.SizeOf(typeof(T)) + offset > data.Length)
		{
			throw new IndexOutOfRangeException();
		}
		GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		T result = (T)Marshal.PtrToStructure(gCHandle.AddrOfPinnedObject(), typeof(T));
		gCHandle.Free();
		return result;
	}

	private ResourceEntry[] GetChildren()
	{
		if (_children != null)
		{
			return _children;
		}
		if (IsLeaf)
		{
			return _children = s_emptyChildren;
		}
		int offset = Image.Resources._offset;
		int offset2 = _offset;
		IMAGE_RESOURCE_DIRECTORY iMAGE_RESOURCE_DIRECTORY = Image.Read<IMAGE_RESOURCE_DIRECTORY>(ref offset2);
		int num = iMAGE_RESOURCE_DIRECTORY.NumberOfNamedEntries + iMAGE_RESOURCE_DIRECTORY.NumberOfIdEntries;
		ResourceEntry[] array = new ResourceEntry[num];
		for (int i = 0; i < num; i++)
		{
			IMAGE_RESOURCE_DIRECTORY_ENTRY entry = Image.Read<IMAGE_RESOURCE_DIRECTORY_ENTRY>(ref offset2);
			array[i] = new ResourceEntry(name: (!entry.IsStringName) ? IMAGE_RESOURCE_DIRECTORY_ENTRY.GetTypeNameForTypeId(entry.Id) : GetName(ref entry, offset), image: Image, parent: this, leaf: entry.IsLeaf, offset: offset + entry.DataOffset);
		}
		return _children = array;
	}

	private string GetName(ref IMAGE_RESOURCE_DIRECTORY_ENTRY entry, int resourceStartFileOffset)
	{
		int offset = resourceStartFileOffset + entry.NameOffset;
		int num = Image.Read<ushort>(ref offset);
		StringBuilder stringBuilder = new StringBuilder(num);
		for (int i = 0; i < num; i++)
		{
			char c = (char)Image.Read<ushort>(ref offset);
			if (c == '\0' || i > 1024)
			{
				break;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	private void GetDataVaAndSize(out int va, out int size)
	{
		IMAGE_RESOURCE_DATA_ENTRY iMAGE_RESOURCE_DATA_ENTRY = Image.Read<IMAGE_RESOURCE_DATA_ENTRY>(_offset);
		va = iMAGE_RESOURCE_DATA_ENTRY.RvaToData;
		size = iMAGE_RESOURCE_DATA_ENTRY.Size;
	}

	public override string ToString()
	{
		return Name;
	}
}
