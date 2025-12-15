using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class LockInspection
{
	private DesktopGCHeap _heap;

	private DesktopRuntimeBase _runtime;

	private ClrType _rwType;

	private ClrType _rwsType;

	private Dictionary<ulong, DesktopBlockingObject> _monitors = new Dictionary<ulong, DesktopBlockingObject>();

	private Dictionary<ulong, DesktopBlockingObject> _locks = new Dictionary<ulong, DesktopBlockingObject>();

	private Dictionary<ClrThread, DesktopBlockingObject> _joinLocks = new Dictionary<ClrThread, DesktopBlockingObject>();

	private Dictionary<ulong, DesktopBlockingObject> _waitLocks = new Dictionary<ulong, DesktopBlockingObject>();

	private Dictionary<ulong, ulong> _syncblks = new Dictionary<ulong, ulong>();

	private DesktopBlockingObject[] _result;

	private const int HASHCODE_BITS = 25;

	private const int SYNCBLOCKINDEX_BITS = 26;

	private const uint BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX = 134217728u;

	private const uint BIT_SBLK_FINALIZER_RUN = 1073741824u;

	private const uint BIT_SBLK_SPIN_LOCK = 268435456u;

	private const uint SBLK_MASK_LOCK_THREADID = 1023u;

	private const int SBLK_MASK_LOCK_RECLEVEL = 64512;

	private const uint SBLK_APPDOMAIN_SHIFT = 16u;

	private const uint SBLK_MASK_APPDOMAININDEX = 2047u;

	private const int SBLK_RECLEVEL_SHIFT = 10;

	private const uint BIT_SBLK_IS_HASHCODE = 67108864u;

	private const uint MASK_HASHCODE = 33554431u;

	private const uint MASK_SYNCBLOCKINDEX = 67108863u;

	internal LockInspection(DesktopGCHeap heap, DesktopRuntimeBase runtime)
	{
		_heap = heap;
		_runtime = runtime;
	}

	internal DesktopBlockingObject[] InitLockInspection()
	{
		if (_result != null)
		{
			return _result;
		}
		foreach (ClrSegment segment in _heap.Segments)
		{
			for (ulong num = segment.FirstObject; num != 0L; num = segment.NextObject(num))
			{
				ClrType objectType = _heap.GetObjectType(num);
				if (IsReaderWriterLock(num, objectType))
				{
					_locks[num] = CreateRWLObject(num, objectType);
				}
				else if (IsReaderWriterSlim(num, objectType))
				{
					_locks[num] = CreateRWSObject(num, objectType);
				}
				if (_heap.GetObjectHeader(num, out var value) && (value & 0x18000000) == 0)
				{
					uint num2 = value & 0x3FF;
					if (num2 != 0)
					{
						ClrThread threadFromThinlockID = _runtime.GetThreadFromThinlockID(num2);
						if (threadFromThinlockID != null)
						{
							int num3 = (int)(value & 0xFC00) >> 10;
							Dictionary<ulong, DesktopBlockingObject> monitors = _monitors;
							ulong num4 = num;
							monitors[num4] = new DesktopBlockingObject(num4, locked: true, num3 + 1, threadFromThinlockID, BlockingReason.Monitor);
						}
					}
				}
			}
		}
		int syncblkCount = _runtime.GetSyncblkCount();
		for (int i = 0; i < syncblkCount; i++)
		{
			ISyncBlkData syncblkData = _runtime.GetSyncblkData(i);
			if (syncblkData == null || syncblkData.Free)
			{
				continue;
			}
			_syncblks[syncblkData.Address] = syncblkData.Object;
			_syncblks[syncblkData.Object] = syncblkData.Object;
			ClrThread owner = null;
			if (syncblkData.MonitorHeld)
			{
				ulong owningThread = syncblkData.OwningThread;
				foreach (ClrThread thread in _runtime.Threads)
				{
					if (thread.Address == owningThread)
					{
						owner = thread;
						break;
					}
				}
			}
			_monitors[syncblkData.Object] = new DesktopBlockingObject(syncblkData.Object, syncblkData.MonitorHeld, (int)syncblkData.Recursion, owner, BlockingReason.Monitor);
		}
		SetThreadWaiters();
		int num5 = _monitors.Count + _locks.Count + _joinLocks.Count + _waitLocks.Count;
		_result = new DesktopBlockingObject[num5];
		int num6 = 0;
		foreach (DesktopBlockingObject value2 in _monitors.Values)
		{
			_result[num6++] = value2;
		}
		foreach (DesktopBlockingObject value3 in _locks.Values)
		{
			_result[num6++] = value3;
		}
		foreach (DesktopBlockingObject value4 in _joinLocks.Values)
		{
			_result[num6++] = value4;
		}
		foreach (DesktopBlockingObject value5 in _waitLocks.Values)
		{
			_result[num6++] = value5;
		}
		_monitors = null;
		_locks = null;
		_joinLocks = null;
		_waitLocks = null;
		_syncblks = null;
		return _result;
	}

	private bool IsReaderWriterLock(ulong obj, ClrType type)
	{
		if (type == null)
		{
			return false;
		}
		if (_rwType == null)
		{
			if (type.Name != "System.Threading.ReaderWriterLock")
			{
				return false;
			}
			_rwType = type;
			return true;
		}
		return _rwType == type;
	}

	private bool IsReaderWriterSlim(ulong obj, ClrType type)
	{
		if (type == null)
		{
			return false;
		}
		if (_rwsType == null)
		{
			if (type.Name != "System.Threading.ReaderWriterLockSlim")
			{
				return false;
			}
			_rwsType = type;
			return true;
		}
		return _rwsType == type;
	}

	private void SetThreadWaiters()
	{
		HashSet<string> hashSet = null;
		List<BlockingObject> list = new List<BlockingObject>();
		foreach (DesktopThread thread in _runtime.Threads)
		{
			int num = thread.StackTrace.Count;
			if (num > 10)
			{
				num = 10;
			}
			list.Clear();
			for (int i = 0; i < num; i++)
			{
				DesktopBlockingObject value = null;
				ClrMethod method = thread.StackTrace[i].Method;
				if (method == null)
				{
					continue;
				}
				ClrType type = method.Type;
				if (type == null)
				{
					continue;
				}
				string name = method.Name;
				uint num2 = _003CPrivateImplementationDetails_003E._0024_0024method0x6000001_002DComputeStringHash(name);
				if (num2 <= 1936066642)
				{
					if (num2 <= 907631167)
					{
						if (num2 <= 705120903)
						{
							if (num2 != 162663674)
							{
								if (num2 != 198508992)
								{
									if (num2 == 705120903 && name == "TryEnterUpgradeableReadLock")
									{
										goto IL_04f3;
									}
								}
								else if (name == "Wait")
								{
									goto IL_0659;
								}
							}
							else if (name == "UpgradeToWriterLock")
							{
								goto IL_0423;
							}
						}
						else if (num2 != 714197144)
						{
							if (num2 != 805458841)
							{
								if (num2 == 907631167 && name == "ObjWait")
								{
									goto IL_0659;
								}
							}
							else if (name == "Join")
							{
								goto IL_05c3;
							}
						}
						else if (name == "TryEnter")
						{
							goto IL_0869;
						}
					}
					else if (num2 <= 1585928636)
					{
						if (num2 != 925157619)
						{
							if (num2 != 1447750699)
							{
								if (num2 == 1585928636 && name == "TryEnterUpgradeableReadLockCore")
								{
									goto IL_04f3;
								}
							}
							else if (name == "TryEnterTimeout")
							{
								goto IL_0869;
							}
						}
						else if (name == "AcquireReaderLock")
						{
							goto IL_0423;
						}
					}
					else if (num2 != 1707674859)
					{
						if (num2 != 1831486128)
						{
							if (num2 == 1936066642 && name == "FCallUpgradeToWriterLock")
							{
								goto IL_0423;
							}
						}
						else if (name == "TryEnterWriteLock")
						{
							goto IL_04f3;
						}
					}
					else if (name == "InternalWaitOne")
					{
						goto IL_0778;
					}
				}
				else if (num2 <= 2999638248u)
				{
					if (num2 <= 2623905299u)
					{
						if (num2 != 2063703683)
						{
							if (num2 != 2546871556u)
							{
								if (num2 == 2623905299u && name == "WaitAll")
								{
									goto IL_06c0;
								}
							}
							else if (name == "WaitOne")
							{
								goto IL_0778;
							}
						}
						else if (name == "TryEnterReadLock")
						{
							goto IL_04f3;
						}
					}
					else if (num2 != 2879186847u)
					{
						if (num2 != 2952291245u)
						{
							if (num2 == 2999638248u && name == "TryEnterReadLockCore")
							{
								goto IL_04f3;
							}
						}
						else if (name == "Enter")
						{
							goto IL_0869;
						}
					}
					else if (name == "TryEnterWriteLockCore")
					{
						goto IL_04f3;
					}
				}
				else if (num2 <= 3283590303u)
				{
					if (num2 != 3009496346u)
					{
						if (num2 != 3129621346u)
						{
							if (num2 == 3283590303u && name == "WaitOneNative")
							{
								goto IL_0778;
							}
						}
						else if (name == "AcquireWriterLockInternal")
						{
							goto IL_0423;
						}
					}
					else if (name == "WaitAny")
					{
						goto IL_06c0;
					}
				}
				else if (num2 != 3294281276u)
				{
					if (num2 != 3847302410u)
					{
						if (num2 == 3903738850u && name == "AcquireReaderLockInternal")
						{
							goto IL_0423;
						}
					}
					else if (name == "ReliableEnterTimeout")
					{
						goto IL_0869;
					}
				}
				else if (name == "JoinInternal")
				{
					goto IL_05c3;
				}
				goto IL_08a8;
				IL_0659:
				if (type.Name == "System.Threading.Monitor")
				{
					value = FindMonitor(thread.StackLimit, thread.StackTrace[i].StackPointer);
					if (value == null)
					{
						value = FindMonitor(thread.StackTrace[i].StackPointer, thread.StackBase);
					}
					value.Reason = BlockingReason.MonitorWait;
				}
				goto IL_08a8;
				IL_0423:
				if (type.Name == "System.Threading.ReaderWriterLock")
				{
					value = FindLocks(thread.StackLimit, thread.StackTrace[i].StackPointer, IsReaderWriterLock);
					if (value == null)
					{
						value = FindLocks(thread.StackTrace[i].StackPointer, thread.StackBase, IsReaderWriterLock);
					}
					if (value != null && (value.Reason == BlockingReason.Unknown || value.Reason == BlockingReason.None))
					{
						if (method.Name == "AcquireReaderLockInternal" || method.Name == "AcquireReaderLock")
						{
							value.Reason = BlockingReason.WriterAcquired;
						}
						else
						{
							value.Reason = BlockingReason.ReaderAcquired;
						}
					}
				}
				goto IL_08a8;
				IL_0869:
				if (type.Name == "System.Threading.Monitor")
				{
					value = FindMonitor(thread.StackLimit, thread.StackTrace[i].StackPointer);
					if (value != null)
					{
						value.Reason = BlockingReason.Monitor;
					}
				}
				goto IL_08a8;
				IL_0778:
				if (type.Name == "System.Threading.WaitHandle")
				{
					if (hashSet == null)
					{
						hashSet = new HashSet<string>();
						hashSet.Add("System.Threading.Mutex");
						hashSet.Add("System.Threading.Semaphore");
						hashSet.Add("System.Threading.ManualResetEvent");
						hashSet.Add("System.Threading.AutoResetEvent");
						hashSet.Add("System.Threading.WaitHandle");
						hashSet.Add("Microsoft.Win32.SafeHandles.SafeWaitHandle");
					}
					ulong num3 = FindWaitHandle(thread.StackLimit, thread.StackTrace[i].StackPointer, hashSet);
					if (num3 == 0L)
					{
						num3 = FindWaitHandle(thread.StackTrace[i].StackPointer, thread.StackBase, hashSet);
					}
					if (num3 != 0L)
					{
						if (_waitLocks == null)
						{
							_waitLocks = new Dictionary<ulong, DesktopBlockingObject>();
						}
						if (!_waitLocks.TryGetValue(num3, out value))
						{
							Dictionary<ulong, DesktopBlockingObject> waitLocks = _waitLocks;
							ulong num4 = num3;
							value = (waitLocks[num4] = new DesktopBlockingObject(num4, locked: true, 0, null, BlockingReason.WaitOne));
						}
					}
				}
				goto IL_08a8;
				IL_04f3:
				if (type.Name == "System.Threading.ReaderWriterLockSlim")
				{
					value = FindLocks(thread.StackLimit, thread.StackTrace[i].StackPointer, IsReaderWriterSlim);
					if (value == null)
					{
						value = FindLocks(thread.StackTrace[i].StackPointer, thread.StackBase, IsReaderWriterSlim);
					}
					if (value != null && (value.Reason == BlockingReason.Unknown || value.Reason == BlockingReason.None))
					{
						if (method.Name == "TryEnterWriteLock" || method.Name == "TryEnterWriteLockCore")
						{
							value.Reason = BlockingReason.ReaderAcquired;
						}
						else
						{
							value.Reason = BlockingReason.WriterAcquired;
						}
					}
				}
				goto IL_08a8;
				IL_08a8:
				if (value == null)
				{
					continue;
				}
				bool flag = false;
				foreach (BlockingObject item in list)
				{
					if (item.Object == value.Object)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(value);
				}
				continue;
				IL_05c3:
				if (type.Name == "System.Threading.Thread" && (FindThread(thread.StackLimit, thread.StackTrace[i].StackPointer, out var threadAddr, out var target) || FindThread(thread.StackTrace[i].StackPointer, thread.StackBase, out threadAddr, out target)) && !_joinLocks.TryGetValue(target, out value))
				{
					value = (_joinLocks[target] = new DesktopBlockingObject(threadAddr, locked: true, 0, target, BlockingReason.ThreadJoin));
				}
				goto IL_08a8;
				IL_06c0:
				if (type.Name == "System.Threading.WaitHandle")
				{
					ulong num5 = FindWaitObjects(thread.StackLimit, thread.StackTrace[i].StackPointer, "System.Threading.WaitHandle[]");
					if (num5 == 0L)
					{
						num5 = FindWaitObjects(thread.StackTrace[i].StackPointer, thread.StackBase, "System.Threading.WaitHandle[]");
					}
					if (num5 != 0L)
					{
						BlockingReason reason = ((method.Name == "WaitAny") ? BlockingReason.WaitAny : BlockingReason.WaitAll);
						if (!_waitLocks.TryGetValue(num5, out value))
						{
							Dictionary<ulong, DesktopBlockingObject> waitLocks2 = _waitLocks;
							ulong num6 = num5;
							value = (waitLocks2[num6] = new DesktopBlockingObject(num6, locked: true, 0, null, reason));
						}
					}
				}
				goto IL_08a8;
			}
			foreach (DesktopBlockingObject item2 in list)
			{
				item2.AddWaiter(thread);
			}
			thread.SetBlockingObjects(list.ToArray());
		}
	}

	private DesktopBlockingObject CreateRWLObject(ulong obj, ClrType type)
	{
		if (type == null)
		{
			return new DesktopBlockingObject(obj, locked: false, 0, null, BlockingReason.None);
		}
		ClrInstanceField fieldByName = type.GetFieldByName("_dwWriterID");
		if (fieldByName != null && fieldByName.ElementType == ClrElementType.Int32)
		{
			int num = (int)fieldByName.GetValue(obj);
			if (num > 0)
			{
				ClrThread threadById = GetThreadById(num);
				if (threadById != null)
				{
					return new DesktopBlockingObject(obj, locked: true, 0, threadById, BlockingReason.ReaderAcquired);
				}
			}
		}
		ClrInstanceField fieldByName2 = type.GetFieldByName("_dwULockID");
		ClrInstanceField fieldByName3 = type.GetFieldByName("_dwLLockID");
		if (fieldByName2 != null && fieldByName2.ElementType == ClrElementType.Int32 && fieldByName3 != null && fieldByName3.ElementType == ClrElementType.Int32)
		{
			int num2 = (int)fieldByName2.GetValue(obj);
			int num3 = (int)fieldByName3.GetValue(obj);
			List<ClrThread> list = null;
			foreach (ClrThread thread in _runtime.Threads)
			{
				foreach (IRWLockData item in _runtime.EnumerateLockData(thread.Address))
				{
					if (item.LLockID == num3 && item.ULockID == num2 && item.Level > 0)
					{
						if (list == null)
						{
							list = new List<ClrThread>();
						}
						list.Add(thread);
						break;
					}
				}
			}
			if (list != null)
			{
				return new DesktopBlockingObject(obj, locked: true, 0, BlockingReason.ReaderAcquired, list.ToArray());
			}
		}
		return new DesktopBlockingObject(obj, locked: false, 0, null, BlockingReason.None);
	}

	private DesktopBlockingObject CreateRWSObject(ulong obj, ClrType type)
	{
		if (type == null)
		{
			return new DesktopBlockingObject(obj, locked: false, 0, null, BlockingReason.None);
		}
		ClrInstanceField fieldByName = type.GetFieldByName("writeLockOwnerId");
		if (fieldByName != null && fieldByName.ElementType == ClrElementType.Int32)
		{
			int id = (int)fieldByName.GetValue(obj);
			ClrThread threadById = GetThreadById(id);
			if (threadById != null)
			{
				return new DesktopBlockingObject(obj, locked: true, 0, threadById, BlockingReason.WriterAcquired);
			}
		}
		fieldByName = type.GetFieldByName("upgradeLockOwnerId");
		if (fieldByName != null && fieldByName.ElementType == ClrElementType.Int32)
		{
			int id2 = (int)fieldByName.GetValue(obj);
			ClrThread threadById2 = GetThreadById(id2);
			if (threadById2 != null)
			{
				return new DesktopBlockingObject(obj, locked: true, 0, threadById2, BlockingReason.WriterAcquired);
			}
		}
		fieldByName = type.GetFieldByName("rwc");
		if (fieldByName != null)
		{
			List<ClrThread> threads = null;
			ulong objRef = (ulong)fieldByName.GetValue(obj);
			ClrType objectType = _heap.GetObjectType(objRef);
			if (objectType != null && objectType.IsArray && objectType.ArrayComponentType != null)
			{
				ClrType arrayComponentType = objectType.ArrayComponentType;
				ClrInstanceField fieldByName2 = arrayComponentType.GetFieldByName("threadid");
				ClrInstanceField fieldByName3 = arrayComponentType.GetFieldByName("next");
				if (fieldByName2 != null && fieldByName3 != null)
				{
					int arrayLength = objectType.GetArrayLength(objRef);
					for (int i = 0; i < arrayLength; i++)
					{
						ulong curr = (ulong)objectType.GetArrayElementValue(objRef, i);
						GetThreadEntry(ref threads, fieldByName2, fieldByName3, curr, interior: false);
					}
				}
			}
			if (threads != null)
			{
				return new DesktopBlockingObject(obj, locked: true, 0, BlockingReason.ReaderAcquired, threads.ToArray());
			}
		}
		return new DesktopBlockingObject(obj, locked: false, 0, null, BlockingReason.None);
	}

	private void GetThreadEntry(ref List<ClrThread> threads, ClrInstanceField threadId, ClrInstanceField next, ulong curr, bool interior)
	{
		if (curr == 0L)
		{
			return;
		}
		int id = (int)threadId.GetValue(curr, interior);
		ClrThread threadById = GetThreadById(id);
		if (threadById != null)
		{
			if (threads == null)
			{
				threads = new List<ClrThread>();
			}
			threads.Add(threadById);
		}
		curr = (ulong)next.GetValue(curr, interior);
		if (curr != 0L)
		{
			GetThreadEntry(ref threads, threadId, next, curr, interior: false);
		}
	}

	private ulong FindWaitHandle(ulong start, ulong stop, HashSet<string> eventTypes)
	{
		_runtime.GetHeap();
		using (IEnumerator<ulong> enumerator = EnumerateObjectsOfTypes(start, stop, eventTypes).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return 0uL;
	}

	private ulong FindWaitObjects(ulong start, ulong stop, string typeName)
	{
		_runtime.GetHeap();
		using (IEnumerator<ulong> enumerator = EnumerateObjectsOfType(start, stop, typeName).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return 0uL;
	}

	private IEnumerable<ulong> EnumerateObjectsOfTypes(ulong start, ulong stop, HashSet<string> types)
	{
		ClrHeap heap = _runtime.GetHeap();
		foreach (ulong item in EnumeratePointersInRange(start, stop))
		{
			if (!_runtime.ReadPointer(item, out var obj) || !heap.IsInHeap(obj))
			{
				continue;
			}
			ClrType type = heap.GetObjectType(obj);
			int sanity = 0;
			while (type != null)
			{
				if (types.Contains(type.Name))
				{
					yield return obj;
					break;
				}
				type = type.BaseType;
				if (sanity++ == 16)
				{
					break;
				}
			}
		}
	}

	private IEnumerable<ulong> EnumerateObjectsOfType(ulong start, ulong stop, string typeName)
	{
		ClrHeap heap = _runtime.GetHeap();
		foreach (ulong item in EnumeratePointersInRange(start, stop))
		{
			if (!_runtime.ReadPointer(item, out var obj) || !heap.IsInHeap(obj))
			{
				continue;
			}
			ClrType type = heap.GetObjectType(obj);
			int sanity = 0;
			while (type != null)
			{
				if (type.Name == typeName)
				{
					yield return obj;
					break;
				}
				type = type.BaseType;
				if (sanity++ == 16)
				{
					break;
				}
			}
		}
	}

	private bool FindThread(ulong start, ulong stop, out ulong threadAddr, out ClrThread target)
	{
		ClrHeap heap = _runtime.GetHeap();
		foreach (ulong item in EnumerateObjectsOfType(start, stop, "System.Threading.Thread"))
		{
			ClrInstanceField fieldByName = heap.GetObjectType(item).GetFieldByName("m_ManagedThreadId");
			if (fieldByName != null && fieldByName.ElementType == ClrElementType.Int32)
			{
				int id = (int)fieldByName.GetValue(item);
				ClrThread threadById = GetThreadById(id);
				if (threadById != null)
				{
					threadAddr = item;
					target = threadById;
					return true;
				}
			}
		}
		threadAddr = 0uL;
		target = null;
		return false;
	}

	private IEnumerable<ulong> EnumeratePointersInRange(ulong start, ulong stop)
	{
		uint diff = (uint)_runtime.PointerSize;
		if (start > stop)
		{
			for (ulong ptr = stop; ptr <= start; ptr += diff)
			{
				yield return ptr;
			}
			yield break;
		}
		for (ulong ptr2 = stop; ptr2 >= start; ptr2 -= diff)
		{
			yield return ptr2;
		}
	}

	private DesktopBlockingObject FindLocks(ulong start, ulong stop, Func<ulong, ClrType, bool> isCorrectType)
	{
		foreach (ulong item in EnumeratePointersInRange(start, stop))
		{
			ulong value = 0uL;
			if (_runtime.ReadPointer(item, out value))
			{
				DesktopBlockingObject value2 = null;
				if (_locks.TryGetValue(value, out value2) && isCorrectType(value, _heap.GetObjectType(value)))
				{
					return value2;
				}
			}
		}
		return null;
	}

	private DesktopBlockingObject FindMonitor(ulong start, ulong stop)
	{
		ulong num = 0uL;
		foreach (ulong item in EnumeratePointersInRange(start, stop))
		{
			ulong value = 0uL;
			if (_runtime.ReadPointer(item, out value) && _syncblks.TryGetValue(value, out value))
			{
				num = value;
				break;
			}
		}
		DesktopBlockingObject value2 = null;
		if (num != 0L && _monitors.TryGetValue(num, out value2))
		{
			return value2;
		}
		return null;
	}

	private ClrThread GetThreadById(int id)
	{
		if (id < 0)
		{
			return null;
		}
		foreach (ClrThread thread in _runtime.Threads)
		{
			if (thread.ManagedThreadId == id)
			{
				return thread;
			}
		}
		return null;
	}
}
