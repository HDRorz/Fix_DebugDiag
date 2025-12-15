using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThread : ClrThread
{
	internal enum TlsThreadType
	{
		ThreadType_GC = 1,
		ThreadType_Timer = 2,
		ThreadType_Gate = 4,
		ThreadType_DbgHelper = 8,
		ThreadType_DynamicSuspendEE = 0x20,
		ThreadType_ShutdownHelper = 0x400,
		ThreadType_Threadpool_IOCompletion = 0x800,
		ThreadType_Threadpool_Worker = 0x1000,
		ThreadType_Wait = 0x2000
	}

	private enum ThreadState
	{
		TS_AbortRequested = 1,
		TS_GCSuspendPending = 2,
		TS_UserSuspendPending = 4,
		TS_DebugSuspendPending = 8,
		TS_Background = 0x200,
		TS_Unstarted = 0x400,
		TS_Dead = 0x800,
		TS_CoInitialized = 0x2000,
		TS_InSTA = 0x4000,
		TS_InMTA = 0x8000,
		TS_Aborted = 0x800000,
		TS_TPWorkerThread = 0x1000000,
		TS_CompletionPortThread = 0x8000000,
		TS_AbortInitiated = 0x10000000
	}

	private uint _osThreadId;

	private RuntimeBase _runtime;

	private IList<ClrStackFrame> _stackTrace;

	private bool _finalizer;

	private bool _tlsInit;

	private int _threadType;

	private int _threadState;

	private uint _managedThreadId;

	private uint _lockCount;

	private ulong _address;

	private ulong _appDomain;

	private ulong _teb;

	private ulong _exception;

	private BlockingObject[] _blockingObjs;

	private bool _preemptive;

	public override ulong Address => _address;

	public override bool IsFinalizer => _finalizer;

	public override ClrException CurrentException
	{
		get
		{
			ulong value = _exception;
			if (value == 0L)
			{
				return null;
			}
			if (!_runtime.ReadPointer(value, out value) || value == 0L)
			{
				return null;
			}
			return _runtime.GetHeap().GetExceptionObject(value);
		}
	}

	public override bool IsGC => (ThreadType & 1) == 1;

	public override bool IsDebuggerHelper => (ThreadType & 8) == 8;

	public override bool IsThreadpoolTimer => (ThreadType & 2) == 2;

	public override bool IsThreadpoolCompletionPort
	{
		get
		{
			if ((ThreadType & 0x800) != 2048)
			{
				return (_threadState & 0x8000000) == 134217728;
			}
			return true;
		}
	}

	public override bool IsThreadpoolWorker
	{
		get
		{
			if ((ThreadType & 0x1000) != 4096)
			{
				return (_threadState & 0x1000000) == 16777216;
			}
			return true;
		}
	}

	public override bool IsThreadpoolWait => (ThreadType & 0x2000) == 8192;

	public override bool IsThreadpoolGate => (ThreadType & 4) == 4;

	public override bool IsSuspendingEE => (ThreadType & 0x20) == 32;

	public override bool IsShutdownHelper => (ThreadType & 0x400) == 1024;

	public override bool IsAborted => (_threadState & 0x800000) == 8388608;

	public override bool IsGCSuspendPending => (_threadState & 2) == 2;

	public override bool IsUserSuspended => (_threadState & 4) == 4;

	public override bool IsDebugSuspended => (_threadState & 8) == 8;

	public override bool IsBackground => (_threadState & 0x200) == 512;

	public override bool IsUnstarted => (_threadState & 0x400) == 1024;

	public override bool IsCoInitialized => (_threadState & 0x2000) == 8192;

	public override GcMode GcMode
	{
		get
		{
			if (!_preemptive)
			{
				return GcMode.Cooperative;
			}
			return GcMode.Preemptive;
		}
	}

	public override bool IsSTA => (_threadState & 0x4000) == 16384;

	public override bool IsMTA => (_threadState & 0x8000) == 32768;

	public override bool IsAbortRequested
	{
		get
		{
			if ((_threadState & 1) != 1)
			{
				return (_threadState & 0x10000000) == 268435456;
			}
			return true;
		}
	}

	public override bool IsAlive
	{
		get
		{
			if (_osThreadId != 0)
			{
				return (_threadState & 0xC00) == 0;
			}
			return false;
		}
	}

	public override uint OSThreadId => _osThreadId;

	public override int ManagedThreadId => (int)_managedThreadId;

	public override ulong AppDomain => _appDomain;

	public override uint LockCount => _lockCount;

	public override ulong Teb => _teb;

	public override ulong StackBase
	{
		get
		{
			if (_teb == 0L)
			{
				return 0uL;
			}
			ulong value = _teb + (ulong)IntPtr.Size;
			if (!_runtime.ReadPointer(value, out value))
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
			if (!_runtime.ReadPointer(value, out value))
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
				foreach (ClrStackFrame item in _runtime.EnumerateStackFrames(OSThreadId))
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

	public override IList<BlockingObject> BlockingObjects
	{
		get
		{
			((DesktopGCHeap)_runtime.GetHeap()).InitLockInspection();
			if (_blockingObjs == null)
			{
				return new BlockingObject[0];
			}
			return _blockingObjs;
		}
	}

	private int ThreadType
	{
		get
		{
			InitTls();
			return _threadType;
		}
	}

	public override IEnumerable<ClrRoot> EnumerateStackObjects()
	{
		return _runtime.EnumerateStackReferences(this, includeDead: true);
	}

	public override IEnumerable<ClrRoot> EnumerateStackObjects(bool includePossiblyDead)
	{
		return _runtime.EnumerateStackReferences(this, includePossiblyDead);
	}

	public override IEnumerable<ClrStackFrame> EnumerateStackTrace()
	{
		return _runtime.EnumerateStackFrames(OSThreadId);
	}

	internal void SetBlockingObjects(BlockingObject[] blobjs)
	{
		_blockingObjs = blobjs;
	}

	private void InitTls()
	{
		if (!_tlsInit)
		{
			_tlsInit = true;
			_threadType = GetTlsSlotForThread(_runtime, Teb);
		}
	}

	internal static int GetTlsSlotForThread(RuntimeBase runtime, ulong teb)
	{
		uint pointerSize = (uint)runtime.PointerSize;
		ulong num = teb + 5248;
		uint tlsSlot = runtime.GetTlsSlot();
		if (tlsSlot == uint.MaxValue)
		{
			return 0;
		}
		ulong value = 0uL;
		if (tlsSlot < 64)
		{
			value = num + pointerSize * tlsSlot;
		}
		else
		{
			if (!runtime.ReadPointer(teb + 6016, out value) || value == 0L)
			{
				return 0;
			}
			value += pointerSize * (tlsSlot - 64);
		}
		ulong value2 = 0uL;
		if (!runtime.ReadPointer(value, out value2))
		{
			return 0;
		}
		uint threadTypeIndex = runtime.GetThreadTypeIndex();
		if (threadTypeIndex == uint.MaxValue)
		{
			return 0;
		}
		ulong value3 = 0uL;
		if (!runtime.ReadPointer(value2 + pointerSize * threadTypeIndex, out value3))
		{
			return 0;
		}
		return (int)value3;
	}

	internal DesktopThread(RuntimeBase clr, IThreadData thread, ulong address, bool finalizer)
	{
		_runtime = clr;
		_address = address;
		_finalizer = finalizer;
		if (thread != null)
		{
			_osThreadId = thread.OSThreadID;
			_managedThreadId = thread.ManagedThreadID;
			_appDomain = thread.AppDomain;
			_lockCount = thread.LockCount;
			_teb = thread.Teb;
			_threadState = thread.State;
			_exception = thread.ExceptionPtr;
			_preemptive = thread.Preemptive;
		}
	}
}
