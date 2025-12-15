using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class VTableBuilder
{
	private readonly Guid _guid;

	private readonly COMCallableIUnknown _wrapper;

	private readonly bool _forceValidation;

	private readonly List<Delegate> _delegates = new List<Delegate>();

	private bool _complete;

	internal VTableBuilder(COMCallableIUnknown wrapper, Guid guid, bool forceValidation)
	{
		_guid = guid;
		_wrapper = wrapper;
		_forceValidation = forceValidation;
	}

	public void AddMethod(Delegate func, bool validate = false)
	{
		if (_complete)
		{
			throw new InvalidOperationException();
		}
		if (_forceValidation || validate)
		{
			if (func.Method.GetParameters().First().ParameterType != typeof(IntPtr))
			{
				throw new InvalidOperationException();
			}
			object[] customAttributes = func.GetType().GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), inherit: false);
			if (customAttributes.Length != 0 && ((UnmanagedFunctionPointerAttribute)customAttributes[0]).CallingConvention != CallingConvention.Winapi)
			{
				throw new InvalidOperationException();
			}
		}
		_delegates.Add(func);
	}

	public unsafe IntPtr Complete()
	{
		if (_complete)
		{
			throw new InvalidOperationException();
		}
		_complete = true;
		IntPtr intPtr = Marshal.AllocHGlobal(IntPtr.Size);
		IntPtr* ptr = (IntPtr*)(void*)Marshal.AllocHGlobal(_delegates.Count * IntPtr.Size + sizeof(IUnknownVTable));
		*(IntPtr**)(void*)intPtr = ptr;
		IUnknownVTable iUnknown = _wrapper.IUnknown;
		*(ptr++) = iUnknown.QueryInterface;
		*(ptr++) = iUnknown.AddRef;
		*(ptr++) = iUnknown.Release;
		foreach (Delegate @delegate in _delegates)
		{
			*(ptr++) = Marshal.GetFunctionPointerForDelegate(@delegate);
		}
		_wrapper.RegisterInterface(_guid, intPtr, _delegates);
		return intPtr;
	}
}
