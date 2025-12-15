using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThread : ThreadBase
{
	private bool _corDebugInit;

	internal DesktopRuntimeBase DesktopRuntime { get; }

	internal ICorDebugThread CorDebugThread => DesktopRuntime.GetCorDebugThread(OSThreadId);

	public override ClrRuntime Runtime => DesktopRuntime;

	public override ClrException CurrentException
	{
		get
		{
			ulong value = _exception;
			if (value == 0L)
			{
				return null;
			}
			if (!DesktopRuntime.ReadPointer(value, out value) || value == 0L)
			{
				return null;
			}
			return DesktopRuntime.Heap.GetExceptionObject(value);
		}
	}

	public override ulong StackBase
	{
		get
		{
			if (_teb == 0L)
			{
				return 0uL;
			}
			ulong value = _teb + (ulong)IntPtr.Size;
			if (!DesktopRuntime.ReadPointer(value, out value))
			{
				return 0uL;
			}
			return value;
		}
	}

	public override ulong StackLimit
	{
		get
		{
			if (_teb == 0L)
			{
				return 0uL;
			}
			ulong value = _teb + (ulong)((long)IntPtr.Size * 2L);
			if (!DesktopRuntime.ReadPointer(value, out value))
			{
				return 0uL;
			}
			return value;
		}
	}

	public override IList<ClrStackFrame> StackTrace
	{
		get
		{
			if (_stackTrace == null)
			{
				List<ClrStackFrame> list = new List<ClrStackFrame>(32);
				ulong num = ulong.MaxValue;
				int num2 = 0;
				int num3 = 4096;
				foreach (ClrStackFrame item in DesktopRuntime.EnumerateStackFrames(this))
				{
					if (num3-- == 0)
					{
						break;
					}
					if (item.StackPointer == num)
					{
						if (num2++ >= 5)
						{
							break;
						}
					}
					else
					{
						num = item.StackPointer;
						num2 = 0;
					}
					list.Add(item);
				}
				_stackTrace = list.ToArray();
			}
			return _stackTrace;
		}
	}

	[Obsolete]
	public override IList<BlockingObject> BlockingObjects
	{
		get
		{
			((DesktopGCHeap)DesktopRuntime.Heap).InitLockInspection();
			if (_blockingObjs == null)
			{
				return new BlockingObject[0];
			}
			return _blockingObjs;
		}
	}

	public override IEnumerable<ClrRoot> EnumerateStackObjects()
	{
		return DesktopRuntime.EnumerateStackReferences(this, includeDead: true);
	}

	public override IEnumerable<ClrRoot> EnumerateStackObjects(bool includePossiblyDead)
	{
		return DesktopRuntime.EnumerateStackReferences(this, includePossiblyDead);
	}

	internal unsafe void InitLocalData()
	{
		if (_corDebugInit)
		{
			return;
		}
		_corDebugInit = true;
		((ICorDebugThread3)CorDebugThread).CreateStackWalk(out var ppStackWalk);
		do
		{
			ppStackWalk.GetFrame(out var ppFrame);
			if (ppFrame is ICorDebugILFrame cordbFrame)
			{
				byte[] context = ContextHelper.Context;
				fixed (byte* value = context)
				{
					ppStackWalk.GetContext(ContextHelper.ContextFlags, ContextHelper.Length, out var _, new IntPtr(value));
				}
				ulong ip;
				ulong sp;
				if (IntPtr.Size == 4)
				{
					ip = BitConverter.ToUInt32(context, ContextHelper.InstructionPointerOffset);
					sp = BitConverter.ToUInt32(context, ContextHelper.StackPointerOffset);
				}
				else
				{
					ip = BitConverter.ToUInt64(context, ContextHelper.InstructionPointerOffset);
					sp = BitConverter.ToUInt64(context, ContextHelper.StackPointerOffset);
				}
				DesktopStackFrame desktopStackFrame = (from frm in _stackTrace
					where sp == frm.StackPointer && ip == frm.InstructionPointer
					select frm into p
					select (DesktopStackFrame)p).SingleOrDefault();
				if (desktopStackFrame != null)
				{
					desktopStackFrame.CordbFrame = cordbFrame;
				}
			}
		}
		while (ppStackWalk.Next() == 0);
	}

	public override IEnumerable<ClrStackFrame> EnumerateStackTrace()
	{
		return DesktopRuntime.EnumerateStackFrames(this);
	}

	internal DesktopThread(DesktopRuntimeBase clr, IThreadData thread, ulong address, bool finalizer)
		: base(thread, address, finalizer)
	{
		DesktopRuntime = clr;
	}
}
