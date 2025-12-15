using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeStaticRootWalker
{
	private NativeRuntime _runtime;

	private ClrHeap _heap;

	public List<ClrRoot> Roots { get; set; }

	public NativeStaticRootWalker(NativeRuntime runtime, bool resolveStatics)
	{
		Roots = new List<ClrRoot>(128);
		_runtime = (resolveStatics ? runtime : null);
		_heap = _runtime.GetHeap();
	}

	public void Callback(IntPtr token, ulong addr, ulong obj, int pinned, int interior)
	{
		string text = (text = _runtime.ResolveSymbol(addr));
		if (text == null)
		{
			text = addr.ToString("X");
		}
		else
		{
			int num = text.IndexOf('!');
			if (num >= 0)
			{
				text = text.Substring(num + 1);
			}
			text = $"{addr:X}: {text}";
		}
		text = "static var " + text;
		ClrType objectType = _heap.GetObjectType(obj);
		Roots.Add(new NativeStaticVar(_runtime, addr, obj, objectType, text, interior != 0, pinned != 0));
	}
}
