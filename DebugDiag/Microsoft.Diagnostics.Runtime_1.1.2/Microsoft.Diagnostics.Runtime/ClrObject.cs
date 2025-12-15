using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Runtime;

[DebuggerDisplay("Address={HexAddress}, Type={Type.Name}")]
public struct ClrObject : IAddressableTypedEntity, IEquatable<IAddressableTypedEntity>, IEquatable<ClrObject>
{
	public ulong Address { get; private set; }

	public string HexAddress => Address.ToString("x");

	public ClrType Type { get; private set; }

	public bool IsNull => Address == 0;

	public ulong Size => Type.GetSize(Address);

	public bool IsBoxed => !Type.IsObjectReference;

	public bool IsArray => Type.IsArray;

	public int Length
	{
		get
		{
			if (!IsArray)
			{
				throw new InvalidOperationException();
			}
			return Type.GetArrayLength(Address);
		}
	}

	public bool ContainsPointers
	{
		get
		{
			if (Type != null)
			{
				return Type.ContainsPointers;
			}
			return false;
		}
	}

	internal static ClrObject Create(ulong address, ClrType type)
	{
		ClrObject result = default(ClrObject);
		result.Address = address;
		result.Type = type;
		return result;
	}

	public ClrObject(ulong address, ClrType type)
	{
		Address = address;
		Type = type;
	}

	public IEnumerable<ClrObject> EnumerateObjectReferences(bool carefully = false)
	{
		return Type.Heap.EnumerateObjectReferences(Address, Type, carefully);
	}

	public override string ToString()
	{
		return $"{Type?.Name} {Address:x}";
	}

	public static explicit operator string(ClrObject obj)
	{
		if (!obj.Type.IsString)
		{
			throw new InvalidOperationException("Object {obj} is not a string.");
		}
		return (string)obj.Type.GetValue(obj.Address);
	}

	public static implicit operator ulong(ClrObject clrObject)
	{
		return clrObject.Address;
	}

	public ClrObject GetObjectField(string fieldName)
	{
		if (IsNull)
		{
			throw new NullReferenceException();
		}
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		if (!fieldByName.IsObjectReference)
		{
			throw new ArgumentException("Field '" + Type.Name + "." + fieldName + "' is not an object reference.");
		}
		ClrHeap heap = Type.Heap;
		ulong address = fieldByName.GetAddress(Address);
		if (!heap.ReadPointer(address, out var value))
		{
			throw new MemoryReadException(address);
		}
		ClrType objectType = heap.GetObjectType(value);
		return new ClrObject(value, objectType);
	}

	public ClrValueClass GetValueClassField(string fieldName)
	{
		if (IsNull)
		{
			throw new NullReferenceException();
		}
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		if (!fieldByName.IsValueClass)
		{
			throw new ArgumentException("Field '" + Type.Name + "." + fieldName + "' is not a ValueClass.");
		}
		if (fieldByName.Type == null)
		{
			throw new Exception("Field does not have an associated class.");
		}
		_ = Type.Heap;
		return new ClrValueClass(fieldByName.GetAddress(Address), fieldByName.Type, interior: true);
	}

	public T GetField<T>(string fieldName) where T : struct
	{
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		return (T)fieldByName.GetValue(Address);
	}

	public string GetStringField(string fieldName)
	{
		ulong fieldAddress = GetFieldAddress(fieldName, ClrElementType.String, "string");
		RuntimeBase runtimeBase = (RuntimeBase)Type.Heap.Runtime;
		if (!runtimeBase.ReadPointer(fieldAddress, out var value))
		{
			throw new MemoryReadException(fieldAddress);
		}
		if (value == 0L)
		{
			return null;
		}
		if (!runtimeBase.ReadString(value, out var value2))
		{
			throw new MemoryReadException(value);
		}
		return value2;
	}

	private ulong GetFieldAddress(string fieldName, ClrElementType element, string typeName)
	{
		if (IsNull)
		{
			throw new NullReferenceException();
		}
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		if (fieldByName.ElementType != element)
		{
			throw new InvalidOperationException("Field '" + Type.Name + "." + fieldName + "' is not of type '" + typeName + "'.");
		}
		return fieldByName.GetAddress(Address);
	}

	public bool Equals(ClrObject other)
	{
		return Address == other.Address;
	}

	public override bool Equals(object other)
	{
		if (other == null)
		{
			return false;
		}
		if (other is ClrObject)
		{
			return Equals((ClrObject)other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Address.GetHashCode();
	}

	public bool Equals(IAddressableTypedEntity other)
	{
		if (other is ClrObject)
		{
			return Equals((ClrObject)(object)other);
		}
		return false;
	}

	public static bool operator ==(ClrObject left, ClrObject right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ClrObject left, ClrObject right)
	{
		return !left.Equals(right);
	}
}
