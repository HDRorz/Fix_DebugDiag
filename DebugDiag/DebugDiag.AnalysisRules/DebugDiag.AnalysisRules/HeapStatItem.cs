using System;
using System.Collections.Generic;

namespace DebugDiag.AnalysisRules;

public class HeapStatItem
{
	private HeapCache heap;

	private CacheInfo info;

	public string Name { get; internal set; }

	public string TypeName { get; internal set; }

	public ulong TotalSize => info.Size;

	public ulong TotalSizeSquared => info.SizeSquared;

	public ulong Count => (ulong)info.Cache.Count;

	public ulong MinSize => info.MinSize;

	public ulong MaxSize => info.MaxSize;

	public ulong Average => info.Size / (ulong)info.Cache.Count;

	public ulong StdDeviation => (ulong)Math.Ceiling(Math.Sqrt((double)((ulong)((long)info.Cache.Count * (long)info.SizeSquared) - info.Size * info.Size) / (double)((info.Cache.Count <= 1) ? 1 : (info.Cache.Count * (info.Cache.Count - 1)))));

	public IList<ulong> Addresses => info.Cache;

	public IEnumerable<string> Interfaces
	{
		get
		{
			ulong address = Addresses[0];
			return heap.EnumerateInterfaces(address);
		}
	}

	public IEnumerable<string> InheritanceChain
	{
		get
		{
			ulong address = Addresses[0];
			return heap.InheritanceChain(address);
		}
	}

	internal HeapStatItem(string Name, HeapCache Cache)
	{
		this.Name = Name;
		heap = Cache;
		info = Cache.cache[Name];
	}

	public bool IsImplementationOf(string TypePattern)
	{
		ulong address = Addresses[0];
		return heap.IsImplementationOf(address, TypePattern);
	}

	public bool IsDerivedOf(string TypePattern)
	{
		ulong address = Addresses[0];
		return heap.IsDerivedOf(address, TypePattern);
	}

	public bool IsDerivedOrImplementationOf(string TypePattern)
	{
		ulong address = Addresses[0];
		return heap.IsDerivedOrImplementationOf(address, TypePattern);
	}
}
