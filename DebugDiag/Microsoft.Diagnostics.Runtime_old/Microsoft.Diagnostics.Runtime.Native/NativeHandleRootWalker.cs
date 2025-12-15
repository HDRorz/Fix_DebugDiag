using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeHandleRootWalker
{
	private ClrHeap _heap;

	private ClrAppDomain _domain;

	private bool _dependentSupport;

	public List<ClrRoot> Roots { get; set; }

	public NativeHandleRootWalker(NativeRuntime runtime, bool dependentHandleSupport)
	{
		_heap = runtime.GetHeap();
		_domain = runtime.GetRhAppDomain();
		_dependentSupport = dependentHandleSupport;
	}

	public void RootCallback(IntPtr ptr, ulong addr, ulong obj, int hndType, uint refCount, int strong)
	{
		bool flag = hndType == 6;
		if ((!flag || !_dependentSupport) && strong == 0)
		{
			return;
		}
		if (Roots == null)
		{
			Roots = new List<ClrRoot>(128);
		}
		string name = Enum.GetName(typeof(HandleType), hndType) + " handle";
		if (flag)
		{
			ulong dependentTarget = obj;
			if (!_heap.ReadPointer(addr, out obj))
			{
				obj = 0uL;
			}
			ClrType objectType = _heap.GetObjectType(obj);
			Roots.Add(new NativeHandleRoot(addr, obj, dependentTarget, objectType, hndType, _domain, name));
		}
		else
		{
			ClrType objectType2 = _heap.GetObjectType(obj);
			Roots.Add(new NativeHandleRoot(addr, obj, objectType2, hndType, _domain, name));
		}
	}
}
