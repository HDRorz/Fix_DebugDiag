#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime.DacInterface;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal abstract class DesktopGCHeap : HeapBase
{
	private class ModuleEntryCompare : IEqualityComparer<ModuleEntry>
	{
		public bool Equals(ModuleEntry mx, ModuleEntry my)
		{
			if (mx.Token == my.Token)
			{
				return mx.Module == my.Module;
			}
			return false;
		}

		public int GetHashCode(ModuleEntry obj)
		{
			return (int)obj.Token;
		}
	}

	private struct ObjectInfo
	{
		public ClrType Type;

		public uint RefOffset;

		public uint RefCount;

		public override string ToString()
		{
			return $"{Type.Name} refs: {RefCount:n0}";
		}
	}

	internal class ExtendedArray<T>
	{
		private const int Initial = 1048576;

		private const int Secondary = 16777216;

		private const int Complete = 67108864;

		private readonly List<T[]> _lists = new List<T[]>();

		private int _curr;

		public long Count
		{
			get
			{
				if (_lists.Count <= 0)
				{
					return 0L;
				}
				return (long)(_lists.Count - 1) * 67108864L + _curr;
			}
		}

		public T this[int index]
		{
			get
			{
				int index2 = index / 67108864;
				index %= 67108864;
				return _lists[index2][index];
			}
			set
			{
				int index2 = index / 67108864;
				index %= 67108864;
				_lists[index2][index] = value;
			}
		}

		public T this[long index]
		{
			get
			{
				long num = index / 67108864;
				index %= 67108864;
				return _lists[(int)num][index];
			}
			set
			{
				long num = index / 67108864;
				index %= 67108864;
				_lists[(int)num][index] = value;
			}
		}

		public void Add(T t)
		{
			T[] array = _lists.LastOrDefault();
			if (array == null || _curr == 67108864)
			{
				array = new T[1048576];
				_lists.Add(array);
				_curr = 0;
			}
			if (_curr >= array.Length)
			{
				if (array.Length == 67108864)
				{
					array = new T[1048576];
					_lists.Add(array);
					_curr = 0;
				}
				else
				{
					int newSize = ((array.Length == 1048576) ? 16777216 : 67108864);
					_lists.RemoveAt(_lists.Count - 1);
					Array.Resize(ref array, newSize);
					_lists.Add(array);
				}
			}
			array[_curr++] = t;
		}

		public void Condense()
		{
			T[] array = _lists.LastOrDefault();
			if (array != null && _curr < array.Length)
			{
				_lists.RemoveAt(_lists.Count - 1);
				Array.Resize(ref array, _curr);
				_lists.Add(array);
			}
		}
	}

	internal class DictionaryList
	{
		private class Entry
		{
			public ulong Start;

			public ulong End;

			public SortedDictionary<ulong, int> Dictionary;
		}

		private const int MaxEntries = 40000000;

		private readonly List<Entry> _entries = new List<Entry>();

		public IEnumerable<KeyValuePair<ulong, int>> Enumerate()
		{
			return _entries.SelectMany((Entry e) => e.Dictionary);
		}

		public void Add(ulong obj, int index)
		{
			Entry orCreateEntry = GetOrCreateEntry(obj);
			orCreateEntry.End = obj;
			orCreateEntry.Dictionary.Add(obj, index);
		}

		public bool TryGetValue(ulong obj, out int index)
		{
			foreach (Entry entry in _entries)
			{
				if (entry.Start <= obj && obj <= entry.End)
				{
					return entry.Dictionary.TryGetValue(obj, out index);
				}
			}
			index = 0;
			return false;
		}

		private Entry GetOrCreateEntry(ulong obj)
		{
			if (_entries.Count == 0)
			{
				return NewEntry(obj);
			}
			Entry entry = _entries.Last();
			if (entry.Dictionary.Count > 40000000)
			{
				return NewEntry(obj);
			}
			return entry;
		}

		private Entry NewEntry(ulong obj)
		{
			Entry entry = new Entry
			{
				Start = obj,
				End = obj,
				Dictionary = new SortedDictionary<ulong, int>()
			};
			_entries.Add(entry);
			return entry;
		}
	}

	[Obsolete]
	private BlockingObject[] _managedLocks;

	private ClrRootStackwalkPolicy _stackwalkPolicy;

	private ClrRootStackwalkPolicy _currentStackCache = ClrRootStackwalkPolicy.SkipStack;

	private Dictionary<ClrThread, ClrRoot[]> _stackCache;

	private ClrHandle[] _strongHandles;

	private Dictionary<ulong, List<ulong>> _dependentHandles;

	protected List<ClrType> _types;

	protected Dictionary<ModuleEntry, int> _typeEntry = new Dictionary<ModuleEntry, int>(new ModuleEntryCompare());

	private Dictionary<ArrayRankHandle, BaseDesktopHeapType> _arrayTypes;

	private ClrInstanceField _firstChar;

	private ClrInstanceField _stringLength;

	private bool _initializedStringFields;

	private ClrType[] _basicTypes;

	internal readonly ClrInterface[] EmptyInterfaceList = new ClrInterface[0];

	internal Dictionary<string, ClrInterface> Interfaces = new Dictionary<string, ClrInterface>();

	private readonly Lazy<ClrType> _arrayType;

	private readonly Lazy<ClrType> _exceptionType;

	private DictionaryList _objectMap;

	private ExtendedArray<ObjectInfo> _objects;

	private ExtendedArray<ulong> _gcRefs;

	public override ClrRuntime Runtime => DesktopRuntime;

	public override ClrRootStackwalkPolicy StackwalkPolicy
	{
		get
		{
			return _stackwalkPolicy;
		}
		set
		{
			if (value != _currentStackCache)
			{
				_stackCache = null;
			}
			_stackwalkPolicy = value;
		}
	}

	public override bool AreRootsCached
	{
		get
		{
			if (_stackwalkPolicy == ClrRootStackwalkPolicy.SkipStack || (_stackCache != null && _currentStackCache == StackwalkPolicy))
			{
				return _strongHandles != null;
			}
			return false;
		}
	}

	internal bool TypesLoaded { get; private set; }

	internal DesktopRuntimeBase DesktopRuntime { get; }

	internal BaseDesktopHeapType ErrorType { get; }

	internal ClrType ObjectType { get; }

	internal ClrType StringType { get; }

	internal ClrType ValueType { get; private set; }

	internal ClrType ArrayType => _arrayType.Value;

	public override ClrType Free { get; }

	internal ClrType ExceptionType => _exceptionType.Value;

	internal ClrType EnumType { get; set; }

	protected internal override long TotalObjects => _objects?.Count ?? (-1);

	public override bool IsHeapCached => _objectMap != null;

	public DesktopGCHeap(DesktopRuntimeBase runtime)
		: base(runtime)
	{
		DesktopRuntime = runtime;
		_types = new List<ClrType>(1000);
		base.Revision = runtime.Revision;
		ErrorType = new ErrorType(this);
		_arrayType = new Lazy<ClrType>(CreateArrayType);
		_exceptionType = new Lazy<ClrType>(() => GetTypeByMethodTable(DesktopRuntime.ExceptionMethodTable, 0uL, 0uL) ?? ErrorType);
		StringType = GetTypeByMethodTable(DesktopRuntime.StringMethodTable, 0uL, 0uL) ?? ErrorType;
		ObjectType = GetTypeByMethodTable(DesktopRuntime.ObjectMethodTable, 0uL, 0uL) ?? ErrorType;
		Free = CreateFree();
		InitSegments(runtime);
	}

	private ClrType CreateFree()
	{
		ClrType typeByMethodTable = GetTypeByMethodTable(DesktopRuntime.FreeMethodTable, 0uL, 0uL);
		if (typeByMethodTable == null)
		{
			return ErrorType;
		}
		((DesktopHeapType)typeByMethodTable).Shared = true;
		((BaseDesktopHeapType)typeByMethodTable).DesktopModule = (DesktopModule)ObjectType.Module;
		return typeByMethodTable;
	}

	private ClrType CreateArrayType()
	{
		ClrType typeByMethodTable = GetTypeByMethodTable(DesktopRuntime.ArrayMethodTable, DesktopRuntime.ObjectMethodTable, 0uL);
		if (typeByMethodTable == null)
		{
			return ErrorType;
		}
		typeByMethodTable.ComponentType = ObjectType;
		return typeByMethodTable;
	}

	protected override int GetRuntimeRevision()
	{
		return DesktopRuntime.Revision;
	}

	public override ClrException GetExceptionObject(ulong objRef)
	{
		ClrType objectType = GetObjectType(objRef);
		if (objectType == null)
		{
			return null;
		}
		if (!objectType.IsException)
		{
			return null;
		}
		return new DesktopException(objRef, (BaseDesktopHeapType)objectType);
	}

	public override ulong GetEEClassByMethodTable(ulong methodTable)
	{
		if (methodTable == 0L)
		{
			return 0uL;
		}
		return DesktopRuntime.GetMethodTableData(methodTable)?.EEClass ?? 0;
	}

	public override ulong GetMethodTableByEEClass(ulong eeclass)
	{
		if (eeclass == 0L)
		{
			return 0uL;
		}
		return DesktopRuntime.GetMethodTableByEEClass(eeclass);
	}

	public override bool TryGetMethodTable(ulong obj, out ulong methodTable, out ulong componentMethodTable)
	{
		componentMethodTable = 0uL;
		if (!ReadPointer(obj, out methodTable))
		{
			return false;
		}
		if (methodTable == DesktopRuntime.ArrayMethodTable && !ReadPointer(obj + (ulong)(IntPtr.Size * 2), out componentMethodTable))
		{
			return false;
		}
		return true;
	}

	protected override MemoryReader GetMemoryReaderForAddress(ulong obj)
	{
		if (base.MemoryReader.Contains(obj))
		{
			return base.MemoryReader;
		}
		return DesktopRuntime.MemoryReader;
	}

	internal ClrType GetGCHeapTypeFromModuleAndToken(ulong moduleAddr, uint token)
	{
		DesktopModule module = DesktopRuntime.GetModule(moduleAddr);
		ModuleEntry key = new ModuleEntry(module, token);
		if (_typeEntry.TryGetValue(key, out var value))
		{
			BaseDesktopHeapType baseDesktopHeapType = (BaseDesktopHeapType)_types[value];
			if (baseDesktopHeapType.MetadataToken == token)
			{
				return baseDesktopHeapType;
			}
		}
		else
		{
			foreach (ClrType item in module.EnumerateTypes())
			{
				if (item.MetadataToken == token)
				{
					return item;
				}
			}
		}
		return null;
	}

	internal abstract ClrType GetTypeByMethodTable(ulong mt, ulong cmt, ulong obj);

	protected ClrType TryGetComponentType(ulong obj, ulong cmt)
	{
		ClrType clrType = null;
		IObjectData objectData = GetObjectData(obj);
		if (objectData != null)
		{
			if (objectData.ElementTypeHandle != 0L)
			{
				clrType = GetTypeByMethodTable(objectData.ElementTypeHandle, 0uL, 0uL);
			}
			if (clrType == null && objectData.ElementType != 0)
			{
				clrType = GetBasicType(objectData.ElementType);
			}
		}
		else if (cmt != 0L)
		{
			clrType = GetTypeByMethodTable(cmt, 0uL);
		}
		return clrType;
	}

	protected static string GetTypeNameFromToken(DesktopModule module, uint token)
	{
		if (module == null)
		{
			return null;
		}
		MetaDataImport metadataImport = module.GetMetadataImport();
		if (metadataImport == null)
		{
			return null;
		}
		if (!metadataImport.GetTypeDefProperties((int)token, out var name, out var _, out var _))
		{
			return null;
		}
		if (metadataImport.GetNestedClassProperties((int)token, out var enclosing) && token != enclosing)
		{
			string text = GetTypeNameFromToken(module, (uint)enclosing);
			if (text == null)
			{
				text = "<UNKNOWN>";
			}
			return text + "+" + name;
		}
		return name;
	}

	public override IEnumerable<ulong> EnumerateFinalizableObjectAddresses()
	{
		if (!DesktopRuntime.GetHeaps(out var heaps))
		{
			yield break;
		}
		SubHeap[] array = heaps;
		foreach (SubHeap subHeap in array)
		{
			foreach (ulong item in DesktopRuntime.GetPointersInRange(subHeap.FQAllObjectsStart, subHeap.FQAllObjectsStop))
			{
				if (item != 0L)
				{
					ClrType objectType = GetObjectType(item);
					if (objectType != null && !objectType.IsFinalizeSuppressed(item))
					{
						yield return item;
					}
				}
			}
		}
	}

	[Obsolete]
	public override IEnumerable<BlockingObject> EnumerateBlockingObjects()
	{
		InitLockInspection();
		return _managedLocks;
	}

	[Obsolete]
	internal void InitLockInspection()
	{
		if (_managedLocks == null)
		{
			LockInspection lockInspection = new LockInspection(this, DesktopRuntime);
			BlockingObject[] managedLocks = lockInspection.InitLockInspection();
			_managedLocks = managedLocks;
		}
	}

	public override void CacheRoots(CancellationToken cancelToken)
	{
		if (StackwalkPolicy != ClrRootStackwalkPolicy.SkipStack && (_stackCache == null || _currentStackCache != StackwalkPolicy))
		{
			Dictionary<ClrThread, ClrRoot[]> dictionary = new Dictionary<ClrThread, ClrRoot[]>();
			bool exactPolicy = ClrThread.GetExactPolicy(Runtime, StackwalkPolicy);
			foreach (ClrThread thread in Runtime.Threads)
			{
				cancelToken.ThrowIfCancellationRequested();
				if (thread.IsAlive)
				{
					dictionary.Add(thread, thread.EnumerateStackObjects(!exactPolicy).ToArray());
				}
			}
			_stackCache = dictionary;
			_currentStackCache = StackwalkPolicy;
		}
		if (_strongHandles == null)
		{
			CacheStrongHandles(cancelToken);
		}
	}

	private void CacheStrongHandles(CancellationToken cancelToken)
	{
		_strongHandles = (from k in EnumerateStrongHandlesWorker(cancelToken)
			orderby GetHandleOrder(k.HandleType)
			select k).ToArray();
	}

	public override void ClearRootCache()
	{
		_currentStackCache = ClrRootStackwalkPolicy.SkipStack;
		_stackCache = null;
		_strongHandles = null;
		_dependentHandles = null;
	}

	private static int GetHandleOrder(HandleType handleType)
	{
		return handleType switch
		{
			HandleType.AsyncPinned => 0, 
			HandleType.Pinned => 1, 
			HandleType.Strong => 2, 
			HandleType.RefCount => 3, 
			_ => 4, 
		};
	}

	protected internal override IEnumerable<ClrHandle> EnumerateStrongHandles()
	{
		if (_strongHandles != null)
		{
			return _strongHandles;
		}
		return EnumerateStrongHandlesWorker(CancellationToken.None);
	}

	protected internal override void BuildDependentHandleMap(CancellationToken cancelToken)
	{
		if (_dependentHandles == null)
		{
			_dependentHandles = DesktopRuntime.GetDependentHandleMap(cancelToken);
		}
	}

	private IEnumerable<ClrHandle> EnumerateStrongHandlesWorker(CancellationToken cancelToken)
	{
		Dictionary<ulong, List<ulong>> dependentHandles = null;
		if (_dependentHandles == null)
		{
			dependentHandles = new Dictionary<ulong, List<ulong>>();
		}
		foreach (ClrHandle item in Runtime.EnumerateHandles())
		{
			cancelToken.ThrowIfCancellationRequested();
			if (item.Object == 0L)
			{
				continue;
			}
			switch (item.HandleType)
			{
			case HandleType.RefCount:
				if (item.RefCount != 0)
				{
					yield return item;
				}
				break;
			case HandleType.Strong:
			case HandleType.Pinned:
			case HandleType.AsyncPinned:
			case HandleType.SizedRef:
				yield return item;
				break;
			case HandleType.Dependent:
				if (dependentHandles != null)
				{
					if (!dependentHandles.TryGetValue(item.Object, out var value))
					{
						value = (dependentHandles[item.Object] = new List<ulong>());
					}
					value.Add(item.DependentTarget);
				}
				break;
			}
		}
		if (dependentHandles != null)
		{
			_dependentHandles = dependentHandles;
		}
	}

	protected internal override IEnumerable<ClrRoot> EnumerateStackRoots()
	{
		if (StackwalkPolicy != ClrRootStackwalkPolicy.SkipStack)
		{
			if (_stackCache != null && _currentStackCache == StackwalkPolicy)
			{
				return _stackCache.SelectMany((KeyValuePair<ClrThread, ClrRoot[]> t) => t.Value);
			}
			return EnumerateStackRootsWorker();
		}
		return new ClrRoot[0];
	}

	private IEnumerable<ClrRoot> EnumerateStackRootsWorker()
	{
		bool exactStackwalk = ClrThread.GetExactPolicy(Runtime, StackwalkPolicy);
		foreach (ClrThread thread in DesktopRuntime.Threads)
		{
			if (!thread.IsAlive)
			{
				continue;
			}
			if (exactStackwalk)
			{
				foreach (ClrRoot item in thread.EnumerateStackObjects(includePossiblyDead: false))
				{
					yield return item;
				}
				continue;
			}
			HashSet<ulong> seen = new HashSet<ulong>();
			foreach (ClrRoot item2 in thread.EnumerateStackObjects(includePossiblyDead: true))
			{
				if (!seen.Contains(item2.Object))
				{
					seen.Add(item2.Object);
					yield return item2;
				}
			}
		}
	}

	public override IEnumerable<ClrRoot> EnumerateRoots()
	{
		return EnumerateRoots(enumerateStatics: true);
	}

	public override IEnumerable<ClrRoot> EnumerateRoots(bool enumerateStatics)
	{
		if (enumerateStatics)
		{
			foreach (ClrType type in EnumerateTypes())
			{
				foreach (ClrStaticField staticField in type.StaticFields)
				{
					if (staticField.ElementType.IsPrimitive())
					{
						continue;
					}
					{
						foreach (ClrAppDomain appDomain in DesktopRuntime.AppDomains)
						{
							ulong address;
							try
							{
								address = staticField.GetAddress(appDomain);
							}
							catch (Exception ex)
							{
								Trace.WriteLine("Error getting stack field " + type.Name + "." + staticField.Name + ": " + ex.Message);
								goto end_IL_00e2;
							}
							if (DesktopRuntime.ReadPointer(address, out var value) && value != 0L)
							{
								ClrType objectType = GetObjectType(value);
								if (objectType != null)
								{
									yield return new StaticVarRoot(address, value, objectType, type.Name, staticField.Name, appDomain);
								}
							}
						}
						end_IL_00e2:;
					}
				}
				foreach (ClrThreadStaticField tsf in type.ThreadStaticFields)
				{
					if (!tsf.ElementType.IsObjectReference())
					{
						continue;
					}
					foreach (ClrAppDomain ad in DesktopRuntime.AppDomains)
					{
						foreach (ClrThread thread in DesktopRuntime.Threads)
						{
							ulong address2 = tsf.GetAddress(ad, thread);
							if (DesktopRuntime.ReadPointer(address2, out var value2) && value2 != 0L)
							{
								ClrType objectType2 = GetObjectType(value2);
								if (objectType2 != null)
								{
									yield return new ThreadStaticVarRoot(address2, value2, objectType2, type.Name, tsf.Name, ad);
								}
							}
						}
					}
				}
			}
		}
		foreach (ClrHandle handle in EnumerateStrongHandles())
		{
			ulong objAddr = handle.Object;
			GCRootKind kind = GCRootKind.Strong;
			if (objAddr == 0L)
			{
				continue;
			}
			ClrType type = GetObjectType(objAddr);
			if (type == null)
			{
				continue;
			}
			switch (handle.HandleType)
			{
			case HandleType.RefCount:
				if (handle.RefCount == 0)
				{
					continue;
				}
				break;
			case HandleType.Pinned:
				kind = GCRootKind.Pinning;
				break;
			case HandleType.AsyncPinned:
				kind = GCRootKind.AsyncPinning;
				break;
			case HandleType.Strong:
			case HandleType.SizedRef:
				break;
			default:
				continue;
			}
			yield return new HandleRoot(handle.Address, objAddr, type, handle.HandleType, kind, handle.AppDomain);
			if (handle.HandleType != HandleType.AsyncPinned)
			{
				continue;
			}
			ClrInstanceField fieldByName = type.GetFieldByName("m_userObject");
			if (fieldByName == null)
			{
				continue;
			}
			ulong address3 = fieldByName.GetAddress(objAddr);
			ulong _userObj = (ulong)fieldByName.GetValue(objAddr);
			ClrType _userObjType = GetObjectType(_userObj);
			if (_userObjType == null)
			{
				continue;
			}
			if (_userObjType.IsArray)
			{
				if (_userObjType.ComponentType == null)
				{
					continue;
				}
				if (_userObjType.ComponentType.ElementType == ClrElementType.Object)
				{
					int len = _userObjType.GetArrayLength(_userObj);
					int i = 0;
					while (i < len)
					{
						ulong arrayElementAddress = _userObjType.GetArrayElementAddress(_userObj, i);
						ulong num = (ulong)_userObjType.GetArrayElementValue(_userObj, i);
						ClrType objectType3 = GetObjectType(num);
						if (num != 0L && objectType3 != null)
						{
							yield return new HandleRoot(arrayElementAddress, num, objectType3, HandleType.AsyncPinned, GCRootKind.AsyncPinning, handle.AppDomain);
						}
						int num2 = i + 1;
						i = num2;
					}
				}
				else
				{
					yield return new HandleRoot(address3, _userObj, _userObjType, HandleType.AsyncPinned, GCRootKind.AsyncPinning, handle.AppDomain);
				}
			}
			else
			{
				yield return new HandleRoot(address3, _userObj, _userObjType, HandleType.AsyncPinned, GCRootKind.AsyncPinning, handle.AppDomain);
			}
		}
		foreach (ulong item in DesktopRuntime.EnumerateFinalizerQueueObjectAddresses())
		{
			if (item != 0L)
			{
				ClrType objectType4 = GetObjectType(item);
				if (objectType4 != null)
				{
					yield return new FinalizerRoot(item, objectType4);
				}
			}
		}
		foreach (ClrRoot item2 in EnumerateStackRoots())
		{
			yield return item2;
		}
	}

	internal string GetStringContents(ulong strAddr)
	{
		if (strAddr == 0L)
		{
			return null;
		}
		if (!_initializedStringFields)
		{
			_firstChar = StringType.GetFieldByName("m_firstChar");
			_stringLength = StringType.GetFieldByName("m_stringLength");
			if (_firstChar?.Type == ErrorType)
			{
				_firstChar = null;
			}
			if (_stringLength?.Type == ErrorType)
			{
				_stringLength = null;
			}
			_initializedStringFields = true;
		}
		int value = 0;
		if (_stringLength != null)
		{
			value = (int)_stringLength.GetValue(strAddr);
		}
		else if (!DesktopRuntime.ReadDword(strAddr + DesktopRuntime.GetStringLengthOffset(), out value))
		{
			return null;
		}
		if (value == 0)
		{
			return "";
		}
		ulong num = 0uL;
		num = ((_firstChar == null) ? (strAddr + DesktopRuntime.GetStringFirstCharOffset()) : _firstChar.GetAddress(strAddr));
		byte[] array = new byte[value * 2];
		if (!DesktopRuntime.ReadMemory(num, array, array.Length, out var _))
		{
			return null;
		}
		return Encoding.Unicode.GetString(array);
	}

	public override int ReadMemory(ulong address, byte[] buffer, int offset, int count)
	{
		if (offset != 0)
		{
			throw new NotImplementedException("Non-zero offsets not supported (yet)");
		}
		if (!DesktopRuntime.ReadMemory(address, buffer, count, out var bytesRead))
		{
			return 0;
		}
		return bytesRead;
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		LoadAllTypes();
		int i = 0;
		while (i < _types.Count)
		{
			yield return _types[i];
			int num = i + 1;
			i = num;
		}
	}

	internal void LoadAllTypes()
	{
		if (TypesLoaded)
		{
			return;
		}
		TypesLoaded = true;
		HashSet<ulong> hashSet = new HashSet<ulong>();
		if (DesktopRuntime.SystemDomain != null)
		{
			foreach (ulong item in DesktopRuntime.EnumerateModules(DesktopRuntime.GetAppDomainData(DesktopRuntime.SystemDomain.Address)))
			{
				hashSet.Add(item);
			}
		}
		if (DesktopRuntime.SharedDomain != null)
		{
			foreach (ulong item2 in DesktopRuntime.EnumerateModules(DesktopRuntime.GetAppDomainData(DesktopRuntime.SharedDomain.Address)))
			{
				hashSet.Add(item2);
			}
		}
		IAppDomainStoreData appDomainStoreData = DesktopRuntime.GetAppDomainStoreData();
		if (appDomainStoreData == null)
		{
			return;
		}
		ulong[] appDomainList = DesktopRuntime.GetAppDomainList(appDomainStoreData.Count);
		if (appDomainList == null)
		{
			return;
		}
		ulong[] array = appDomainList;
		foreach (ulong addr in array)
		{
			IAppDomainData appDomainData = DesktopRuntime.GetAppDomainData(addr);
			if (appDomainData == null)
			{
				continue;
			}
			foreach (ulong item3 in DesktopRuntime.EnumerateModules(appDomainData))
			{
				hashSet.Add(item3);
			}
		}
		ulong arrayMethodTable = DesktopRuntime.ArrayMethodTable;
		foreach (ulong item4 in hashSet)
		{
			IList<MethodTableTokenPair> methodTableList = DesktopRuntime.GetMethodTableList(item4);
			if (methodTableList == null)
			{
				continue;
			}
			foreach (MethodTableTokenPair item5 in methodTableList)
			{
				if (item5.MethodTable != arrayMethodTable)
				{
					_ = GetTypeByMethodTable(item5.MethodTable, 0uL, 0uL)?.ElementType;
				}
			}
		}
	}

	internal bool GetObjectHeader(ulong obj, out uint value)
	{
		return base.MemoryReader.TryReadDword(obj - 4, out value);
	}

	internal IObjectData GetObjectData(ulong address)
	{
		return DesktopRuntime.GetObjectData(address);
	}

	internal object GetValueAtAddress(ClrElementType cet, ulong addr)
	{
		switch (cet)
		{
		case ClrElementType.String:
			return GetStringContents(addr);
		case ClrElementType.Class:
		case ClrElementType.Array:
		case ClrElementType.Object:
		case ClrElementType.SZArray:
		{
			if (!base.MemoryReader.TryReadPtr(addr, out var value14))
			{
				return null;
			}
			return value14;
		}
		case ClrElementType.Boolean:
		{
			if (!DesktopRuntime.ReadByte(addr, out byte value7))
			{
				return null;
			}
			return value7 != 0;
		}
		case ClrElementType.Int32:
		{
			if (!DesktopRuntime.ReadDword(addr, out int value11))
			{
				return null;
			}
			return value11;
		}
		case ClrElementType.UInt32:
		{
			if (!DesktopRuntime.ReadDword(addr, out uint value2))
			{
				return null;
			}
			return value2;
		}
		case ClrElementType.Int64:
		{
			if (!DesktopRuntime.ReadQword(addr, out long value10))
			{
				return long.MaxValue;
			}
			return value10;
		}
		case ClrElementType.UInt64:
		{
			if (!DesktopRuntime.ReadQword(addr, out ulong value3))
			{
				return long.MaxValue;
			}
			return value3;
		}
		case ClrElementType.Pointer:
		case ClrElementType.NativeUInt:
		case ClrElementType.FunctionPointer:
		{
			if (!base.MemoryReader.TryReadPtr(addr, out var value13))
			{
				return null;
			}
			return value13;
		}
		case ClrElementType.NativeInt:
		{
			if (!base.MemoryReader.TryReadPtr(addr, out var value8))
			{
				return null;
			}
			return (long)value8;
		}
		case ClrElementType.Int8:
		{
			if (!DesktopRuntime.ReadByte(addr, out sbyte value5))
			{
				return null;
			}
			return value5;
		}
		case ClrElementType.UInt8:
		{
			if (!DesktopRuntime.ReadByte(addr, out byte value15))
			{
				return null;
			}
			return value15;
		}
		case ClrElementType.Float:
		{
			if (!DesktopRuntime.ReadFloat(addr, out float value12))
			{
				return null;
			}
			return value12;
		}
		case ClrElementType.Double:
		{
			if (!DesktopRuntime.ReadFloat(addr, out double value9))
			{
				return null;
			}
			return value9;
		}
		case ClrElementType.Int16:
		{
			if (!DesktopRuntime.ReadShort(addr, out short value6))
			{
				return null;
			}
			return value6;
		}
		case ClrElementType.Char:
		{
			if (!DesktopRuntime.ReadShort(addr, out ushort value4))
			{
				return null;
			}
			return (char)value4;
		}
		case ClrElementType.UInt16:
		{
			if (!DesktopRuntime.ReadShort(addr, out ushort value))
			{
				return null;
			}
			return value;
		}
		default:
			throw new Exception("Unexpected element type.");
		}
	}

	internal ClrElementType GetElementType(BaseDesktopHeapType type, int depth)
	{
		if (depth >= 32)
		{
			return ClrElementType.Object;
		}
		if (type == ObjectType)
		{
			return ClrElementType.Object;
		}
		if (type == StringType)
		{
			return ClrElementType.String;
		}
		if (type.ElementSize > 0)
		{
			return ClrElementType.SZArray;
		}
		BaseDesktopHeapType baseDesktopHeapType = (BaseDesktopHeapType)type.BaseType;
		if (baseDesktopHeapType == null || baseDesktopHeapType == ObjectType)
		{
			return ClrElementType.Object;
		}
		bool flag = false;
		if (ValueType == null)
		{
			if (baseDesktopHeapType.Name == "System.ValueType")
			{
				ValueType = baseDesktopHeapType;
				flag = true;
			}
		}
		else if (baseDesktopHeapType == ValueType)
		{
			flag = true;
		}
		if (!flag)
		{
			ClrElementType clrElementType = baseDesktopHeapType.ElementType;
			if (clrElementType == ClrElementType.Unknown)
			{
				clrElementType = (baseDesktopHeapType.ElementType = GetElementType(baseDesktopHeapType, depth + 1));
			}
			return clrElementType;
		}
		return type.Name switch
		{
			"System.Int32" => ClrElementType.Int32, 
			"System.Int16" => ClrElementType.Int16, 
			"System.Int64" => ClrElementType.Int64, 
			"System.IntPtr" => ClrElementType.NativeInt, 
			"System.UInt16" => ClrElementType.UInt16, 
			"System.UInt32" => ClrElementType.UInt32, 
			"System.UInt64" => ClrElementType.UInt64, 
			"System.UIntPtr" => ClrElementType.NativeUInt, 
			"System.Boolean" => ClrElementType.Boolean, 
			"System.Single" => ClrElementType.Float, 
			"System.Double" => ClrElementType.Double, 
			"System.Byte" => ClrElementType.UInt8, 
			"System.Char" => ClrElementType.Char, 
			"System.SByte" => ClrElementType.Int8, 
			"System.Enum" => ClrElementType.Int32, 
			_ => ClrElementType.Struct, 
		};
	}

	internal ClrType GetBasicType(ClrElementType elType)
	{
		if (_basicTypes == null)
		{
			switch (elType)
			{
			case ClrElementType.String:
				return StringType;
			case ClrElementType.Array:
			case ClrElementType.SZArray:
				return ArrayType;
			case ClrElementType.Class:
			case ClrElementType.Object:
				return ObjectType;
			case ClrElementType.Struct:
				if (ValueType != null)
				{
					return ValueType;
				}
				break;
			}
		}
		if (_basicTypes == null)
		{
			InitBasicTypes();
		}
		if (_basicTypes[(int)elType] == null && DesktopRuntime.DataReader.IsMinidump)
		{
			switch (elType)
			{
			case ClrElementType.Boolean:
			case ClrElementType.Char:
			case ClrElementType.Int8:
			case ClrElementType.UInt8:
			case ClrElementType.Int16:
			case ClrElementType.UInt16:
			case ClrElementType.Int32:
			case ClrElementType.UInt32:
			case ClrElementType.Int64:
			case ClrElementType.UInt64:
			case ClrElementType.Float:
			case ClrElementType.Double:
			case ClrElementType.Pointer:
			case ClrElementType.NativeInt:
			case ClrElementType.NativeUInt:
			case ClrElementType.FunctionPointer:
				_basicTypes[(int)elType] = new PrimitiveType(this, elType);
				break;
			}
		}
		return _basicTypes[(int)elType];
	}

	private void InitBasicTypes()
	{
		_basicTypes = new ClrType[30];
		_basicTypes[0] = null;
		_basicTypes[14] = StringType;
		_basicTypes[20] = ArrayType;
		_basicTypes[29] = ArrayType;
		_basicTypes[28] = ObjectType;
		_basicTypes[18] = ObjectType;
		ClrModule mscorlib = DesktopRuntime.Mscorlib;
		if (mscorlib == null)
		{
			return;
		}
		int num = 0;
		foreach (ClrType item in mscorlib.EnumerateTypes())
		{
			if (num == 14)
			{
				break;
			}
			switch (item.Name)
			{
			case "System.ValueType":
				_basicTypes[17] = item;
				num++;
				break;
			case "System.Boolean":
				_basicTypes[2] = item;
				num++;
				break;
			case "System.Char":
				_basicTypes[3] = item;
				num++;
				break;
			case "System.SByte":
				_basicTypes[4] = item;
				num++;
				break;
			case "System.Byte":
				_basicTypes[5] = item;
				num++;
				break;
			case "System.Int16":
				_basicTypes[6] = item;
				num++;
				break;
			case "System.UInt16":
				_basicTypes[7] = item;
				num++;
				break;
			case "System.Int32":
				_basicTypes[8] = item;
				num++;
				break;
			case "System.UInt32":
				_basicTypes[9] = item;
				num++;
				break;
			case "System.Int64":
				_basicTypes[10] = item;
				num++;
				break;
			case "System.UInt64":
				_basicTypes[11] = item;
				num++;
				break;
			case "System.Single":
				_basicTypes[12] = item;
				num++;
				break;
			case "System.Double":
				_basicTypes[13] = item;
				num++;
				break;
			case "System.IntPtr":
				_basicTypes[24] = item;
				num++;
				break;
			case "System.UIntPtr":
				_basicTypes[25] = item;
				num++;
				break;
			}
		}
	}

	internal BaseDesktopHeapType CreatePointerType(BaseDesktopHeapType innerType, ClrElementType clrElementType, string nameHint)
	{
		return new DesktopPointerType(this, (DesktopBaseModule)DesktopRuntime.Mscorlib, clrElementType, 0u, nameHint)
		{
			ComponentType = innerType
		};
	}

	internal BaseDesktopHeapType GetArrayType(ClrElementType clrElementType, int ranks, string nameHint)
	{
		if (_arrayTypes == null)
		{
			_arrayTypes = new Dictionary<ArrayRankHandle, BaseDesktopHeapType>();
		}
		ArrayRankHandle key = new ArrayRankHandle(clrElementType, ranks);
		if (!_arrayTypes.TryGetValue(key, out var value))
		{
			value = (_arrayTypes[key] = new DesktopArrayType(this, (DesktopBaseModule)DesktopRuntime.Mscorlib, clrElementType, ranks, ArrayType.MetadataToken, nameHint));
		}
		return value;
	}

	public override void ClearHeapCache()
	{
		_objectMap = null;
		_objects = null;
		_gcRefs = null;
	}

	public override void CacheHeap(CancellationToken cancelToken)
	{
		Action<long, long> action = null;
		DictionaryList dictionaryList = new DictionaryList();
		ExtendedArray<ulong> extendedArray = new ExtendedArray<ulong>();
		ExtendedArray<ObjectInfo> extendedArray2 = new ExtendedArray<ObjectInfo>();
		long num = Segments.Sum((ClrSegment s) => (long)s.Length);
		long num2 = 0L;
		uint pointerSize = (uint)PointerSize;
		foreach (ClrSegment segment in Segments)
		{
			action?.Invoke(num2, num);
			ulong num3 = segment.FirstObject;
			while (num3 < segment.End && num3 != 0L)
			{
				cancelToken.ThrowIfCancellationRequested();
				ClrType objectType = GetObjectType(num3);
				if (objectType == null || GCRoot.IsTooLarge(num3, objectType, segment))
				{
					AddObject(dictionaryList, extendedArray, extendedArray2, num3, Free);
					do
					{
						cancelToken.ThrowIfCancellationRequested();
						num3 += pointerSize;
						if (num3 >= segment.End)
						{
							break;
						}
						objectType = GetObjectType(num3);
					}
					while (objectType == null);
					if (num3 >= segment.End)
					{
						break;
					}
				}
				AddObject(dictionaryList, extendedArray, extendedArray2, num3, objectType);
				num3 = segment.NextObject(num3);
			}
			num2 += (long)segment.Length;
		}
		action?.Invoke(num, num);
		_objectMap = dictionaryList;
		_gcRefs = extendedArray;
		_objects = extendedArray2;
	}

	public override IEnumerable<ClrObject> EnumerateObjects()
	{
		RevisionValidator.Validate(base.Revision, GetRuntimeRevision());
		if (IsHeapCached)
		{
			return from item in _objectMap.Enumerate()
				select ClrObject.Create(item.Key, _objects[item.Value].Type);
		}
		return base.EnumerateObjects();
	}

	public override IEnumerable<ulong> EnumerateObjectAddresses()
	{
		RevisionValidator.Validate(base.Revision, GetRuntimeRevision());
		if (IsHeapCached)
		{
			return from item in _objectMap.Enumerate()
				select item.Key;
		}
		return base.EnumerateObjectAddresses();
	}

	public override ClrType GetObjectType(ulong objRef)
	{
		if (!_objectMap.TryGetValue(objRef, out var index))
		{
			return null;
		}
		return _objects[index].Type;
	}

	protected internal override void EnumerateObjectReferences(ulong obj, ClrType type, bool carefully, Action<ulong, int> callback)
	{
		if (IsHeapCached)
		{
			if (type.ContainsPointers && _objectMap.TryGetValue(obj, out var index))
			{
				uint refCount = _objects[index].RefCount;
				uint refOffset = _objects[index].RefOffset;
				for (uint num = refOffset; num < refOffset + refCount; num++)
				{
					callback(_gcRefs[num], 0);
				}
			}
		}
		else
		{
			base.EnumerateObjectReferences(obj, type, carefully, callback);
		}
		if (_dependentHandles == null)
		{
			BuildDependentHandleMap(CancellationToken.None);
		}
		if (!_dependentHandles.TryGetValue(obj, out var value))
		{
			return;
		}
		foreach (ulong item in value)
		{
			callback(item, -1);
		}
	}

	protected internal override IEnumerable<ClrObject> EnumerateObjectReferences(ulong obj, ClrType type, bool carefully)
	{
		IEnumerable<ClrObject> enumerable = null;
		if (IsHeapCached)
		{
			if (type.ContainsPointers && _objectMap.TryGetValue(obj, out var index))
			{
				uint refCount = _objects[index].RefCount;
				uint refOffset = _objects[index].RefOffset;
				enumerable = EnumerateRefs(refOffset, refCount);
			}
			else
			{
				enumerable = HeapBase.s_emptyObjectSet;
			}
		}
		else
		{
			enumerable = base.EnumerateObjectReferences(obj, type, carefully);
		}
		if (_dependentHandles == null)
		{
			BuildDependentHandleMap(CancellationToken.None);
		}
		if (_dependentHandles.TryGetValue(obj, out var value))
		{
			enumerable = enumerable.Union(value.Select((ulong v) => GetObject(v)));
		}
		return enumerable;
	}

	private IEnumerable<ClrObject> EnumerateRefs(uint offset, uint count)
	{
		for (uint i = offset; i < offset + count; i++)
		{
			ulong objRef = _gcRefs[i];
			yield return GetObject(objRef);
		}
	}

	private void AddObject(DictionaryList objmap, ExtendedArray<ulong> gcrefs, ExtendedArray<ObjectInfo> objInfo, ulong obj, ClrType type)
	{
		uint num = (uint)gcrefs.Count;
		if (type.ContainsPointers || type.IsCollectible)
		{
			EnumerateObjectReferences(obj, type, carefully: true, delegate(ulong addr, int offs)
			{
				gcrefs.Add(addr);
			});
		}
		uint num2 = (uint)(int)gcrefs.Count - num;
		objmap.Add(obj, checked((int)objInfo.Count));
		objInfo.Add(new ObjectInfo
		{
			Type = type,
			RefOffset = ((num2 != 0) ? num : uint.MaxValue),
			RefCount = num2
		});
	}

	protected string GetTypeName(ulong mt, DesktopModule module, uint token)
	{
		return GetBetterTypeName(DesktopRuntime.GetMethodTableName(mt), module, token);
	}

	protected string GetTypeName(TypeHandle hnd, DesktopModule module, uint token)
	{
		return GetBetterTypeName(DesktopRuntime.GetTypeName(hnd), module, token);
	}

	private static string GetBetterTypeName(string typeName, DesktopModule module, uint token)
	{
		if (typeName == null || typeName == "<Unloaded Type>")
		{
			string typeNameFromToken = GetTypeNameFromToken(module, token);
			if (typeNameFromToken != null && typeNameFromToken != "<UNKNOWN>")
			{
				typeName = typeNameFromToken;
			}
		}
		else
		{
			typeName = DesktopHeapType.FixGenerics(typeName);
		}
		return typeName;
	}
}
