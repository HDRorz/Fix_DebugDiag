using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class CallableCOMWrapper : COMHelper, IDisposable
{
	private bool _disposed;

	private unsafe readonly IUnknownVTable* _unknownVTable;

	private readonly RefCountedFreeLibrary _library;

	private ReleaseDelegate _release;

	private AddRefDelegate _addRef;

	protected IntPtr Self { get; }

	protected unsafe void* _vtable => _unknownVTable + 1;

	protected unsafe CallableCOMWrapper(CallableCOMWrapper toClone)
	{
		if (toClone._disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		Self = toClone.Self;
		_unknownVTable = toClone._unknownVTable;
		_library = toClone._library;
		AddRef();
		_library.AddRef();
	}

	public unsafe int AddRef()
	{
		if (_addRef == null)
		{
			_addRef = (AddRefDelegate)Marshal.GetDelegateForFunctionPointer(_unknownVTable->AddRef, typeof(AddRefDelegate));
		}
		return _addRef(Self);
	}

	protected unsafe CallableCOMWrapper(RefCountedFreeLibrary library, ref Guid desiredInterface, IntPtr pUnknown)
	{
		_library = library;
		_library.AddRef();
		IntPtr intPtr = *(IntPtr*)(void*)pUnknown;
		IntPtr ptr;
		int num = ((QueryInterfaceDelegate)Marshal.GetDelegateForFunctionPointer(((IUnknownVTable*)intPtr)->QueryInterface, typeof(QueryInterfaceDelegate)))(pUnknown, ref desiredInterface, out ptr);
		if (num != 0)
		{
			GC.SuppressFinalize(this);
			throw new InvalidCastException($"{GetType().FullName}.QueryInterface({desiredInterface}) failed, hr=0x{num:x}");
		}
		((ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(((IUnknownVTable*)intPtr)->Release, typeof(ReleaseDelegate)))(pUnknown);
		Self = ptr;
		_unknownVTable = *(IUnknownVTable**)(void*)ptr;
	}

	public unsafe int Release()
	{
		if (_release == null)
		{
			_release = (ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(_unknownVTable->Release, typeof(ReleaseDelegate));
		}
		return _release(Self);
	}

	public unsafe IntPtr QueryInterface(ref Guid riid)
	{
		if (((QueryInterfaceDelegate)Marshal.GetDelegateForFunctionPointer(_unknownVTable->QueryInterface, typeof(QueryInterfaceDelegate)))(Self, ref riid, out var ptr) != 0)
		{
			return IntPtr.Zero;
		}
		return ptr;
	}

	protected static bool SUCCEEDED(int hresult)
	{
		return hresult >= 0;
	}

	protected static void InitDelegate<T>(ref T t, IntPtr entry) where T : Delegate
	{
		if (!(t != null))
		{
			t = (T)Marshal.GetDelegateForFunctionPointer(entry, typeof(T));
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			Release();
			_library.Release();
			_disposed = true;
		}
	}

	~CallableCOMWrapper()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
