using System;

namespace Microsoft.Diagnostics.Runtime;

internal class GCDesc
{
	private static readonly int s_GCDescSize = IntPtr.Size * 2;

	private byte[] _data;

	public GCDesc(byte[] data)
	{
		_data = data;
	}

	public void WalkObject(ulong addr, ulong size, MemoryReader cache, Action<ulong, int> refCallback)
	{
		int numSeries = GetNumSeries();
		int num = GetHighestSeries();
		if (numSeries > 0)
		{
			int lowestSeries = GetLowestSeries();
			do
			{
				ulong num2 = addr + GetSeriesOffset(num);
				for (ulong num3 = (ulong)((long)num2 + (long)GetSeriesSize(num)) + size; num2 < num3; num2 += (ulong)IntPtr.Size)
				{
					if (cache.ReadPtr(num2, out var value) && value != 0L)
					{
						refCallback(value, (int)(num2 - addr));
					}
				}
				num -= s_GCDescSize;
			}
			while (num >= lowestSeries);
			return;
		}
		ulong num4 = addr + GetSeriesOffset(num);
		while (num4 < (ulong)((long)(addr + size) - (long)IntPtr.Size))
		{
			for (int num5 = 0; num5 > numSeries; num5--)
			{
				uint pointers = GetPointers(num, num5);
				uint skip = GetSkip(num, num5);
				ulong num6 = num4 + (ulong)(pointers * IntPtr.Size);
				do
				{
					if (cache.ReadPtr(num4, out var value2) && value2 != 0L)
					{
						refCallback(value2, (int)(num4 - addr));
					}
					num4 += (ulong)IntPtr.Size;
				}
				while (num4 < num6);
				num4 += skip;
			}
		}
	}

	private uint GetPointers(int curr, int i)
	{
		int num = i * IntPtr.Size;
		if (IntPtr.Size == 4)
		{
			return BitConverter.ToUInt16(_data, curr + num);
		}
		return BitConverter.ToUInt32(_data, curr + num);
	}

	private uint GetSkip(int curr, int i)
	{
		int num = i * IntPtr.Size + IntPtr.Size / 2;
		if (IntPtr.Size == 4)
		{
			return BitConverter.ToUInt16(_data, curr + num);
		}
		return BitConverter.ToUInt32(_data, curr + num);
	}

	private int GetSeriesSize(int curr)
	{
		if (IntPtr.Size == 4)
		{
			return BitConverter.ToInt32(_data, curr);
		}
		return (int)BitConverter.ToInt64(_data, curr);
	}

	private ulong GetSeriesOffset(int curr)
	{
		if (IntPtr.Size == 4)
		{
			return BitConverter.ToUInt32(_data, curr + IntPtr.Size);
		}
		return BitConverter.ToUInt64(_data, curr + IntPtr.Size);
	}

	private int GetHighestSeries()
	{
		return _data.Length - IntPtr.Size * 3;
	}

	private int GetLowestSeries()
	{
		return _data.Length - ComputeSize(GetNumSeries());
	}

	private static int ComputeSize(int series)
	{
		return IntPtr.Size + series * IntPtr.Size * 2;
	}

	private int GetNumSeries()
	{
		if (IntPtr.Size == 4)
		{
			return BitConverter.ToInt32(_data, _data.Length - IntPtr.Size);
		}
		return (int)BitConverter.ToInt64(_data, _data.Length - IntPtr.Size);
	}
}
