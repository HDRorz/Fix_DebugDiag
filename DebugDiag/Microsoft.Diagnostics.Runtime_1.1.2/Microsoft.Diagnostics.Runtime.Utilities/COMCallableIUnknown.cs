using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class COMCallableIUnknown : COMHelper
{
	private readonly GCHandle _handle;

	private int _refCount;

	private readonly Dictionary<Guid, IntPtr> _interfaces = new Dictionary<Guid, IntPtr>();

	private readonly List<Delegate> _delegates = new List<Delegate>();

	public IntPtr IUnknownObject { get; }

	public unsafe IUnknownVTable IUnknown => *(*(IUnknownVTable**)(void*)IUnknownObject);

	public unsafe COMCallableIUnknown()
	{
		_handle = GCHandle.Alloc(this);
		IUnknownVTable* ptr = (IUnknownVTable*)Marshal.AllocHGlobal(sizeof(IUnknownVTable)).ToPointer();
		QueryInterfaceDelegate queryInterfaceDelegate = QueryInterfaceImpl;
		ptr->QueryInterface = Marshal.GetFunctionPointerForDelegate((Delegate)queryInterfaceDelegate);
		_delegates.Add(queryInterfaceDelegate);
		AddRefDelegate addRefDelegate = AddRefImpl;
		ptr->AddRef = Marshal.GetFunctionPointerForDelegate((Delegate)addRefDelegate);
		_delegates.Add(addRefDelegate);
		ReleaseDelegate releaseDelegate = ReleaseImpl;
		ptr->Release = Marshal.GetFunctionPointerForDelegate((Delegate)releaseDelegate);
		_delegates.Add(releaseDelegate);
		IUnknownObject = Marshal.AllocHGlobal(IntPtr.Size);
		*(IUnknownVTable**)(void*)IUnknownObject = ptr;
		_interfaces.Add(IUnknownGuid, IUnknownObject);
	}

	public int AddRef()
	{
		return AddRefImpl(IUnknownObject);
	}

	public int Release()
	{
		return ReleaseImpl(IUnknownObject);
	}

	public VTableBuilder AddInterface(Guid guid, bool validate)
	{
		return new VTableBuilder(this, guid, validate);
	}

	internal void RegisterInterface(Guid guid, IntPtr clsPtr, List<Delegate> keepAlive)
	{
		_interfaces.Add(guid, clsPtr);
		_delegates.AddRange(keepAlive);
	}

	private int QueryInterfaceImpl(IntPtr self, ref Guid guid, out IntPtr ptr)
	{
		if (_interfaces.TryGetValue(guid, out var value))
		{
			Interlocked.Increment(ref _refCount);
			ptr = value;
			return 0;
		}
		ptr = IntPtr.Zero;
		return -2147467262;
	}

	private unsafe int ReleaseImpl(IntPtr self)
	{
		int num = Interlocked.Decrement(ref _refCount);
		if (num <= 0 && _handle.IsAllocated)
		{
			foreach (IntPtr value in _interfaces.Values)
			{
				IntPtr* ptr = (IntPtr*)(void*)value;
				Marshal.FreeHGlobal(*ptr);
				Marshal.FreeHGlobal(value);
			}
			_handle.Free();
			_interfaces.Clear();
			_delegates.Clear();
		}
		return num;
	}

	private int AddRefImpl(IntPtr self)
	{
		return Interlocked.Increment(ref _refCount);
	}
}
