using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopException : ClrException
{
	private ulong _object;

	private BaseDesktopHeapType _type;

	private IList<ClrStackFrame> _stackTrace;

	public override ClrType Type => _type;

	public override string Message
	{
		get
		{
			ClrInstanceField fieldByName = _type.GetFieldByName("_message");
			if (fieldByName != null)
			{
				return (string)fieldByName.GetValue(_object);
			}
			DesktopRuntimeBase desktopRuntime = _type.DesktopHeap.DesktopRuntime;
			uint exceptionMessageOffset = desktopRuntime.GetExceptionMessageOffset();
			ulong value = _object + exceptionMessageOffset;
			if (!desktopRuntime.ReadPointer(value, out value))
			{
				return null;
			}
			return _type.DesktopHeap.GetStringContents(value);
		}
	}

	public override ulong Address => _object;

	public override ClrException Inner
	{
		get
		{
			ClrInstanceField fieldByName = _type.GetFieldByName("_innerException");
			if (fieldByName == null)
			{
				return null;
			}
			object value = fieldByName.GetValue(_object);
			if (value == null || !(value is ulong) || (ulong)value == 0L)
			{
				return null;
			}
			ulong objRef = (ulong)value;
			BaseDesktopHeapType type = (BaseDesktopHeapType)_type.DesktopHeap.GetObjectType(objRef);
			return new DesktopException(objRef, type);
		}
	}

	public override IList<ClrStackFrame> StackTrace
	{
		get
		{
			if (_stackTrace == null)
			{
				_stackTrace = _type.DesktopHeap.DesktopRuntime.GetExceptionStackTrace(_object, _type);
			}
			return _stackTrace;
		}
	}

	public override int HResult
	{
		get
		{
			ClrInstanceField fieldByName = _type.GetFieldByName("_HResult");
			if (fieldByName != null)
			{
				return (int)fieldByName.GetValue(_object);
			}
			int value = 0;
			DesktopRuntimeBase desktopRuntime = _type.DesktopHeap.DesktopRuntime;
			uint exceptionHROffset = desktopRuntime.GetExceptionHROffset();
			desktopRuntime.ReadDword(_object + exceptionHROffset, out value);
			return value;
		}
	}

	public DesktopException(ulong objRef, BaseDesktopHeapType type)
	{
		_object = objRef;
		_type = type;
	}
}
