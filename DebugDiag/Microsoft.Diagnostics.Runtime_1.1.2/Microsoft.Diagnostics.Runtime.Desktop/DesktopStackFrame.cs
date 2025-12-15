using System.Text;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopStackFrame : ClrStackFrame
{
	private readonly ulong _ip;

	private readonly string _frameName;

	private readonly ClrStackFrameType _type;

	private ClrMethod _method;

	private readonly DesktopRuntimeBase _runtime;

	private readonly DesktopThread _thread;

	private readonly byte[] _context;

	public override byte[] Context => _context;

	public override ClrThread Thread => _thread;

	public override ulong StackPointer { get; }

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

	public ICorDebugILFrame CordbFrame { get; internal set; }

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

	public DesktopStackFrame(DesktopRuntimeBase runtime, DesktopThread thread, byte[] context, ulong ip, ulong sp, ulong md)
	{
		_runtime = runtime;
		_thread = thread;
		_context = context;
		_ip = ip;
		StackPointer = sp;
		_frameName = _runtime.GetNameForMD(md) ?? "Unknown";
		_type = ClrStackFrameType.ManagedMethod;
		InitMethod(md);
	}

	public DesktopStackFrame(DesktopRuntimeBase runtime, DesktopThread thread, byte[] context, ulong sp, ulong md)
	{
		_runtime = runtime;
		_thread = thread;
		_context = context;
		StackPointer = sp;
		_frameName = _runtime.GetNameForMD(md) ?? "Unknown";
		_type = ClrStackFrameType.Runtime;
		InitMethod(md);
	}

	public DesktopStackFrame(DesktopRuntimeBase runtime, DesktopThread thread, byte[] context, ulong sp, string method, ClrMethod innerMethod)
	{
		_runtime = runtime;
		_thread = thread;
		_context = context;
		StackPointer = sp;
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
