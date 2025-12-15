using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeStackRootWalker
{
	private ClrHeap _heap;

	private ClrAppDomain _domain;

	private ClrThread _thread;

	public List<ClrRoot> Roots { get; set; }

	public NativeStackRootWalker(ClrHeap heap, ClrAppDomain domain, ClrThread thread)
	{
		_heap = heap;
		_domain = domain;
		_thread = thread;
		Roots = new List<ClrRoot>();
	}

	public void Callback(IntPtr token, ulong symbol, ulong addr, ulong obj, int pinned, int interior)
	{
		string name = "local variable";
		NativeStackRoot item = new NativeStackRoot(_thread, addr, obj, name, _heap.GetObjectType(obj), _domain, pinned != 0, interior != 0);
		Roots.Add(item);
	}
}
