using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThreadPool : ClrThreadPool
{
	private DesktopRuntimeBase _runtime;

	private ClrHeap _heap;

	private int _totalThreads;

	private int _runningThreads;

	private int _idleThreads;

	private int _minThreads;

	private int _maxThreads;

	private int _minCP;

	private int _maxCP;

	private int _cpu;

	private int _freeCP;

	private int _maxFreeCP;

	public override int TotalThreads => _totalThreads;

	public override int RunningThreads => _runningThreads;

	public override int IdleThreads => _idleThreads;

	public override int MinThreads => _minThreads;

	public override int MaxThreads => _maxThreads;

	public override int MinCompletionPorts => _minCP;

	public override int MaxCompletionPorts => _maxCP;

	public override int CpuUtilization => _cpu;

	public override int FreeCompletionPortCount => _freeCP;

	public override int MaxFreeCompletionPorts => _maxFreeCP;

	public DesktopThreadPool(DesktopRuntimeBase runtime, IThreadPoolData data)
	{
		_runtime = runtime;
		_totalThreads = data.TotalThreads;
		_runningThreads = data.RunningThreads;
		_idleThreads = data.IdleThreads;
		_minThreads = data.MinThreads;
		_maxThreads = data.MaxThreads;
		_minCP = data.MinCP;
		_maxCP = data.MaxCP;
		_cpu = data.CPU;
		_freeCP = data.NumFreeCP;
		_maxFreeCP = data.MaxFreeCP;
	}

	public override IEnumerable<NativeWorkItem> EnumerateNativeWorkItems()
	{
		return _runtime.EnumerateWorkItems();
	}

	public override IEnumerable<ManagedWorkItem> EnumerateManagedWorkItems()
	{
		foreach (ulong item in EnumerateManagedThreadpoolObjects())
		{
			if (item != 0L)
			{
				ClrType objectType = _heap.GetObjectType(item);
				if (objectType != null)
				{
					yield return new DesktopManagedWorkItem(objectType, item);
				}
			}
		}
	}

	private IEnumerable<ulong> EnumerateManagedThreadpoolObjects()
	{
		_heap = _runtime.GetHeap();
		ClrModule mscorlib = GetMscorlib();
		if (mscorlib == null)
		{
			yield break;
		}
		ClrType typeByName = mscorlib.GetTypeByName("System.Threading.ThreadPoolGlobals");
		if (typeByName != null)
		{
			ClrStaticField workQueueField = typeByName.GetStaticFieldByName("workQueue");
			if (workQueueField != null)
			{
				foreach (ClrAppDomain appDomain in _runtime.AppDomains)
				{
					object value = workQueueField.GetValue(appDomain);
					ulong workQueue = ((value == null) ? 0 : ((ulong)value));
					ClrType workQueueType = _heap.GetObjectType(workQueue);
					if (workQueue == 0L || workQueueType == null)
					{
						continue;
					}
					ClrType queueHeadType;
					ulong queueHead;
					while (GetFieldObject(workQueueType, workQueue, "queueHead", out queueHeadType, out queueHead))
					{
						if (GetFieldObject(queueHeadType, queueHead, "nodes", out var nodesType, out var nodes) && nodesType.IsArray)
						{
							int len = nodesType.GetArrayLength(nodes);
							int i = 0;
							while (i < len)
							{
								ulong num = (ulong)nodesType.GetArrayElementValue(nodes, i);
								if (num != 0L)
								{
									yield return num;
								}
								int num2 = i + 1;
								i = num2;
							}
						}
						if (!GetFieldObject(queueHeadType, queueHead, "Next", out queueHeadType, out queueHead))
						{
							break;
						}
						nodesType = null;
						if (queueHead == 0L)
						{
							break;
						}
					}
					queueHeadType = null;
				}
			}
		}
		typeByName = mscorlib.GetTypeByName("System.Threading.ThreadPoolWorkQueue");
		if (typeByName == null)
		{
			yield break;
		}
		ClrStaticField threadQueuesField = typeByName.GetStaticFieldByName("allThreadQueues");
		if (threadQueuesField == null)
		{
			yield break;
		}
		foreach (ClrAppDomain appDomain2 in _runtime.AppDomains)
		{
			ulong num3 = (ulong)threadQueuesField.GetValue(appDomain2);
			if (num3 == 0L)
			{
				continue;
			}
			ClrType objectType = _heap.GetObjectType(num3);
			if (objectType == null)
			{
				continue;
			}
			ulong outerArray = 0uL;
			ClrType outerArrayType = null;
			if (!GetFieldObject(objectType, num3, "m_array", out outerArrayType, out outerArray) || !outerArrayType.IsArray)
			{
				continue;
			}
			int outerLen = outerArrayType.GetArrayLength(outerArray);
			int i2 = 0;
			while (i2 < outerLen)
			{
				ulong num4 = (ulong)outerArrayType.GetArrayElementValue(outerArray, i2);
				int num2;
				if (num4 != 0L)
				{
					ClrType objectType2 = _heap.GetObjectType(num4);
					if (objectType2 != null && GetFieldObject(objectType2, num4, "m_array", out var arrayType, out var array) && arrayType.IsArray)
					{
						int len2 = arrayType.GetArrayLength(array);
						for (int j = 0; j < len2; j = num2)
						{
							ulong num5 = (ulong)arrayType.GetArrayElementValue(array, i2);
							if (num5 != 0L)
							{
								yield return num5;
							}
							num2 = j + 1;
						}
						arrayType = null;
					}
				}
				num2 = i2 + 1;
				i2 = num2;
			}
		}
	}

	private ClrModule GetMscorlib()
	{
		foreach (ClrModule item in _runtime.EnumerateModules())
		{
			if (item.AssemblyName.Contains("mscorlib.dll"))
			{
				return item;
			}
		}
		foreach (ClrModule item2 in _runtime.EnumerateModules())
		{
			if (item2.AssemblyName.ToLower().Contains("mscorlib"))
			{
				return item2;
			}
		}
		return null;
	}

	private bool GetFieldObject(ClrType type, ulong obj, string fieldName, out ClrType valueType, out ulong value)
	{
		value = 0uL;
		valueType = null;
		ClrInstanceField fieldByName = type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			return false;
		}
		value = (ulong)fieldByName.GetValue(obj);
		if (value == 0L)
		{
			return false;
		}
		valueType = _heap.GetObjectType(value);
		return valueType != null;
	}
}
