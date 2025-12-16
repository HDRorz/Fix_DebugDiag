using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public class ClrObject : DynamicObject
{
	private ulong m_addr;

	private bool m_inner;

	private ClrType m_type;

	private ClrHeap m_heap;

	private int m_len = -1;

	public ClrObject(ClrHeap heap, ClrType type, ulong addr, bool inner)
	{
		if (heap == null)
		{
			throw new ArgumentNullException("heap");
		}
		m_addr = addr;
		m_inner = inner;
		m_heap = heap;
		m_type = (inner ? type : heap.GetObjectType(addr));
	}

	public ClrObject(ClrHeap heap, ClrType type, ulong addr)
	{
		if (heap == null)
		{
			throw new ArgumentNullException("heap");
		}
		m_addr = addr;
		m_heap = heap;
		if (addr != 0L)
		{
			ClrType objectType = heap.GetObjectType(addr);
			if (objectType != null)
			{
				type = objectType;
			}
		}
		m_type = type;
	}

	public override string ToString()
	{
		if (IsNull())
		{
			return "{null}";
		}
		return $"[{m_addr:X} {m_type.Name}]";
	}

	public bool IsNull()
	{
		if (m_addr != 0L)
		{
			return m_type == null;
		}
		return true;
	}

	public ulong GetValue()
	{
		return m_addr;
	}

	public ClrType GetHeapType()
	{
		return m_type;
	}

	public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
	{
		if (IsNull())
		{
			result = new ClrNullValue(m_heap);
			return true;
		}
		if (m_type.IsArray)
		{
			if (m_len == -1)
			{
				m_len = m_type.GetArrayLength(m_addr);
			}
			int indexFromObjects = GetIndexFromObjects(indexes);
			return GetArrayValue(m_type, m_addr, indexFromObjects, out result);
		}
		if (IsDictionary())
		{
			if (indexes.Length != 1)
			{
				throw new ArgumentException("Only one index is allowed for Dictionary indexing.");
			}
			dynamic val = ((dynamic)this).entries;
			ClrType type = ((ClrType)val).ComponentType.GetFieldByName("key").Type;
			if (type.IsObjectReference)
			{
				if (indexes[0].GetType() == typeof(string))
				{
					string text = (string)indexes[0];
					int num = ((dynamic)this).count;
					for (int i = 0; i < num; i++)
					{
						if ((string)val[i].key == text)
						{
							result = val[i].value;
							return true;
						}
					}
					throw new KeyNotFoundException();
				}
				ulong num2 = ((indexes[0].GetType() == typeof(long)) ? ((ulong)(long)indexes[0]) : ((!(indexes[0].GetType() == typeof(ulong))) ? ((ulong)(dynamic)indexes[0]) : ((ulong)indexes[0])));
				int num3 = ((dynamic)this).count;
				for (int j = 0; j < num3; j++)
				{
					if ((ulong)val[j].key == num2)
					{
						result = val[j].value;
						return true;
					}
				}
			}
			else if (type.IsPrimitive)
			{
				object obj = indexes[0];
				if (obj is ClrPrimitiveValue)
				{
					obj = ((ClrPrimitiveValue)obj).GetValue();
				}
				obj.GetType();
				int num4 = ((dynamic)this).count;
				for (int k = 0; k < num4; k++)
				{
					if (((ClrPrimitiveValue)val[k].key).GetValue().Equals(obj))
					{
						result = val[k].value;
						return true;
					}
				}
				throw new KeyNotFoundException();
			}
		}
		if (IsList())
		{
			int indexFromObjects2 = GetIndexFromObjects(indexes);
			ClrInstanceField fieldByName = m_type.GetFieldByName("_items");
			ulong num5 = (ulong)fieldByName.GetValue(m_addr);
			if (m_len == -1)
			{
				ClrInstanceField fieldByName2 = m_type.GetFieldByName("_size");
				m_len = (int)fieldByName2.GetValue(m_addr);
			}
			ClrType clrType = fieldByName.Type;
			if (clrType == null || clrType.ComponentType == null)
			{
				clrType = m_heap.GetObjectType(num5);
				if (clrType == null || clrType.ComponentType == null)
				{
					result = new ClrNullValue(m_heap);
					return true;
				}
			}
			return GetArrayValue(clrType, num5, indexFromObjects2, out result);
		}
		throw new InvalidOperationException($"Object of type '{m_type.Name}' is not indexable.");
	}

	private static int GetIndexFromObjects(object[] indexes)
	{
		if (indexes.Length != 1)
		{
			throw new ArgumentException("Only one integer index is allowed for array indexing.");
		}
		int num = -1;
		if (indexes[0] is int)
		{
			return (int)indexes[0];
		}
		if (indexes[0] is uint)
		{
			return (int)(uint)indexes[0];
		}
		throw new ArgumentException("Array index must be integer.");
	}

	private bool GetArrayValue(ClrType type, ulong addr, int index, out object result)
	{
		ClrType arrayComponentType = type.ComponentType;
		if (addr == 0L || arrayComponentType == null)
		{
			result = new ClrNullValue(m_heap);
			return true;
		}
		if (index < 0 || index >= m_len)
		{
			throw new IndexOutOfRangeException();
		}
		if (arrayComponentType.ElementType == ClrElementType.Struct)
		{
			addr = type.GetArrayElementAddress(addr, index);
			result = new ClrObject(m_heap, arrayComponentType, addr, inner: true);
			return true;
		}
		if (arrayComponentType.IsObjectReference)
		{
			addr = type.GetArrayElementAddress(addr, index);
			if (!m_heap.Runtime.ReadPointer(addr, out addr) || addr == 0L)
			{
				result = new ClrNullValue(m_heap);
				return true;
			}
			result = new ClrObject(m_heap, arrayComponentType, addr);
			return true;
		}
		if (arrayComponentType.IsPrimitive)
		{
			result = new ClrPrimitiveValue(type.GetArrayElementValue(addr, index), arrayComponentType.ElementType);
			return true;
		}
		result = null;
		return false;
	}

	private bool IsDictionary()
	{
		if (!m_type.IsArray && m_type.Name.StartsWith("System.Collections.Generic.Dictionary<"))
		{
			return m_type.Name.EndsWith(">");
		}
		return false;
	}

	private bool IsList()
	{
		if (!m_type.IsArray && m_type.Name.StartsWith("System.Collections.Generic.List<"))
		{
			return m_type.Name.EndsWith(">");
		}
		return false;
	}

	public override IEnumerable<string> GetDynamicMemberNames()
	{
		if (m_type == null)
		{
			return new string[0];
		}
		return m_type.Fields.Select((ClrInstanceField f) => f.Name);
	}

	public override bool TryConvert(ConvertBinder binder, out object result)
	{
		if (m_type == null)
		{
			return ClrNullValue.GetDefaultNullValue(binder.Type, out result);
		}
		if (binder.Type == typeof(ClrType))
		{
			result = m_type;
			return true;
		}
		if (binder.Type.IsPrimitive && m_type.IsPrimitive && m_type.HasSimpleValue)
		{
			result = Convert.ChangeType(m_type.GetValue(m_addr), binder.Type);
			return true;
		}
		if (binder.Type == typeof(ulong))
		{
			result = m_addr;
			return true;
		}
		if (binder.Type == typeof(long))
		{
			result = (long)m_addr;
			return true;
		}
		if (binder.Type == typeof(string))
		{
			if (m_type.IsString)
			{
				result = m_type.GetValue(m_addr);
			}
			else
			{
				result = ToString();
			}
			return true;
		}
		result = null;
		return false;
	}

	public int GetLength()
	{
		if (m_type == null)
		{
			return -1;
		}
		if (m_len != -1)
		{
			return m_len;
		}
		if (m_type.IsArray)
		{
			m_len = m_type.GetArrayLength(m_addr);
			return m_len;
		}
		if (IsDictionary())
		{
			ClrInstanceField fieldByName = m_type.GetFieldByName("count");
			m_len = (int)fieldByName.GetValue(m_addr);
			return m_len;
		}
		if (IsList())
		{
			ClrInstanceField fieldByName2 = m_type.GetFieldByName("_size");
			m_len = (int)fieldByName2.GetValue(m_addr);
			return m_len;
		}
		throw new InvalidOperationException("Object does not have a length associated with it.");
	}

	public ClrType GetDictionaryKeyType()
	{
		dynamic val = ((dynamic)this).entries;
		return ((ClrType)val).ComponentType.GetFieldByName("key")?.Type;
	}

	public ClrType GetDictionaryValueType()
	{
		dynamic val = ((dynamic)this).entries;
		return ((ClrType)val).ComponentType.GetFieldByName("value")?.Type;
	}

	public IList<Tuple<dynamic, dynamic>> GetDictionaryItems()
	{
		if (m_type == null || !IsDictionary())
		{
			throw new InvalidOperationException("Can only call GetDictionaryItems on a System.Collections.Generic.Dictionary object.");
		}
		dynamic val = ((dynamic)this).entries;
		if (val.IsNull())
		{
			return new Tuple<object, object>[0];
		}
		int num = ((dynamic)this).count;
		Tuple<object, object>[] array = new Tuple<object, object>[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new Tuple<object, object>(val[i].key, val[i].value);
		}
		return array;
	}

	public override bool TryGetMember(GetMemberBinder binder, out object result)
	{
		if (IsNull())
		{
			result = new ClrNullValue(m_heap);
			return true;
		}
		ClrInstanceField clrInstanceField = null;
		if (binder.IgnoreCase)
		{
			foreach (ClrInstanceField field in m_type.Fields)
			{
				if (field.Name.Equals(binder.Name, StringComparison.CurrentCultureIgnoreCase))
				{
					clrInstanceField = field;
					break;
				}
			}
		}
		else
		{
			clrInstanceField = m_type.GetFieldByName(binder.Name);
		}
		if (clrInstanceField == null)
		{
			if (ClrDynamicClass.GetStaticField(m_heap, m_type, binder, out result))
			{
				return true;
			}
			throw new InvalidOperationException($"Type '{m_type.Name}' does not contain a '{binder.Name}' field.");
		}
		if (clrInstanceField.IsPrimitive)
		{
			object fieldValue = clrInstanceField.GetValue(m_addr, m_inner);
			if (fieldValue == null)
			{
				result = new ClrNullValue(m_heap);
			}
			else
			{
				result = new ClrPrimitiveValue(fieldValue, clrInstanceField.ElementType);
			}
			return true;
		}
		if (clrInstanceField.IsValueClass)
		{
			ulong fieldAddress = clrInstanceField.GetAddress(m_addr, m_inner);
			result = new ClrObject(m_heap, clrInstanceField.Type, fieldAddress, inner: true);
			return true;
		}
		if (clrInstanceField.ElementType == ClrElementType.String)
		{
			ulong value = clrInstanceField.GetAddress(m_addr, m_inner);
			if (!m_heap.Runtime.ReadPointer(value, out value))
			{
				result = new ClrNullValue(m_heap);
				return true;
			}
			result = new ClrObject(m_heap, clrInstanceField.Type, value);
			return true;
		}
		object fieldValue2 = clrInstanceField.GetValue(m_addr, m_inner);
		if (fieldValue2 == null)
		{
			result = new ClrNullValue(m_heap);
		}
		else
		{
			result = new ClrObject(m_heap, clrInstanceField.Type, (ulong)fieldValue2);
		}
		return true;
	}

	private bool IsStringDict()
	{
		if (m_type.Name.Length > 52)
		{
			return m_type.Name.Substring(38, 13) == "System.String";
		}
		return false;
	}
}
