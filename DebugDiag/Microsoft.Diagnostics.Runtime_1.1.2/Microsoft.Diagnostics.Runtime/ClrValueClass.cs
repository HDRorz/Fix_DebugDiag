using System;

namespace Microsoft.Diagnostics.Runtime;

public struct ClrValueClass : IAddressableTypedEntity, IEquatable<IAddressableTypedEntity>
{
	private readonly bool _interior;

	public ulong Address { get; }

	public string HexAddress => Address.ToString("x");

	public ClrType Type { get; }

	internal ClrValueClass(ulong address, ClrType type, bool interior)
	{
		Address = address;
		Type = type;
		_interior = interior;
	}

	public ClrObject GetObjectField(string fieldName)
	{
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
		ulong address = fieldByName.GetAddress(Address, _interior);
		if (!heap.ReadPointer(address, out var value))
		{
			throw new MemoryReadException(address);
		}
		ClrType objectType = heap.GetObjectType(value);
		return new ClrObject(value, objectType);
	}

	public T GetField<T>(string fieldName) where T : struct
	{
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		return (T)fieldByName.GetValue(Address, _interior);
	}

	public ClrValueClass GetValueClassField(string fieldName)
	{
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
		return new ClrValueClass(fieldByName.GetAddress(Address, _interior), fieldByName.Type, interior: true);
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
		ClrInstanceField fieldByName = Type.GetFieldByName(fieldName);
		if (fieldByName == null)
		{
			throw new ArgumentException("Type '" + Type.Name + "' does not contain a field named '" + fieldName + "'");
		}
		if (fieldByName.ElementType != element)
		{
			throw new InvalidOperationException("Field '" + Type.Name + "." + fieldName + "' is not of type '" + typeName + "'.");
		}
		return fieldByName.GetAddress(Address, _interior);
	}

	public bool Equals(IAddressableTypedEntity other)
	{
		if (other != null && Address == other.Address)
		{
			return Type == other.Type;
		}
		return false;
	}
}
