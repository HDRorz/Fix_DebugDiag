using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

public class GCRoot
{
	private struct PathEntry
	{
		public ClrObject Object;

		public Stack<ClrObject> Todo;

		public override string ToString()
		{
			return Object.ToString();
		}
	}

	private static readonly Stack<ClrObject> s_emptyStack = new Stack<ClrObject>();

	private int _maxTasks;

	public ClrHeap Heap { get; }

	public bool AllowParallelSearch { get; set; } = true;

	public int MaximumTasksAllowed
	{
		get
		{
			return _maxTasks;
		}
		set
		{
			if (_maxTasks < 0)
			{
				throw new InvalidOperationException("MaximumTasksAllowed cannot be less than 0!");
			}
			_maxTasks = value;
		}
	}

	public bool IsFullyCached => Heap.AreRootsCached;

	public event GCRootProgressEvent ProgressUpdate;

	public GCRoot(ClrHeap heap)
	{
		Heap = heap ?? throw new ArgumentNullException("heap");
		_maxTasks = Environment.ProcessorCount * 2;
	}

	public IEnumerable<GCRootPath> EnumerateGCRoots(ulong target, CancellationToken cancelToken)
	{
		return EnumerateGCRoots(target, unique: true, cancelToken);
	}

	public IEnumerable<GCRootPath> EnumerateGCRoots(ulong target, bool unique, CancellationToken cancelToken)
	{
		Heap.BuildDependentHandleMap(cancelToken);
		long totalObjects = Heap.TotalObjects;
		long lastObjectReported = 0L;
		bool parallel = AllowParallelSearch && IsFullyCached && _maxTasks > 0;
		Dictionary<ulong, LinkedListNode<ClrObject>> knownEndPoints = new Dictionary<ulong, LinkedListNode<ClrObject>> { 
		{
			target,
			new LinkedListNode<ClrObject>(Heap.GetObject(target))
		} };
		ObjectSet processedObjects = (parallel ? new ParallelObjectSet(Heap) : new ObjectSet(Heap));
		Task<Tuple<LinkedList<ClrObject>, ClrRoot>>[] tasks = (parallel ? new Task<Tuple<LinkedList<ClrObject>, ClrRoot>>[_maxTasks] : null);
		int initial = 0;
		foreach (ClrHandle handle in Heap.EnumerateStrongHandles())
		{
			GCRootPath? gCRootPath = ProcessRoot(handle.Object, handle.Type, () => GetHandleRoot(handle));
			if (gCRootPath.HasValue)
			{
				yield return gCRootPath.Value;
			}
		}
		foreach (ClrRoot root in Heap.EnumerateStackRoots())
		{
			GCRootPath? gCRootPath2 = ProcessRoot(root.Object, root.Type, () => root);
			if (gCRootPath2.HasValue)
			{
				yield return gCRootPath2.Value;
			}
		}
		if (parallel)
		{
			foreach (Tuple<LinkedList<ClrObject>, ClrRoot> item in WhenEach(tasks))
			{
				ReportObjectCount(processedObjects.Count);
				yield return new GCRootPath
				{
					Root = item.Item2,
					Path = item.Item1.ToArray()
				};
			}
		}
		ReportObjectCount(totalObjects);
		GCRootPath? ProcessRoot(ulong rootRef, ClrType rootType, Func<ClrRoot> rootFunc)
		{
			ClrObject rootObject = ClrObject.Create(rootRef, rootType);
			GCRootPath? result = null;
			if (parallel)
			{
				Task<Tuple<LinkedList<ClrObject>, ClrRoot>> task = Task.Run(delegate
				{
					LinkedList<ClrObject> linkedList = PathsTo(processedObjects, knownEndPoints, rootObject, target, unique, cancelToken).FirstOrDefault();
					return new Tuple<LinkedList<ClrObject>, ClrRoot>(linkedList, (linkedList == null) ? null : rootFunc());
				}, cancelToken);
				if (initial < tasks.Length)
				{
					tasks[initial++] = task;
				}
				else
				{
					Task[] tasks2 = tasks;
					int num = Task.WaitAny(tasks2);
					Task<Tuple<LinkedList<ClrObject>, ClrRoot>> task2 = tasks[num];
					tasks[num] = task;
					if (task2.Result.Item1 != null)
					{
						result = new GCRootPath
						{
							Root = task2.Result.Item2,
							Path = task2.Result.Item1.ToArray()
						};
					}
				}
			}
			else
			{
				LinkedList<ClrObject> linkedList2 = PathsTo(processedObjects, knownEndPoints, rootObject, target, unique, cancelToken).FirstOrDefault();
				if (linkedList2 != null)
				{
					result = new GCRootPath
					{
						Root = rootFunc(),
						Path = linkedList2.ToArray()
					};
				}
			}
			ReportObjectCount(processedObjects.Count);
			return result;
		}
		void ReportObjectCount(long curr)
		{
			if (curr != lastObjectReported)
			{
				lastObjectReported = curr;
				this.ProgressUpdate?.Invoke(this, lastObjectReported, totalObjects);
			}
		}
	}

	private static IEnumerable<Tuple<LinkedList<ClrObject>, ClrRoot>> WhenEach(Task<Tuple<LinkedList<ClrObject>, ClrRoot>>[] tasks)
	{
		List<Task<Tuple<LinkedList<ClrObject>, ClrRoot>>> taskList = tasks.Where((Task<Tuple<LinkedList<ClrObject>, ClrRoot>> t) => t != null).ToList();
		while (taskList.Count > 0)
		{
			Task<Tuple<LinkedList<ClrObject>, ClrRoot>> task = Task.WhenAny(taskList).Result;
			if (task.Result.Item1 != null)
			{
				yield return task.Result;
			}
			taskList.Remove(task);
		}
	}

	public LinkedList<ClrObject> FindSinglePath(ulong source, ulong target, CancellationToken cancelToken)
	{
		Heap.BuildDependentHandleMap(cancelToken);
		return PathsTo(new ObjectSet(Heap), null, new ClrObject(source, Heap.GetObjectType(source)), target, unique: false, cancelToken).FirstOrDefault();
	}

	public IEnumerable<LinkedList<ClrObject>> EnumerateAllPaths(ulong source, ulong target, bool unique, CancellationToken cancelToken)
	{
		Heap.BuildDependentHandleMap(cancelToken);
		return PathsTo(new ObjectSet(Heap), new Dictionary<ulong, LinkedListNode<ClrObject>>(), new ClrObject(source, Heap.GetObjectType(source)), target, unique, cancelToken);
	}

	public void BuildCache(CancellationToken cancelToken)
	{
		Heap.CacheRoots(cancelToken);
		Heap.CacheHeap(cancelToken);
	}

	public void ClearCache()
	{
		Heap.ClearHeapCache();
		Heap.ClearRootCache();
	}

	private IEnumerable<LinkedList<ClrObject>> PathsTo(ObjectSet seen, Dictionary<ulong, LinkedListNode<ClrObject>> knownEndPoints, ClrObject source, ulong target, bool unique, CancellationToken cancelToken)
	{
		LinkedList<PathEntry> path = new LinkedList<PathEntry>();
		if (knownEndPoints != null && knownEndPoints.TryGetValue(source.Address, out var value))
		{
			yield return GetResult(value);
			yield break;
		}
		if (!seen.Add(source.Address))
		{
			yield return null;
		}
		if (source.Type == null)
		{
			yield break;
		}
		if (source.Address == target)
		{
			path.AddLast(new PathEntry
			{
				Object = source
			});
			yield return GetResult(null);
			yield break;
		}
		path.AddLast(new PathEntry
		{
			Object = source,
			Todo = GetRefs(source, out var found2, out var end2)
		});
		if (found2)
		{
			path.AddLast(new PathEntry
			{
				Object = Heap.GetObject(target)
			});
			yield return GetResult(null);
		}
		else if (end2 != null)
		{
			yield return GetResult(end2);
		}
		while (path.Count > 0)
		{
			cancelToken.ThrowIfCancellationRequested();
			PathEntry value2 = path.Last.Value;
			if (value2.Todo.Count == 0)
			{
				path.RemoveLast();
				continue;
			}
			do
			{
				cancelToken.ThrowIfCancellationRequested();
				ClrObject clrObject = value2.Todo.Pop();
				if (seen.Add(clrObject.Address))
				{
					PathEntry pathEntry = default(PathEntry);
					pathEntry.Object = clrObject;
					pathEntry.Todo = GetRefs(clrObject, out found2, out end2);
					PathEntry value3 = pathEntry;
					path.AddLast(value3);
					if (found2)
					{
						path.AddLast(new PathEntry
						{
							Object = Heap.GetObject(target)
						});
						yield return GetResult(null);
						path.RemoveLast();
						path.RemoveLast();
					}
					else if (end2 != null)
					{
						yield return GetResult(end2);
						path.RemoveLast();
					}
					break;
				}
			}
			while (value2.Todo.Count > 0);
		}
		Stack<ClrObject> GetRefs(ClrObject obj, out bool found, out LinkedListNode<ClrObject> end)
		{
			Stack<ClrObject> stack = null;
			found = false;
			end = null;
			if (obj.Type.ContainsPointers || obj.Type.IsCollectible)
			{
				foreach (ClrObject item in obj.EnumerateObjectReferences(carefully: true))
				{
					cancelToken.ThrowIfCancellationRequested();
					if (!unique && end == null && knownEndPoints != null)
					{
						lock (knownEndPoints)
						{
							knownEndPoints.TryGetValue(item.Address, out end);
						}
					}
					if (item.Address == target)
					{
						found = true;
					}
					if (!seen.Contains(item.Address))
					{
						stack = stack ?? new Stack<ClrObject>();
						stack.Push(item);
					}
				}
			}
			return stack ?? s_emptyStack;
		}
		LinkedList<ClrObject> GetResult(LinkedListNode<ClrObject> end)
		{
			LinkedList<ClrObject> linkedList = new LinkedList<ClrObject>(path.Select((PathEntry p) => p.Object));
			while (end != null)
			{
				linkedList.AddLast(end.Value);
				end = end.Next;
			}
			if (!unique && knownEndPoints != null)
			{
				lock (knownEndPoints)
				{
					for (LinkedListNode<ClrObject> linkedListNode = linkedList.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
					{
						ulong address = linkedListNode.Value.Address;
						if (knownEndPoints.ContainsKey(address))
						{
							break;
						}
						knownEndPoints[address] = linkedListNode;
					}
				}
			}
			return linkedList;
		}
	}

	private static ClrRoot GetHandleRoot(ClrHandle handle)
	{
		GCRootKind kind = GCRootKind.Strong;
		switch (handle.HandleType)
		{
		case HandleType.Pinned:
			kind = GCRootKind.Pinning;
			break;
		case HandleType.AsyncPinned:
			kind = GCRootKind.AsyncPinning;
			break;
		}
		return new HandleRoot(handle.Address, handle.Object, handle.Type, handle.HandleType, kind, handle.AppDomain);
	}

	internal static bool IsTooLarge(ulong obj, ClrType type, ClrSegment seg)
	{
		ulong size = type.GetSize(obj);
		if (!seg.IsLarge && size >= 85000)
		{
			return true;
		}
		return obj + size > seg.End;
	}

	[Conditional("GCROOTTRACE")]
	private static void TraceFullPath(LinkedList<PathEntry> path, LinkedListNode<ClrObject> foundEnding)
	{
	}

	private static List<string> NodeToList(LinkedListNode<ClrObject> tmp)
	{
		List<string> list = new List<string>();
		while (tmp != null)
		{
			list.Add(tmp.Value.ToString());
			tmp = tmp.Next;
		}
		return list;
	}

	[Conditional("GCROOTTRACE")]
	private void TraceCurrent(ulong next, IEnumerable<ulong> refs)
	{
	}

	[Conditional("GCROOTTRACE")]
	private static void TraceFullPath(string prefix, LinkedList<PathEntry> path)
	{
		prefix = (string.IsNullOrWhiteSpace(prefix) ? "" : (prefix + ": "));
	}
}
