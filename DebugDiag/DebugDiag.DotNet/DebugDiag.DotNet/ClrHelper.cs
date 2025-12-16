using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using ClrObject = Microsoft.Diagnostics.RuntimeExt.ClrObject;
using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.DotNet;

/// <summary>
/// Helper class used to work with Microsoft.Diagnostics.Runtime (CLRMD)
/// PS: Most of the methods in this class were imported from MEX project (Extension)
/// </summary>
public static class ClrHelper
{
	/// <summary>
	/// Return object's field value in a safe way (checking ClrType and if is an ObectReference, Primitive or Value type)
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	public static dynamic SafeGetObj(ClrObject obj, string fieldName)
	{
		ClrType heapType = obj.GetHeapType();
		return SafeGetObj(obj, heapType.GetFieldByName(fieldName));
	}

	/// <summary>
	/// Return object's field value in a safe way (checking ClrType and if is an ObectReference, Primitive or Value type)
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="field"></param>
	/// <returns></returns>
	public static dynamic SafeGetObj(ClrObject obj, ClrInstanceField field)
	{
		ClrType heapType = obj.GetHeapType();
		if (field != null)
		{
			if (!field.IsObjectReference)
			{
				if (field.IsPrimitive)
				{
					return new ClrPrimitiveValue(field.GetValue(obj.GetValue()), field.ElementType);
				}
				ulong address = field.GetAddress(obj.GetValue());
				return new ClrObject(heapType.Heap, field.Type, address, inner: true);
			}
			ulong value = field.GetAddress(obj.GetValue());
			if (value != 0L)
			{
				heapType.Heap.Runtime.ReadPointer(value, out value);
				if (value != 0L)
				{
					return heapType.Heap.GetDynamicObject(value);
				}
			}
		}
		return new ClrNullValue(heapType.Heap);
	}

	/// <summary>
	/// Return the HeapType name of an object
	/// </summary>
	/// <param name="clrObj"></param>
	/// <returns></returns>
	public static string GetTypeName(ClrObject clrObj)
	{
		if (clrObj == null || clrObj.IsNull())
		{
			return string.Empty;
		}
		ClrType clrType = clrObj.GetHeapType();
		if (clrType.IsRuntimeType)
		{
			clrType = clrType.GetRuntimeType(clrObj.GetValue());
			if (clrType == null)
			{
				return string.Empty;
			}
		}
		return clrType.Name;
	}

	/// <summary>
	/// Check if object is null
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static bool IsNull(dynamic obj)
	{
		if (obj == null || obj is ClrNullValue)
		{
			return true;
		}
		if (obj is ClrObject)
		{
			return obj.IsNull();
		}
		return false;
	}

	/// <summary>
	/// Return the value of an enum field in string
	/// </summary>
	/// <param name="obj">Object that has enum field</param>
	/// <param name="fieldName">Name of the enum field</param>
	/// <returns></returns>
	public static string EnumValueAsString(ClrObject obj, string fieldName)
	{
		ClrType heapType = obj.GetHeapType();
		bool isValueClass = heapType.IsValueClass;
		ClrInstanceField fieldByName = heapType.GetFieldByName(fieldName);
		return EnumValueAsString(fieldByName.Type, fieldByName.GetValue(obj.GetValue(), isValueClass));
	}

	/// <summary>
	/// Return enum's name based on its value
	/// </summary>
	/// <param name="type">Type of Enum</param>
	/// <param name="value">Current value of enum</param>
	/// <returns></returns>
	public static string EnumValueAsString(ClrType type, object value)
	{
		if (!type.IsEnum)
		{
			return value.ToString();
		}
		ulong num = EnumValueToUInt64(value);
		string enumName = type.GetEnumName(value);
		if (!string.IsNullOrEmpty(enumName))
		{
			return $"{enumName}";
		}
		if (num == 0L)
		{
			return "0";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string enumName2 in type.GetEnumNames())
		{
			if (!type.TryGetEnumValue(enumName2, out object value2))
			{
				continue;
			}
			ulong num2 = EnumValueToUInt64(value2);
			if ((num & num2) == num2)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(enumName2);
			}
		}
		stringBuilder.AppendFormat(" (0x{0:x})", value);
		return stringBuilder.ToString();
	}

	private static ulong EnumValueToUInt64(object value)
	{
		ulong result = 0uL;
		switch (System.Convert.GetTypeCode(value))
		{
		case TypeCode.Boolean:
		case TypeCode.Char:
		case TypeCode.Byte:
		case TypeCode.UInt16:
		case TypeCode.UInt32:
		case TypeCode.UInt64:
			result = System.Convert.ToUInt64(value);
			break;
		case TypeCode.SByte:
		case TypeCode.Int16:
		case TypeCode.Int32:
		case TypeCode.Int64:
			result = (ulong)System.Convert.ToInt64(value);
			break;
		}
		return result;
	}

	public static bool Is(ClrObject obj, string typeName)
	{
		return obj.GetHeapType().Name == typeName;
	}

	public static bool IsGenericOf(ClrObject obj, string typeName)
	{
		ClrType heapType = obj.GetHeapType();
		if (heapType.Name.StartsWith(typeName + "<"))
		{
			return heapType.Name.EndsWith(">");
		}
		return false;
	}

	/// <summary>
	/// Return true if class (type) has any baseclass within typeName provided. (Recursively)
	/// </summary>
	/// <param name="type"></param>
	/// <param name="typeName">name of base type</param>
	/// <returns></returns>
	public static bool IsSubclassOf(ClrType type, string typeName)
	{
		while (type != null)
		{
			if (type.Name == typeName)
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	public static IEnumerable<Tuple<ClrObject, NetDbgThread>> EnumObjectFromThreadStack(string[] typeName, List<NetDbgThread> threads)
	{
		List<Tuple<ClrObject, NetDbgThread>> items = new List<Tuple<ClrObject, NetDbgThread>>();
		threads.ForEach(delegate(NetDbgThread thread)
		{
			IEnumerable<Tuple<ClrObject, NetDbgThread>> collection = from w in thread.EnumerateStackObjects()
				where typeName.Any((string t) => t.Contains(w.GetType().Name.ToString()))
				select w into g
				group g by g.GetValue() into s
				select new Tuple<ClrObject, NetDbgThread>(s.First(), thread);
			items.AddRange(collection);
		});
		return items;
	}

	public static T Convert<T>(ClrObject obj)
	{
		if (TryConvert<T>(obj, out var result))
		{
			return result;
		}
		throw new InvalidOperationException($"Could not convert {obj} to {typeof(T)}");
	}

	public static T Convert<T>(ClrPrimitiveValue obj)
	{
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)new IntPtr(System.Convert.ToInt64(obj.GetValue()));
		}
		return (T)(dynamic)obj;
	}

	public static T Convert<T>(ClrNullValue obj)
	{
		return default(T);
	}

	private static bool TryConvert<T>(ClrObject obj, out T result)
	{
		try
		{
			result = default(T);
			object obj2 = obj;
			ClrType heapType = obj.GetHeapType();
			if (heapType.ElementType == ClrElementType.String)
			{
				try
				{
					obj2 = GetStringValue(obj);
				}
				catch
				{
					return false;
				}
			}
			else if (heapType.IsEnum)
			{
				obj2 = heapType.GetValue(obj.GetValue());
				if (typeof(T) == typeof(string))
				{
					obj2 = EnumValueAsString(heapType, obj2);
				}
			}
			else if (heapType.IsPrimitive && heapType.HasSimpleValue)
			{
				obj2 = heapType.GetValue(obj.GetValue());
			}
			else if (heapType.Name == "System.DateTime")
			{
				obj2 = DateTime.FromBinary((long)(ulong)((dynamic)obj).dateData);
			}
			else if (heapType.Name == "System.TimeSpan")
			{
				obj2 = TimeSpan.FromTicks((long)((dynamic)obj)._ticks);
			}
			else if (heapType.Name == "System.Guid")
			{
				int a = ((dynamic)obj)._a;
				short b = ((dynamic)obj)._b;
				short c = ((dynamic)obj)._c;
				byte[] d = new byte[8]
				{
					((dynamic)obj)._d,
					((dynamic)obj)._e,
					((dynamic)obj)._f,
					((dynamic)obj)._g,
					((dynamic)obj)._h,
					((dynamic)obj)._i,
					((dynamic)obj)._j,
					((dynamic)obj)._k
				};
				obj2 = new Guid(a, b, c, d);
			}
			else if (heapType.Name == "System.Version")
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(((dynamic)obj)._Major.GetValue().ToString()).Append(".").Append(((dynamic)obj)._Minor.GetValue().ToString());
				if (((dynamic)obj)._Build.GetValue().ToString() != "-1")
				{
					stringBuilder.Append(".").Append(((dynamic)obj)._Build.GetValue().ToString());
				}
				if (((dynamic)obj)._Revision.GetValue().ToString() != "-1")
				{
					stringBuilder.Append(".").Append(((dynamic)obj)._Revision.GetValue().ToString());
				}
				obj2 = stringBuilder.ToString();
			}
			else if (heapType.Name == "System.Decimal")
			{
				bool isNegative = ((int)((dynamic)obj).flags & 0x80000000u) != 0;
				byte scale = (byte)(((int)((dynamic)obj).flags & 0x7FFFFFFF) >> 16);
				obj2 = new decimal((int)((dynamic)obj).lo, (int)((dynamic)obj).mid, (int)((dynamic)obj).hi, isNegative, scale);
			}
			else if (heapType.IsRuntimeType)
			{
				obj2 = heapType.GetRuntimeType(obj.GetValue());
				if (typeof(T) == typeof(string) && obj2 != null)
				{
					obj2 = ((ClrType)obj2).Name;
				}
			}
			else if (heapType.Name == "System.Uri" && typeof(T) == typeof(string))
			{
				obj2 = (string)((dynamic)obj).m_String;
			}
			else if (heapType.Name == "System.Net.IPEndPoint" && typeof(T) == typeof(string))
			{
				obj2 = EndPointToString(obj);
			}
			else if (heapType.Name == "System.Net.DnsEndPoint" && typeof(T) == typeof(string))
			{
				obj2 = EndPointToString(obj);
			}
			if (obj2 != null)
			{
				if (heapType.Name == typeof(T).FullName)
				{
					result = (T)obj2;
					return true;
				}
				if (obj2 is IConvertible)
				{
					try
					{
						result = (T)System.Convert.ChangeType(obj2, typeof(T));
						return true;
					}
					catch
					{
						return false;
					}
				}
				if (typeof(T) == typeof(string))
				{
					result = (T)(object)System.Convert.ToString(obj2);
					return true;
				}
				return false;
			}
		}
		catch
		{
			result = default(T);
			return false;
		}
		return false;
	}

	public static string EndPointToString(dynamic ep)
	{
		int num = 0;
		string text = "";
		if ((!ClrHelper.IsNull(ep)))
		{
			if (ClrHelper.Is(ep, "System.Net.IPEndPoint"))
			{
				IPAddress iPAddress = ClrHelper.ToIPAddress(ep.m_Address);
				if (iPAddress != null)
				{
					text = iPAddress.ToString();
				}
				num = ClrHelper.Convert<int>(ep.m_Port);
			}
			else if (ClrHelper.Is(ep, "System.Net.DnsEndPoint"))
			{
				text = (string)ep.m_Host;
				num = ClrHelper.Convert<int>(ep.m_Port);
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			return $"{text}:{num}";
		}
		return "";
	}

	private static string GetStringValue(ClrObject obj)
	{
		return (string)(dynamic)obj;
	}

	public static IPAddress ToIPAddress(dynamic addr)
	{
		if ((int)ClrHelper.Convert<int>(addr.m_Family) == 23)
		{
			dynamic val = addr.m_Numbers;
			byte[] array = new byte[16];
			for (int i = 0; i < 8; i++)
			{
				array[i * 2] = (byte)((ushort)val[i] >> 8);
				array[i * 2 + 1] = (byte)(ushort)val[i];
			}
			return new IPAddress(array, (long)addr.m_ScopeId);
		}
		return new IPAddress(ClrHelper.Convert<long>(addr.m_Address));
	}

	public static string ToString(ClrObject obj)
	{
		if (obj.IsNull())
		{
			return "(null)";
		}
		if (TryConvert<string>(obj, out var result))
		{
			return result;
		}
		return "{" + obj.GetHeapType().Name + "}";
	}
}
