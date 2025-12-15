using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class ELFVirtualAddressSpace : IAddressSpace
{
	private readonly ElfProgramHeader[] _segments;

	private readonly IAddressSpace _addressSpace;

	public string Name => _addressSpace.Name;

	public long Length { get; }

	public ELFVirtualAddressSpace(IReadOnlyList<ElfProgramHeader> segments, IAddressSpace addressSpace)
	{
		Length = segments.Max((ElfProgramHeader s) => s.VirtualAddress + s.VirtualSize);
		_segments = segments.Where((ElfProgramHeader programHeader) => programHeader.FileSize > 0).ToArray();
		_addressSpace = addressSpace;
	}

	public int Read(long position, byte[] buffer, int bufferOffset, int count)
	{
		int num = 0;
		while (num != count)
		{
			int num2 = 0;
			long virtualAddress;
			long num3;
			while (num2 < _segments.Length)
			{
				virtualAddress = _segments[num2].VirtualAddress;
				long virtualSize = _segments[num2].VirtualSize;
				num3 = virtualAddress + virtualSize;
				if (virtualAddress > position || position >= num3)
				{
					num2++;
					continue;
				}
				goto IL_0035;
			}
			goto IL_008f;
			IL_0035:
			int count2 = (int)Math.Min(count - num, num3 - position);
			long position2 = position - virtualAddress;
			int num4 = _segments[num2].AddressSpace.Read(position2, buffer, bufferOffset, count2);
			if (num4 == 0)
			{
				break;
			}
			position += num4;
			bufferOffset += num4;
			num += num4;
			goto IL_008f;
			IL_008f:
			if (num2 == _segments.Length)
			{
				break;
			}
		}
		Array.Clear(buffer, bufferOffset, count - num);
		return num;
	}
}
