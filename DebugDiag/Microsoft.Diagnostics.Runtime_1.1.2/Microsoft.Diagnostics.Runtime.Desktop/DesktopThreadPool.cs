using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopThreadPool : ClrThreadPool
{
	private readonly DesktopRuntimeBase _runtime;

	private ClrHeap _heap;

	public override int TotalThreads { get; }

	public override int RunningThreads { get; }

	public override int IdleThreads { get; }

	public override int MinThreads { get; }

	public override int MaxThreads { get; }

	public override int MinCompletionPorts { get; }

	public override int MaxCompletionPorts { get; }

	public override int CpuUtilization { get; }

	public override int FreeCompletionPortCount { get; }

	public override int MaxFreeCompletionPorts { get; }

	public DesktopThreadPool(DesktopRuntimeBase runtime, IThreadPoolData data)
	{
		_runtime = runtime;
		TotalThreads = data.TotalThreads;
		RunningThreads = data.RunningThreads;
		IdleThreads = data.IdleThreads;
		MinThreads = data.MinThreads;
		MaxThreads = data.MaxThreads;
		MinCompletionPorts = data.MinCP;
		MaxCompletionPorts = data.MaxCP;
		CpuUtilization = data.CPU;
		FreeCompletionPortCount = data.NumFreeCP;
		MaxFreeCompletionPorts = data.MaxFreeCP;
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
		_heap = _runtime.Heap;
		ClrModule mscorlib = GetMscorlib();
		if (mscorlib == null)
		{
			yield break;
		}
		ClrType typeByName = mscorlib.GetTypeByName("System.Threading.ThreadPoolGlobals");
		ulong workQueue;
		ClrType workQueueType;
		ulong queueHead;
		ClrType nodesType;
		ClrStaticField workQueueField;
		if (typeByName != null)
		{
			workQueueField = typeByName.GetStaticFieldByName("workQueue");
			if (workQueueField != null)
			{
				foreach (ClrAppDomain appDomain in _runtime.AppDomains)
				{
					object value = workQueueField.GetValue(appDomain);
					workQueue = ((value == null) ? 0 : ((ulong)value));
					workQueueType = _heap.GetObjectType(workQueue);
					if (workQueue == 0L || workQueueType == null)
					{
						continue;
					}
					ClrType queueHeadType;
					while (GetFieldObject(workQueueType, workQueue, "queueHead", out queueHeadType, out queueHead))
					{
						if (GetFieldObject(queueHeadType, queueHead, "nodes", out nodesType, out var nodes) && nodesType.IsArray)
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
						queueHeadType = null;
						nodesType = null;
						if (queueHead == 0L)
						{
							break;
						}
					}
					workQueueType = null;
				}
			}
		}
		typeByName = mscorlib.GetTypeByName("System.Threading.ThreadPoolWorkQueue");
		if (typeByName == null)
		{
			yield break;
		}
		workQueueField = typeByName.GetStaticFieldByName("allThreadQueues");
		if (workQueueField == null)
		{
			yield break;
		}
		foreach (ClrAppDomain appDomain2 in _runtime.AppDomains)
		{
			ulong? num3 = (ulong?)workQueueField.GetValue(appDomain2);
			if (!num3.HasValue || num3.Value == 0L)
			{
				continue;
			}
			ClrType objectType = _heap.GetObjectType(num3.Value);
			if (objectType == null || !GetFieldObject(objectType, num3.Value, "m_array", out workQueueType, out queueHead) || !workQueueType.IsArray)
			{
				continue;
			}
			int len = workQueueType.GetArrayLength(queueHead);
			int i = 0;
			while (i < len)
			{
				ulong num4 = (ulong)workQueueType.GetArrayElementValue(queueHead, i);
				int num2;
				if (num4 != 0L)
				{
					ClrType objectType2 = _heap.GetObjectType(num4);
					if (objectType2 != null && GetFieldObject(objectType2, num4, "m_array", out nodesType, out workQueue) && nodesType.IsArray)
					{
						int len2 = nodesType.GetArrayLength(workQueue);
						for (int j = 0; j < len2; j = num2)
						{
							ulong num5 = (ulong)nodesType.GetArrayElementValue(workQueue, i);
							if (num5 != 0L)
							{
								yield return num5;
							}
							num2 = j + 1;
						}
						nodesType = null;
					}
				}
				num2 = i + 1;
				i = num2;
			}
			workQueueType = null;
		}
	}

	private ClrModule GetMscorlib()
	{
		foreach (ClrModule module in _runtime.Modules)
		{
			if (module.AssemblyName.Contains("mscorlib.dll"))
			{
				return module;
			}
		}
		foreach (ClrModule module2 in _runtime.Modules)
		{
			if (module2.AssemblyName.ToLower().Contains("mscorlib"))
			{
				return module2;
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
