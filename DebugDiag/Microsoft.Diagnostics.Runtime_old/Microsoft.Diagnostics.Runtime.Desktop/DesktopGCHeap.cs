#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopGCHeap : HeapBase
{
	private class LastObjectData
	{
		public IObjectData Data;

		public ulong Address;

		public LastObjectData(ulong addr, IObjectData data)
		{
			Address = addr;
			Data = data;
		}
	}

	internal struct LastObjectType
	{
		public ulong Address;

		public ClrType Type;
	}

	private class ModuleEntry
	{
		public ClrModule Module;

		public uint Token;

		public ModuleEntry(ClrModule module, uint token)
		{
			Module = module;
			Token = token;
		}
	}

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

	private BlockingObject[] _managedLocks;

	private TextWriter _log;

	private List<ClrType> _types;

	private Dictionary<TypeHandle, int> _indices = new Dictionary<TypeHandle, int>(TypeHandle.EqualityComparer);

	private Dictionary<ArrayRankHandle, BaseDesktopHeapType> _arrayTypes;

	private ClrModule _mscorlib;

	private Dictionary<ModuleEntry, int> _typeEntry = new Dictionary<ModuleEntry, int>(new ModuleEntryCompare());

	private ClrInstanceField _firstChar;

	private ClrInstanceField _stringLength;

	private LastObjectData _lastObjData;

	private LastObjectType _lastObjType;

	private ClrType[] _basicTypes;

	private bool _loadedTypes;

	internal readonly ClrInterface[] EmptyInterfaceList = new ClrInterface[0];

	internal Dictionary<string, ClrInterface> Interfaces = new Dictionary<string, ClrInterface>();

	internal bool TypesLoaded => _loadedTypes;

	internal DesktopRuntimeBase DesktopRuntime { get; set; }

	internal ClrType ObjectType { get; set; }

	internal ClrType StringType { get; set; }

	internal ClrType ValueType { get; set; }

	internal ClrType FreeType { get; set; }

	internal ClrType ExceptionType { get; set; }

	internal ClrType EnumType { get; set; }

	internal ClrType ArrayType { get; set; }

	public override int TypeIndexLimit => _types.Count;

	internal ClrModule Mscorlib
	{
		get
		{
			if (_mscorlib == null)
			{
				foreach (ClrModule item in DesktopRuntime.EnumerateModules())
				{
					if (item.Name.Contains("mscorlib"))
					{
						_mscorlib = item;
						break;
					}
				}
			}
			return _mscorlib;
		}
	}

	public DesktopGCHeap(DesktopRuntimeBase runtime, TextWriter log)
		: base(runtime)
	{
		DesktopRuntime = runtime;
		_log = log;
		_lastObjType = default(LastObjectType);
		_types = new List<ClrType>(1000);
		base.Revision = runtime.Revision;
		FreeType = GetGCHeapType(DesktopRuntime.FreeMethodTable, 0uL, 0uL);
		ArrayType = GetGCHeapType(DesktopRuntime.ArrayMethodTable, DesktopRuntime.ObjectMethodTable, 0uL);
		ObjectType = GetGCHeapType(DesktopRuntime.ObjectMethodTable, 0uL, 0uL);
		ArrayType.ArrayComponentType = ObjectType;
		((BaseDesktopHeapType)FreeType).DesktopModule = (DesktopModule)ObjectType.Module;
		StringType = GetGCHeapType(DesktopRuntime.StringMethodTable, 0uL, 0uL);
		ExceptionType = GetGCHeapType(DesktopRuntime.ExceptionMethodTable, 0uL, 0uL);
		InitSegments(runtime);
	}

	protected override int GetRuntimeRevision()
	{
		return DesktopRuntime.Revision;
	}

	public override ClrRuntime GetRuntime()
	{
		return DesktopRuntime;
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

	public override ClrType GetObjectType(ulong objRef)
	{
		ulong value = 0uL;
		if (_lastObjType.Address == objRef)
		{
			return _lastObjType.Type;
		}
		MemoryReader memoryReader = base.MemoryReader;
		ulong value2;
		if (memoryReader.Contains(objRef))
		{
			if (!memoryReader.ReadPtr(objRef, out value2))
			{
				return null;
			}
		}
		else if (DesktopRuntime.MemoryReader.Contains(objRef))
		{
			memoryReader = DesktopRuntime.MemoryReader;
			if (!memoryReader.ReadPtr(objRef, out value2))
			{
				return null;
			}
		}
		else
		{
			memoryReader = null;
			value2 = DesktopRuntime.DataReader.ReadPointerUnsafe(objRef);
		}
		if (((int)value2 & 3) != 0)
		{
			value2 &= 0xFFFFFFFFFFFFFFFCuL;
		}
		if (value2 == DesktopRuntime.ArrayMethodTable)
		{
			uint num = (uint)(PointerSize * 2);
			if (memoryReader == null)
			{
				value = DesktopRuntime.DataReader.ReadPointerUnsafe(objRef + num);
			}
			else if (!memoryReader.ReadPtr(objRef + num, out value))
			{
				return null;
			}
		}
		else
		{
			value = 0uL;
		}
		ClrType gCHeapType = GetGCHeapType(value2, value, objRef);
		_lastObjType.Address = objRef;
		_lastObjType.Type = gCHeapType;
		return gCHeapType;
	}

	internal ClrType GetGCHeapType(ulong mt, ulong cmt)
	{
		return GetGCHeapType(mt, cmt, 0uL);
	}

	internal ClrType GetGCHeapType(ulong mt, ulong cmt, ulong obj)
	{
		if (mt == 0L)
		{
			return null;
		}
		TypeHandle typeHandle = new TypeHandle(mt, cmt);
		ClrType clrType = null;
		if (_indices.TryGetValue(typeHandle, out var value))
		{
			clrType = _types[value];
		}
		else if (mt == DesktopRuntime.ArrayMethodTable && cmt == 0L)
		{
			uint metadataToken = DesktopRuntime.GetMetadataToken(mt);
			if (metadataToken == uint.MaxValue)
			{
				return null;
			}
			ModuleEntry key = new ModuleEntry(ArrayType.Module, metadataToken);
			clrType = ArrayType;
			value = _types.Count;
			_indices[typeHandle] = value;
			_typeEntry[key] = value;
			_types.Add(clrType);
		}
		else
		{
			ulong moduleForMT = DesktopRuntime.GetModuleForMT(typeHandle.MethodTable);
			DesktopModule module = DesktopRuntime.GetModule(moduleForMT);
			uint metadataToken2 = DesktopRuntime.GetMetadataToken(mt);
			bool flag = mt == DesktopRuntime.FreeMethodTable;
			if (metadataToken2 == uint.MaxValue && !flag)
			{
				return null;
			}
			uint token = metadataToken2;
			if (!flag && (module == null || module.IsDynamic))
			{
				token = (uint)mt;
			}
			ModuleEntry key2 = new ModuleEntry(module, token);
			string typeName = DesktopRuntime.GetTypeName(typeHandle);
			if (typeName == null || typeName == "<Unloaded Type>")
			{
				StringBuilder typeNameFromToken = GetTypeNameFromToken(module, metadataToken2);
				typeName = ((typeNameFromToken != null) ? typeNameFromToken.ToString() : "<UNKNOWN>");
			}
			else
			{
				typeName = DesktopHeapType.FixGenerics(typeName);
			}
			if (_typeEntry.TryGetValue(key2, out value))
			{
				BaseDesktopHeapType baseDesktopHeapType = (BaseDesktopHeapType)_types[value];
				if (baseDesktopHeapType.Name == typeName)
				{
					_indices[typeHandle] = value;
					clrType = baseDesktopHeapType;
				}
			}
			if (clrType == null)
			{
				IMethodTableData methodTableData = DesktopRuntime.GetMethodTableData(mt);
				if (methodTableData == null)
				{
					return null;
				}
				value = _types.Count;
				clrType = new DesktopHeapType(typeName, module, metadataToken2, mt, methodTableData, this, value);
				_indices[typeHandle] = value;
				_typeEntry[key2] = value;
				_types.Add(clrType);
			}
		}
		if (obj != 0L && clrType.ArrayComponentType == null && clrType.IsArray)
		{
			IObjectData objectData = GetObjectData(obj);
			if (objectData != null)
			{
				if (objectData.ElementTypeHandle != 0L)
				{
					clrType.ArrayComponentType = GetGCHeapType(objectData.ElementTypeHandle, 0uL, 0uL);
				}
				if (clrType.ArrayComponentType == null && objectData.ElementType != 0)
				{
					clrType.ArrayComponentType = GetBasicType(objectData.ElementType);
				}
			}
			else if (cmt != 0L)
			{
				clrType.ArrayComponentType = GetGCHeapType(cmt, 0uL);
			}
		}
		return clrType;
	}

	private static StringBuilder GetTypeNameFromToken(DesktopModule module, uint token)
	{
		if (module == null)
		{
			return null;
		}
		IMetadata metadataImport = module.GetMetadataImport();
		if (metadataImport == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(256);
		if (metadataImport.GetTypeDefProps((int)token, stringBuilder, stringBuilder.Capacity, out var _, out var _, out var _) < 0)
		{
			return null;
		}
		int tdEnclosingClass = 0;
		if (metadataImport.GetNestedClassProps((int)token, out tdEnclosingClass) == 0 && token != tdEnclosingClass)
		{
			StringBuilder stringBuilder2 = GetTypeNameFromToken(module, (uint)tdEnclosingClass);
			if (stringBuilder2 == null)
			{
				stringBuilder2 = new StringBuilder(stringBuilder.Capacity + 16);
				stringBuilder2.Append("<UNKNOWN>");
			}
			stringBuilder2.Append('+');
			stringBuilder2.Append(stringBuilder);
			return stringBuilder2;
		}
		return stringBuilder;
	}

	public override IEnumerable<ulong> EnumerateFinalizableObjects()
	{
		if (!DesktopRuntime.GetHeaps(out var heaps))
		{
			yield break;
		}
		SubHeap[] array = heaps;
		foreach (SubHeap subHeap in array)
		{
			foreach (ulong item in DesktopRuntime.GetPointersInRange(subHeap.FQLiveStart, subHeap.FQLiveStop))
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

	public override IEnumerable<BlockingObject> EnumerateBlockingObjects()
	{
		InitLockInspection();
		return _managedLocks;
	}

	internal void InitLockInspection()
	{
		if (_managedLocks == null)
		{
			LockInspection lockInspection = new LockInspection(this, DesktopRuntime);
			_managedLocks = lockInspection.InitLockInspection();
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
					if (ClrRuntime.IsPrimitive(staticField.ElementType))
					{
						continue;
					}
					{
						foreach (ClrAppDomain appDomain in DesktopRuntime.AppDomains)
						{
							ulong value = 0uL;
							ulong address;
							try
							{
								address = staticField.GetAddress(appDomain);
							}
							catch (Exception ex)
							{
								Trace.WriteLine($"Error getting stack field {type.Name}.{staticField.Name}: {ex.Message}");
								goto end_IL_00e5;
							}
							if (DesktopRuntime.ReadPointer(address, out value) && value != 0L)
							{
								ClrType objectType = GetObjectType(value);
								if (objectType != null)
								{
									yield return new StaticVarRoot(address, value, objectType, type.Name, staticField.Name, appDomain);
								}
							}
						}
						end_IL_00e5:;
					}
				}
				foreach (ClrThreadStaticField tsf in type.ThreadStaticFields)
				{
					if (!ClrRuntime.IsObjectReference(tsf.ElementType))
					{
						continue;
					}
					foreach (ClrAppDomain ad in DesktopRuntime.AppDomains)
					{
						foreach (ClrThread thread in DesktopRuntime.Threads)
						{
							ulong address2 = tsf.GetAddress(ad, thread);
							ulong value2 = 0uL;
							if (DesktopRuntime.ReadPointer(address2, out value2) && value2 != 0L)
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
		IEnumerable<ClrHandle> enumerable = DesktopRuntime.EnumerateHandles();
		if (enumerable != null)
		{
			foreach (ClrHandle handle in enumerable)
			{
				ulong objAddr = handle.Object;
				GCRootKind kind = GCRootKind.Strong;
				if (objAddr == 0L)
				{
					continue;
				}
				ClrType type2 = GetObjectType(objAddr);
				if (type2 == null)
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
				case HandleType.Dependent:
					if (objAddr == 0L)
					{
						continue;
					}
					objAddr = handle.DependentTarget;
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
				yield return new HandleRoot(handle.Address, objAddr, type2, handle.HandleType, kind, handle.AppDomain);
				if (handle.HandleType != HandleType.AsyncPinned)
				{
					continue;
				}
				ClrInstanceField fieldByName = type2.GetFieldByName("m_userObject");
				if (fieldByName == null)
				{
					continue;
				}
				ulong _userObjAddr = fieldByName.GetAddress(objAddr);
				ulong _userObj = (ulong)fieldByName.GetValue(objAddr);
				ClrType _userObjType = GetObjectType(_userObj);
				if (_userObjType == null)
				{
					continue;
				}
				if (_userObjType.IsArray)
				{
					if (_userObjType.ArrayComponentType == null)
					{
						continue;
					}
					if (_userObjType.ArrayComponentType.ElementType == ClrElementType.Object)
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
						yield return new HandleRoot(_userObjAddr, _userObj, _userObjType, HandleType.AsyncPinned, GCRootKind.AsyncPinning, handle.AppDomain);
					}
				}
				else
				{
					yield return new HandleRoot(_userObjAddr, _userObj, _userObjType, HandleType.AsyncPinned, GCRootKind.AsyncPinning, handle.AppDomain);
				}
			}
		}
		else
		{
			Trace.WriteLine("Warning, GetHandles() return null!");
		}
		foreach (ulong item in DesktopRuntime.EnumerateFinalizerQueue())
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
		foreach (ClrThread thread2 in DesktopRuntime.Threads)
		{
			if (!thread2.IsAlive)
			{
				continue;
			}
			foreach (ClrRoot item2 in thread2.EnumerateStackObjects(includePossiblyDead: false))
			{
				yield return item2;
			}
		}
	}

	internal string GetStringContents(ulong strAddr)
	{
		if (strAddr == 0L)
		{
			return null;
		}
		if (_firstChar == null || _stringLength == null)
		{
			_firstChar = StringType.GetFieldByName("m_firstChar");
			_stringLength = StringType.GetFieldByName("m_stringLength");
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
		int bytesRead = 0;
		if (!DesktopRuntime.ReadMemory(address, buffer, count, out bytesRead))
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
		if (_loadedTypes)
		{
			return;
		}
		_loadedTypes = true;
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (ulong item in DesktopRuntime.EnumerateModules(DesktopRuntime.GetAppDomainData(DesktopRuntime.SystemDomainAddress)))
		{
			hashSet.Add(item);
		}
		foreach (ulong item2 in DesktopRuntime.EnumerateModules(DesktopRuntime.GetAppDomainData(DesktopRuntime.SharedDomainAddress)))
		{
			hashSet.Add(item2);
		}
		IAppDomainStoreData appDomainStoreData = DesktopRuntime.GetAppDomainStoreData();
		if (appDomainStoreData == null)
		{
			return;
		}
		IList<ulong> appDomainList = DesktopRuntime.GetAppDomainList(appDomainStoreData.Count);
		if (appDomainList == null)
		{
			return;
		}
		foreach (ulong item3 in appDomainList)
		{
			IAppDomainData appDomainData = DesktopRuntime.GetAppDomainData(item3);
			if (appDomainData != null)
			{
				foreach (ulong item4 in DesktopRuntime.EnumerateModules(appDomainData))
				{
					hashSet.Add(item4);
				}
			}
			else if (_log != null)
			{
				_log.WriteLine("Error: Could not get appdomain information from Appdomain {0:x}.  Skipping.", item3);
			}
		}
		ulong arrayMethodTable = DesktopRuntime.ArrayMethodTable;
		foreach (ulong item5 in hashSet)
		{
			IList<ulong> methodTableList = DesktopRuntime.GetMethodTableList(item5);
			if (methodTableList != null)
			{
				foreach (ulong item6 in methodTableList)
				{
					if (item6 != arrayMethodTable)
					{
						_ = GetGCHeapType(item6, 0uL, 0uL)?.ElementType;
					}
				}
			}
			else if (_log != null)
			{
				_log.WriteLine("Error: Could not get method table list for module {0:x}.  Skipping.", item5);
			}
		}
	}

	internal bool GetObjectHeader(ulong obj, out uint value)
	{
		return base.MemoryReader.TryReadDword(obj - 4, out value);
	}

	internal IObjectData GetObjectData(ulong address)
	{
		LastObjectData lastObjData = _lastObjData;
		if (_lastObjData != null && _lastObjData.Address == address)
		{
			return _lastObjData.Data;
		}
		return (_lastObjData = new LastObjectData(address, DesktopRuntime.GetObjectData(address))).Data;
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
		string name = type.Name;
		switch (_003CPrivateImplementationDetails_003E._0024_0024method0x6000001_002DComputeStringHash(name))
		{
		case 4180476474u:
			if (!(name == "System.Int32"))
			{
				break;
			}
			return ClrElementType.Int32;
		case 1697786220u:
			if (!(name == "System.Int16"))
			{
				break;
			}
			return ClrElementType.Int16;
		case 1764058053u:
			if (!(name == "System.Int64"))
			{
				break;
			}
			return ClrElementType.Int64;
		case 1599499907u:
			if (!(name == "System.IntPtr"))
			{
				break;
			}
			return ClrElementType.NativeInt;
		case 942540437u:
			if (!(name == "System.UInt16"))
			{
				break;
			}
			return ClrElementType.UInt16;
		case 3291009739u:
			if (!(name == "System.UInt32"))
			{
				break;
			}
			return ClrElementType.UInt32;
		case 875577056u:
			if (!(name == "System.UInt64"))
			{
				break;
			}
			return ClrElementType.UInt64;
		case 3482805428u:
			if (!(name == "System.UIntPtr"))
			{
				break;
			}
			return ClrElementType.NativeUInt;
		case 347085918u:
			if (!(name == "System.Boolean"))
			{
				break;
			}
			return ClrElementType.Boolean;
		case 2185383742u:
			if (!(name == "System.Single"))
			{
				break;
			}
			return ClrElementType.Float;
		case 848225627u:
			if (!(name == "System.Double"))
			{
				break;
			}
			return ClrElementType.Double;
		case 3079944380u:
			if (!(name == "System.Byte"))
			{
				break;
			}
			return ClrElementType.UInt8;
		case 2249825754u:
			if (!(name == "System.Char"))
			{
				break;
			}
			return ClrElementType.Char;
		case 2747029693u:
			if (!(name == "System.SByte"))
			{
				break;
			}
			return ClrElementType.Int8;
		case 4124991751u:
			if (!(name == "System.Enum"))
			{
				break;
			}
			return ClrElementType.Int32;
		}
		return ClrElementType.Struct;
	}

	public override ClrType GetTypeByIndex(int index)
	{
		return _types[index];
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
		ClrModule mscorlib = Mscorlib;
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
			string name = item.Name;
			switch (_003CPrivateImplementationDetails_003E._0024_0024method0x6000001_002DComputeStringHash(name))
			{
			case 347085918u:
				if (name == "System.Boolean")
				{
					_basicTypes[2] = item;
					num++;
				}
				break;
			case 2249825754u:
				if (name == "System.Char")
				{
					_basicTypes[3] = item;
					num++;
				}
				break;
			case 2747029693u:
				if (name == "System.SByte")
				{
					_basicTypes[4] = item;
					num++;
				}
				break;
			case 3079944380u:
				if (name == "System.Byte")
				{
					_basicTypes[5] = item;
					num++;
				}
				break;
			case 1697786220u:
				if (name == "System.Int16")
				{
					_basicTypes[6] = item;
					num++;
				}
				break;
			case 942540437u:
				if (name == "System.UInt16")
				{
					_basicTypes[7] = item;
					num++;
				}
				break;
			case 4180476474u:
				if (name == "System.Int32")
				{
					_basicTypes[8] = item;
					num++;
				}
				break;
			case 3291009739u:
				if (name == "System.UInt32")
				{
					_basicTypes[9] = item;
					num++;
				}
				break;
			case 1764058053u:
				if (name == "System.Int64")
				{
					_basicTypes[10] = item;
					num++;
				}
				break;
			case 875577056u:
				if (name == "System.UInt64")
				{
					_basicTypes[11] = item;
					num++;
				}
				break;
			case 2185383742u:
				if (name == "System.Single")
				{
					_basicTypes[12] = item;
					num++;
				}
				break;
			case 848225627u:
				if (name == "System.Double")
				{
					_basicTypes[13] = item;
					num++;
				}
				break;
			case 1599499907u:
				if (name == "System.IntPtr")
				{
					_basicTypes[24] = item;
					num++;
				}
				break;
			case 3482805428u:
				if (name == "System.UIntPtr")
				{
					_basicTypes[25] = item;
					num++;
				}
				break;
			}
		}
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
			value = (_arrayTypes[key] = new DesktopArrayType(this, (DesktopBaseModule)Mscorlib, clrElementType, ranks, ArrayType.MetadataToken, nameHint));
		}
		return value;
	}
}
