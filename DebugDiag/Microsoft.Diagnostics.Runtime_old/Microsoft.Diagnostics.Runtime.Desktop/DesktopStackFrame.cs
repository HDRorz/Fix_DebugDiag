using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopStackFrame : ClrStackFrame
{
	private ulong _ip;

	private ulong _sp;

	private string _frameName;

	private ClrStackFrameType _type;

	private ClrMethod _method;

	private DesktopRuntimeBase _runtime;

	public override ulong StackPointer => _sp;

	public override ulong InstructionPointer => _ip;

	public override ClrStackFrameType Kind => _type;

	public override string DisplayString => _frameName;

	public override ClrMethod Method
	{
		get
		{
			if (_method == null && _ip != 0L && _type == ClrStackFrameType.ManagedMethod)
			{
				_method = _runtime.GetMethodByAddress(_ip);
			}
			return _method;
		}
	}

	public override string ToString()
	{
		if (_type == ClrStackFrameType.ManagedMethod)
		{
			return _frameName;
		}
		int num = 0;
		int num2 = 0;
		if (_method != null)
		{
			num = _method.Name.Length;
			if (_method.Type != null)
			{
				num2 = _method.Type.Name.Length;
			}
		}
		StringBuilder stringBuilder = new StringBuilder(_frameName.Length + num + num2 + 10);
		stringBuilder.Append('[');
		stringBuilder.Append(_frameName);
		stringBuilder.Append(']');
		if (_method != null)
		{
			stringBuilder.Append(" (");
			if (_method.Type != null)
			{
				stringBuilder.Append(_method.Type.Name);
				stringBuilder.Append('.');
			}
			stringBuilder.Append(_method.Name);
			stringBuilder.Append(')');
		}
		return stringBuilder.ToString();
	}

	public override SourceLocation GetFileAndLineNumber()
	{
		if (_type == ClrStackFrameType.Runtime)
		{
			return null;
		}
		ClrMethod method = Method;
		return method?.GetSourceLocationForOffset(_ip - method.NativeCode);
	}

	public DesktopStackFrame(DesktopRuntimeBase runtime, ulong ip, ulong sp, ulong md)
	{
		_runtime = runtime;
		_ip = ip;
		_sp = sp;
		_frameName = _runtime.GetNameForMD(md) ?? "Unknown";
		_type = ClrStackFrameType.ManagedMethod;
		InitMethod(md);
	}

	public DesktopStackFrame(DesktopRuntimeBase runtime, ulong sp, ulong md)
	{
		_runtime = runtime;
		_sp = sp;
		_frameName = _runtime.GetNameForMD(md) ?? "Unknown";
		_type = ClrStackFrameType.Runtime;
		InitMethod(md);
	}

	public DesktopStackFrame(DesktopRuntimeBase runtime, ulong sp, string method, ClrMethod innerMethod)
	{
		_runtime = runtime;
		_sp = sp;
		_frameName = method ?? "Unknown";
		_type = ClrStackFrameType.Runtime;
		_method = innerMethod;
	}

	private void InitMethod(ulong md)
	{
		if (_method == null)
		{
			if (_ip != 0L && _type == ClrStackFrameType.ManagedMethod)
			{
				_method = _runtime.GetMethodByAddress(_ip);
			}
			else if (md != 0L)
			{
				IMethodDescData methodDescData = _runtime.GetMethodDescData(md);
				_method = DesktopMethod.Create(_runtime, methodDescData);
			}
		}
	}
}
