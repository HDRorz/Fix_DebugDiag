using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopHeapType : BaseDesktopHeapType
{
	private class EnumData
	{
		internal ClrElementType ElementType;

		internal readonly Dictionary<string, object> NameToValue = new Dictionary<string, object>();

		internal readonly Dictionary<object, string> ValueToName = new Dictionary<object, string>();
	}

	private ulong _cachedMethodTable;

	private ulong[] _methodTables;

	private readonly Lazy<string> _name;

	private int _index;

	private TypeAttributes _attributes;

	private readonly ulong _parent;

	private readonly uint _baseSize;

	private readonly uint _componentSize;

	private readonly bool _containsPointers;

	private readonly bool _isCollectible;

	private readonly ulong _loaderAllocatorObjectHandle;

	private byte _finalizable;

	private List<ClrInstanceField> _fields;

	private List<ClrStaticField> _statics;

	private List<ClrThreadStaticField> _threadStatics;

	private int[] _fieldNameMap;

	private int _baseArrayOffset;

	private bool _hasMethods;

	private bool? _runtimeType;

	private EnumData _enumData;

	private const uint FinalizationSuppressedFlag = 1073741824u;

	private IList<ClrMethod> _methods;

	private static readonly ClrStaticField[] s_emptyStatics = new ClrStaticField[0];

	private static readonly ClrThreadStaticField[] s_emptyThreadStatics = new ClrThreadStaticField[0];

	public override ulong MethodTable
	{
		get
		{
			if (_cachedMethodTable != 0L)
			{
				return _cachedMethodTable;
			}
			if (base.Shared || ((DesktopRuntimeBase)Heap.Runtime).IsSingleDomain)
			{
				_cachedMethodTable = _constructedMT;
			}
			else
			{
				_cachedMethodTable = EnumerateMethodTables().FirstOrDefault();
				if (_cachedMethodTable == 0L)
				{
					_cachedMethodTable = _constructedMT;
				}
			}
			return _cachedMethodTable;
		}
	}

	public override ClrElementType ElementType
	{
		get
		{
			if (_elementType == ClrElementType.Unknown)
			{
				_elementType = base.DesktopHeap.GetElementType(this, 0);
			}
			return _elementType;
		}
		internal set
		{
			_elementType = value;
		}
	}

	public override string Name => _name.Value;

	public override ClrModule Module
	{
		get
		{
			if (base.DesktopModule == null)
			{
				return base.DesktopHeap.DesktopRuntime.ErrorModule;
			}
			return base.DesktopModule;
		}
	}

	public override ClrHeap Heap => base.DesktopHeap;

	public override bool HasSimpleValue => ElementType != ClrElementType.Struct;

	public override bool IsException
	{
		get
		{
			for (ClrType clrType = this; clrType != null; clrType = clrType.BaseType)
			{
				if (clrType == base.DesktopHeap.ExceptionType)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override bool IsEnum
	{
		get
		{
			ClrType clrType = this;
			ClrType enumType = base.DesktopHeap.EnumType;
			while (clrType != null)
			{
				if (enumType == null && clrType.Name == "System.Enum")
				{
					base.DesktopHeap.EnumType = clrType;
					return true;
				}
				if (clrType == enumType)
				{
					return true;
				}
				clrType = clrType.BaseType;
			}
			return false;
		}
	}

	public override bool IsFree => this == base.DesktopHeap.Free;

	public override bool IsFinalizable
	{
		get
		{
			if (_finalizable == 0)
			{
				foreach (ClrMethod method in Methods)
				{
					if (method.IsVirtual && method.Name == "Finalize")
					{
						_finalizable = 1;
						break;
					}
				}
				if (_finalizable == 0)
				{
					_finalizable = 2;
				}
			}
			return _finalizable == 1;
		}
	}

	public override bool IsArray
	{
		get
		{
			if (_componentSize != 0 && this != base.DesktopHeap.StringType)
			{
				return this != base.DesktopHeap.Free;
			}
			return false;
		}
	}

	public override bool ContainsPointers => _containsPointers;

	public override bool IsCollectible => _isCollectible;

	public override ulong LoaderAllocatorObject
	{
		get
		{
			if (_isCollectible)
			{
				base.DesktopHeap.ReadPointer(_loaderAllocatorObjectHandle, out var value);
				return value;
			}
			return 0uL;
		}
	}

	public override bool IsString => this == base.DesktopHeap.StringType;

	public override int ElementSize => (int)_componentSize;

	public override IList<ClrInstanceField> Fields
	{
		get
		{
			if (_fields == null)
			{
				InitFields();
			}
			return _fields;
		}
	}

	public override IList<ClrStaticField> StaticFields
	{
		get
		{
			if (_fields == null)
			{
				InitFields();
			}
			if (_statics == null)
			{
				return s_emptyStatics;
			}
			return _statics;
		}
	}

	public override IList<ClrThreadStaticField> ThreadStaticFields
	{
		get
		{
			if (_fields == null)
			{
				InitFields();
			}
			if (_threadStatics == null)
			{
				return s_emptyThreadStatics;
			}
			return _threadStatics;
		}
	}

	public override IList<ClrMethod> Methods
	{
		get
		{
			if (_methods != null)
			{
				return _methods;
			}
			MetaDataImport metadata = null;
			if (base.DesktopModule != null)
			{
				metadata = base.DesktopModule.GetMetadataImport();
			}
			DesktopRuntimeBase desktopRuntime = base.DesktopHeap.DesktopRuntime;
			IList<ulong> methodDescList = desktopRuntime.GetMethodDescList(_constructedMT);
			if (methodDescList != null)
			{
				_methods = new List<ClrMethod>(methodDescList.Count);
				foreach (ulong item in methodDescList)
				{
					if (item != 0L)
					{
						IMethodDescData methodDescData = desktopRuntime.GetMethodDescData(item);
						DesktopMethod desktopMethod = DesktopMethod.Create(desktopRuntime, metadata, methodDescData);
						if (desktopMethod != null)
						{
							_methods.Add(desktopMethod);
						}
					}
				}
			}
			else
			{
				_methods = new ClrMethod[0];
			}
			return _methods;
		}
	}

	public override ClrType BaseType
	{
		get
		{
			if (_parent == 0L)
			{
				return null;
			}
			return base.DesktopHeap.GetTypeByMethodTable(_parent, 0uL, 0uL);
		}
	}

	public override int BaseSize => (int)_baseSize;

	public override bool IsInternal
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			TypeAttributes typeAttributes = _attributes & TypeAttributes.VisibilityMask;
			if (typeAttributes != TypeAttributes.NestedAssembly)
			{
				return typeAttributes == TypeAttributes.NotPublic;
			}
			return true;
		}
	}

	public override bool IsPublic
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			TypeAttributes typeAttributes = _attributes & TypeAttributes.VisibilityMask;
			if (typeAttributes != TypeAttributes.Public)
			{
				return typeAttributes == TypeAttributes.NestedPublic;
			}
			return true;
		}
	}

	public override bool IsPrivate
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			return (_attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
		}
	}

	public override bool IsProtected
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			return (_attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
		}
	}

	public override bool IsAbstract
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			return (_attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
		}
	}

	public override bool IsSealed
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			return (_attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
		}
	}

	public override bool IsInterface
	{
		get
		{
			if (_attributes == TypeAttributes.NotPublic)
			{
				InitFlags();
			}
			return (_attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask;
		}
	}

	public override bool IsRuntimeType
	{
		get
		{
			if (!_runtimeType.HasValue)
			{
				_runtimeType = Name == "System.RuntimeType";
			}
			return _runtimeType.Value;
		}
	}

	public override IEnumerable<ulong> EnumerateMethodTables()
	{
		if (_methodTables == null && (base.Shared || ((DesktopRuntimeBase)Heap.Runtime).IsSingleDomain) && _cachedMethodTable == 0L && _constructedMT != 0L)
		{
			_cachedMethodTable = _constructedMT;
			_methodTables = new ulong[1] { _cachedMethodTable };
			return _methodTables;
		}
		return FillAndEnumerateTypeHandles();
	}

	private IEnumerable<ulong> FillAndEnumerateTypeHandles()
	{
		IList<ClrAppDomain> domains = null;
		if (_methodTables == null)
		{
			domains = Module.AppDomains;
			_methodTables = new ulong[domains.Count];
		}
		for (int i = 0; i < _methodTables.Length; i++)
		{
			if (_methodTables[i] == 0L)
			{
				if (domains == null)
				{
					domains = Module.AppDomains;
				}
				ulong mTForDomain = ((DesktopModule)base.DesktopModule).GetMTForDomain(domains[i], this);
				_methodTables[i] = ((mTForDomain != 0L) ? mTForDomain : ulong.MaxValue);
			}
			if (_methodTables[i] != ulong.MaxValue)
			{
				yield return _methodTables[i];
			}
		}
	}

	public override ulong GetSize(ulong objRef)
	{
		uint pointerSize = (uint)base.DesktopHeap.PointerSize;
		ulong num;
		if (_componentSize == 0)
		{
			num = _baseSize;
		}
		else
		{
			uint num2 = pointerSize;
			ulong addr = objRef + num2;
			MemoryReader memoryReader = base.DesktopHeap.MemoryReader;
			if (!memoryReader.Contains(addr) && base.DesktopHeap.DesktopRuntime.MemoryReader.Contains(addr))
			{
				memoryReader = base.DesktopHeap.DesktopRuntime.MemoryReader;
			}
			if (!memoryReader.ReadDword(addr, out uint value))
			{
				throw new Exception("Could not read from heap at " + objRef.ToString("x"));
			}
			if (base.DesktopHeap.StringType == this && base.DesktopHeap.DesktopRuntime.CLRVersion != 0)
			{
				value++;
			}
			num = (ulong)((long)value * (long)_componentSize + _baseSize);
		}
		uint num3 = pointerSize * 3;
		if (num < num3)
		{
			num = num3;
		}
		return num;
	}

	public override void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action)
	{
		if (_containsPointers)
		{
			Heap.EnumerateObjectReferences(objRef, this, carefully: true, action);
		}
	}

	public override void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> action)
	{
		if (_containsPointers)
		{
			Heap.EnumerateObjectReferences(objRef, this, carefully: false, action);
		}
	}

	public override string ToString()
	{
		return Name;
	}

	public override object GetValue(ulong address)
	{
		if (IsPrimitive)
		{
			address += (ulong)base.DesktopHeap.PointerSize;
		}
		return base.DesktopHeap.GetValueAtAddress(ElementType, address);
	}

	public override bool IsCCW(ulong obj)
	{
		if (IsFree)
		{
			return false;
		}
		if (base.DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
		{
			return false;
		}
		IObjectData objectData = base.DesktopHeap.GetObjectData(obj);
		if (objectData != null)
		{
			return objectData.CCW != 0;
		}
		return false;
	}

	public override CcwData GetCCWData(ulong obj)
	{
		if (base.DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
		{
			return null;
		}
		DesktopCCWData result = null;
		IObjectData objectData = base.DesktopHeap.GetObjectData(obj);
		if (objectData != null && objectData.CCW != 0L)
		{
			ICCWData cCWData = base.DesktopHeap.DesktopRuntime.GetCCWData(objectData.CCW);
			if (cCWData != null)
			{
				result = new DesktopCCWData(base.DesktopHeap, objectData.CCW, cCWData);
			}
		}
		return result;
	}

	public override bool IsRCW(ulong obj)
	{
		if (IsFree)
		{
			return false;
		}
		if (base.DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
		{
			return false;
		}
		IObjectData objectData = base.DesktopHeap.GetObjectData(obj);
		if (objectData != null)
		{
			return objectData.RCW != 0;
		}
		return false;
	}

	public override RcwData GetRCWData(ulong obj)
	{
		if (base.DesktopHeap.DesktopRuntime.CLRVersion != DesktopVersion.v45)
		{
			return null;
		}
		DesktopRCWData result = null;
		IObjectData objectData = base.DesktopHeap.GetObjectData(obj);
		if (objectData != null && objectData.RCW != 0L)
		{
			IRCWData rCWData = base.DesktopHeap.DesktopRuntime.GetRCWData(objectData.RCW);
			if (rCWData != null)
			{
				result = new DesktopRCWData(base.DesktopHeap, objectData.RCW, rCWData);
			}
		}
		return result;
	}

	public override ClrElementType GetEnumElementType()
	{
		if (_enumData == null)
		{
			InitEnumData();
		}
		return _enumData.ElementType;
	}

	public override bool TryGetEnumValue(string name, out int value)
	{
		if (TryGetEnumValue(name, out object value2))
		{
			value = (int)value2;
			return true;
		}
		value = int.MinValue;
		return false;
	}

	public override bool TryGetEnumValue(string name, out object value)
	{
		if (_enumData == null)
		{
			InitEnumData();
		}
		return _enumData.NameToValue.TryGetValue(name, out value);
	}

	public override string GetEnumName(object value)
	{
		if (_enumData == null)
		{
			InitEnumData();
		}
		_enumData.ValueToName.TryGetValue(value, out var value2);
		return value2;
	}

	public override string GetEnumName(int value)
	{
		return GetEnumName((object)value);
	}

	public override IEnumerable<string> GetEnumNames()
	{
		if (_enumData == null)
		{
			InitEnumData();
		}
		return _enumData.NameToValue.Keys;
	}

	private void InitEnumData()
	{
		if (!IsEnum)
		{
			throw new InvalidOperationException("Type is not an Enum.");
		}
		_enumData = new EnumData();
		MetaDataImport metaDataImport = base.DesktopModule?.GetMetadataImport();
		if (metaDataImport == null)
		{
			return;
		}
		_ = IntPtr.Zero;
		List<string> list = new List<string>();
		foreach (int item in metaDataImport.EnumerateFields((int)_token))
		{
			if (!metaDataImport.GetFieldProps(item, out var name, out var attrs, out var ppvSigBlob, out var pcbSigBlob, out var pdwCPlusTypeFlag, out var ppValue))
			{
				continue;
			}
			if (attrs == (FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName) && name == "value__")
			{
				SigParser sigParser = new SigParser(ppvSigBlob, pcbSigBlob);
				if (sigParser.GetCallingConvInfo(out var _) && sigParser.GetElemType(out var etype))
				{
					_enumData.ElementType = (ClrElementType)etype;
				}
			}
			if (attrs == (FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault))
			{
				list.Add(name);
				SigParser sigParser2 = new SigParser(ppvSigBlob, pcbSigBlob);
				sigParser2.GetCallingConvInfo(out var _);
				sigParser2.GetElemType(out var _);
				Type typeForElementType = ((ClrElementType)pdwCPlusTypeFlag).GetTypeForElementType();
				if (typeForElementType != null)
				{
					object obj = Marshal.PtrToStructure(ppValue, typeForElementType);
					_enumData.NameToValue[name] = obj;
					_enumData.ValueToName[obj] = name;
				}
			}
		}
	}

	public override bool IsFinalizeSuppressed(ulong obj)
	{
		if (base.DesktopHeap.GetObjectHeader(obj, out var value))
		{
			return (value & 0x40000000) == 1073741824;
		}
		return false;
	}

	public override bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset)
	{
		if (!IsArray)
		{
			int num = fieldOffset;
			if (!inner)
			{
				num -= base.DesktopHeap.PointerSize;
			}
			foreach (ClrInstanceField field in Fields)
			{
				if (field.ElementType == ClrElementType.Unknown)
				{
					break;
				}
				if (field.Offset <= num && num < field.Offset + field.Size)
				{
					childField = field;
					childFieldOffset = num - field.Offset;
					return true;
				}
			}
		}
		if (BaseType != null)
		{
			return BaseType.GetFieldForOffset(fieldOffset, inner, out childField, out childFieldOffset);
		}
		childField = null;
		childFieldOffset = 0;
		return false;
	}

	private void InitFields()
	{
		if (_fields != null)
		{
			return;
		}
		if (IsFree)
		{
			_fields = new List<ClrInstanceField>();
			return;
		}
		DesktopRuntimeBase desktopRuntime = base.DesktopHeap.DesktopRuntime;
		IFieldInfo fieldInfo = desktopRuntime.GetFieldInfo(_constructedMT);
		if (fieldInfo == null)
		{
			_fields = new List<ClrInstanceField>();
			return;
		}
		_fields = new List<ClrInstanceField>((int)fieldInfo.InstanceFields);
		if (BaseType != null)
		{
			foreach (ClrInstanceField field in BaseType.Fields)
			{
				_fields.Add(field);
			}
		}
		int num = (int)(fieldInfo.InstanceFields + fieldInfo.StaticFields) - _fields.Count;
		ulong num2 = fieldInfo.FirstField;
		int num3 = 0;
		MetaDataImport metaDataImport = null;
		if (num2 != 0L && base.DesktopModule != null)
		{
			metaDataImport = base.DesktopModule.GetMetadataImport();
		}
		while (num3 < num && num2 != 0L)
		{
			IFieldData fieldData = desktopRuntime.GetFieldData(num2);
			if (fieldData == null)
			{
				break;
			}
			if (fieldData.IsContextLocal)
			{
				num2 = fieldData.NextField;
				continue;
			}
			string name = null;
			FieldAttributes attrs = FieldAttributes.PrivateScope;
			int pcbSigBlob = 0;
			IntPtr ppValue = IntPtr.Zero;
			IntPtr ppvSigBlob = IntPtr.Zero;
			metaDataImport?.GetFieldProps((int)fieldData.FieldToken, out name, out attrs, out ppvSigBlob, out pcbSigBlob, out var _, out ppValue);
			if (metaDataImport == null || name == null)
			{
				name = $"<ERROR:{fieldData.FieldToken:X}>";
			}
			if (fieldData.IsThreadLocal)
			{
				if (_threadStatics == null)
				{
					_threadStatics = new List<ClrThreadStaticField>((int)fieldInfo.ThreadStaticFields);
				}
			}
			else if (fieldData.IsStatic)
			{
				if (_statics == null)
				{
					_statics = new List<ClrStaticField>();
				}
				_statics.Add(new DesktopStaticField(base.DesktopHeap, fieldData, this, name, attrs, null, ppvSigBlob, pcbSigBlob));
			}
			else
			{
				_fields.Add(new DesktopInstanceField(base.DesktopHeap, fieldData, name, attrs, ppvSigBlob, pcbSigBlob));
			}
			num3++;
			num2 = fieldData.NextField;
		}
		_fields.Sort((ClrInstanceField a, ClrInstanceField b) => a.Offset.CompareTo(b.Offset));
	}

	internal override ClrMethod GetMethod(uint token)
	{
		return Methods.Where((ClrMethod m) => m.MetadataToken == token).FirstOrDefault();
	}

	public override ClrStaticField GetStaticFieldByName(string name)
	{
		foreach (ClrStaticField staticField in StaticFields)
		{
			if (staticField.Name == name)
			{
				return staticField;
			}
		}
		return null;
	}

	public override ClrInstanceField GetFieldByName(string name)
	{
		if (_fields == null)
		{
			InitFields();
		}
		if (_fields.Count == 0)
		{
			return null;
		}
		if (_fieldNameMap == null)
		{
			_fieldNameMap = new int[_fields.Count];
			for (int i = 0; i < _fieldNameMap.Length; i++)
			{
				_fieldNameMap[i] = i;
			}
			Array.Sort(_fieldNameMap, (int x, int y) => _fields[x].Name.CompareTo(_fields[y].Name));
		}
		int num = 0;
		int num2 = _fieldNameMap.Length - 1;
		while (num2 >= num)
		{
			int num3 = (num2 + num) / 2;
			int num4 = _fields[_fieldNameMap[num3]].Name.CompareTo(name);
			if (num4 < 0)
			{
				num = num3 + 1;
				continue;
			}
			if (num4 > 0)
			{
				num2 = num3 - 1;
				continue;
			}
			return _fields[_fieldNameMap[num3]];
		}
		return null;
	}

	public override int GetArrayLength(ulong objRef)
	{
		if (!base.DesktopHeap.DesktopRuntime.ReadDword(objRef + (uint)base.DesktopHeap.DesktopRuntime.PointerSize, out uint value))
		{
			return 0;
		}
		return (int)value;
	}

	public override ulong GetArrayElementAddress(ulong objRef, int index)
	{
		if (_baseArrayOffset == 0)
		{
			ClrType componentType = ComponentType;
			IObjectData objectData = base.DesktopHeap.DesktopRuntime.GetObjectData(objRef);
			if (objectData != null)
			{
				_baseArrayOffset = (int)(objectData.DataPointer - objRef);
			}
			else
			{
				if (componentType == null)
				{
					return 0uL;
				}
				if (!componentType.IsObjectReference || !Heap.Runtime.HasArrayComponentMethodTables)
				{
					_baseArrayOffset = IntPtr.Size * 2;
				}
				else
				{
					_baseArrayOffset = IntPtr.Size * 3;
				}
			}
		}
		return objRef + (ulong)(_baseArrayOffset + index * _componentSize);
	}

	public override object GetArrayElementValue(ulong objRef, int index)
	{
		ulong value = GetArrayElementAddress(objRef, index);
		if (value == 0L)
		{
			return null;
		}
		ClrElementType clrElementType = ClrElementType.Unknown;
		ClrType componentType = ComponentType;
		if (componentType != null)
		{
			clrElementType = componentType.ElementType;
		}
		else
		{
			IObjectData objectData = base.DesktopHeap.DesktopRuntime.GetObjectData(objRef);
			if (objectData == null)
			{
				return null;
			}
			clrElementType = objectData.ElementType;
		}
		switch (clrElementType)
		{
		case ClrElementType.Unknown:
			return null;
		case ClrElementType.String:
			if (!base.DesktopHeap.MemoryReader.ReadPtr(value, out value))
			{
				return null;
			}
			break;
		}
		return base.DesktopHeap.GetValueAtAddress(clrElementType, value);
	}

	internal static int FixGenericsWorker(string name, int start, int end, StringBuilder sb)
	{
		int num = 0;
		while (start < end)
		{
			char c = name[start];
			if (c == '`')
			{
				break;
			}
			if (c == '[')
			{
				num++;
			}
			if (c == ']')
			{
				num--;
			}
			if (num < 0)
			{
				return start + 1;
			}
			if (c == ',' && num == 0)
			{
				return start;
			}
			sb.Append(c);
			start++;
		}
		if (start >= end)
		{
			return start;
		}
		start++;
		bool flag = false;
		int num2 = 0;
		do
		{
			int num3 = 0;
			flag = false;
			while (start < end)
			{
				char c2 = name[start];
				if (c2 < '0' || c2 > '9')
				{
					break;
				}
				num3 = num3 * 10 + c2 - 48;
				start++;
			}
			num2 += num3;
			if (start >= end)
			{
				return start;
			}
			if (name[start] != '+')
			{
				continue;
			}
			while (start < end && name[start] != '[')
			{
				if (name[start] == '`')
				{
					start++;
					flag = true;
					break;
				}
				sb.Append(name[start]);
				start++;
			}
			if (start >= end)
			{
				return start;
			}
		}
		while (flag);
		if (name[start] == '[')
		{
			sb.Append('<');
			start++;
			while (num2-- > 0)
			{
				if (start >= end)
				{
					return start;
				}
				bool flag2 = false;
				if (name[start] == '[')
				{
					flag2 = true;
					start++;
				}
				start = FixGenericsWorker(name, start, end, sb);
				if (start < end && name[start] == '[')
				{
					start++;
					if (start >= end)
					{
						return start;
					}
					sb.Append('[');
					while (start < end && name[start] == ',')
					{
						sb.Append(',');
						start++;
					}
					if (start >= end)
					{
						return start;
					}
					if (name[start] == ']')
					{
						sb.Append(']');
						start++;
					}
				}
				if (flag2)
				{
					while (start < end && name[start] != ']')
					{
						start++;
					}
					start++;
				}
				if (num2 > 0)
				{
					if (start >= end)
					{
						return start;
					}
					sb.Append(',');
					start++;
					if (start >= end)
					{
						return start;
					}
					if (name[start] == ' ')
					{
						start++;
					}
				}
			}
			sb.Append('>');
			start++;
		}
		if (start + 1 >= end)
		{
			return start;
		}
		if (name[start] == '[' && name[start + 1] == ']')
		{
			sb.Append("[]");
		}
		return start;
	}

	internal static string FixGenerics(string name)
	{
		StringBuilder stringBuilder = new StringBuilder();
		FixGenericsWorker(name, 0, name.Length, stringBuilder);
		return stringBuilder.ToString();
	}

	internal DesktopHeapType(Func<string> typeNameFactory, DesktopModule module, uint token, ulong mt, IMethodTableData mtData, DesktopGCHeap heap, IMethodTableCollectibleData mtCollectibleData = null)
		: base(mt, heap, module, token)
	{
		_name = new Lazy<string>(typeNameFactory);
		base.Shared = mtData.Shared;
		_parent = mtData.Parent;
		_baseSize = mtData.BaseSize;
		_componentSize = mtData.ComponentSize;
		_containsPointers = mtData.ContainsPointers;
		_hasMethods = mtData.NumMethods != 0;
		if (mtCollectibleData != null)
		{
			_isCollectible = mtCollectibleData.Collectible;
			_loaderAllocatorObjectHandle = mtCollectibleData.LoaderAllocatorObjectHandle;
		}
	}

	public DesktopHeapType(ulong mt, DesktopGCHeap heap, DesktopBaseModule module, uint token)
		: base(mt, heap, module, token)
	{
	}

	internal void SetIndex(int index)
	{
		_index = index;
	}

	private void InitFlags()
	{
		if (_attributes == TypeAttributes.NotPublic && base.DesktopModule != null)
		{
			MetaDataImport metadataImport = base.DesktopModule.GetMetadataImport();
			if (metadataImport == null)
			{
				_attributes = (TypeAttributes)1879048192;
			}
			else if (!metadataImport.GetTypeDefAttributes((int)_token, out _attributes) || _attributes == TypeAttributes.NotPublic)
			{
				_attributes = (TypeAttributes)1879048192;
			}
		}
	}

	internal override ulong GetModuleAddress(ClrAppDomain appDomain)
	{
		if (base.DesktopModule == null)
		{
			return 0uL;
		}
		return base.DesktopModule.GetDomainModule(appDomain);
	}

	public override ClrType GetRuntimeType(ulong obj)
	{
		if (!IsRuntimeType)
		{
			return null;
		}
		ClrInstanceField fieldByName = GetFieldByName("m_handle");
		if (fieldByName == null)
		{
			return null;
		}
		ulong mt = 0uL;
		if (fieldByName.ElementType == ClrElementType.NativeInt)
		{
			mt = (ulong)(long)fieldByName.GetValue(obj);
		}
		else if (fieldByName.ElementType == ClrElementType.Struct)
		{
			mt = (ulong)(long)fieldByName.Type.GetFieldByName("m_ptr").GetValue(fieldByName.GetAddress(obj, interior: false), interior: true);
		}
		return base.DesktopHeap.GetTypeByMethodTable(mt, 0uL, obj);
	}

	internal void InitMethodHandles()
	{
		DesktopRuntimeBase desktopRuntime = base.DesktopHeap.DesktopRuntime;
		foreach (ulong item in EnumerateMethodTables())
		{
			foreach (ulong methodDesc in desktopRuntime.GetMethodDescList(item))
			{
				IMethodDescData methodDescData = desktopRuntime.GetMethodDescData(methodDesc);
				DesktopMethod desktopMethod = (DesktopMethod)GetMethod(methodDescData.MDToken);
				if (desktopMethod.Type == this)
				{
					desktopMethod.AddMethodHandle(methodDesc);
				}
			}
		}
	}
}
