using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

internal class MDType : IMDType
{
	private ClrType m_type;

	public static IMDType Construct(ClrType type)
	{
		if (type == null)
		{
			return null;
		}
		return new MDType(type);
	}

	public MDType(ClrType type)
	{
		m_type = type;
		if (type == null)
		{
			throw new NullReferenceException();
		}
	}

	public void GetName(out string pName)
	{
		pName = m_type.Name;
	}

	public void GetSize(ulong objRef, out ulong pSize)
	{
		pSize = m_type.GetSize(objRef);
	}

	public void ContainsPointers(out int pContainsPointers)
	{
		pContainsPointers = (m_type.ContainsPointers ? 1 : 0);
	}

	public void GetCorElementType(out int pCET)
	{
		pCET = (int)m_type.ElementType;
	}

	public void GetBaseType(out IMDType ppBaseType)
	{
		ppBaseType = Construct(m_type.BaseType);
	}

	public void GetArrayComponentType(out IMDType ppArrayComponentType)
	{
		ppArrayComponentType = Construct(m_type.ArrayComponentType);
	}

	public void GetCCW(ulong addr, out IMDCCW ppCCW)
	{
		if (m_type.IsCCW(addr))
		{
			ppCCW = new MDCCW(m_type.GetCCWData(addr));
		}
		else
		{
			ppCCW = null;
		}
	}

	public void GetRCW(ulong addr, out IMDRCW ppRCW)
	{
		if (m_type.IsRCW(addr))
		{
			ppRCW = new MDRCW(m_type.GetRCWData(addr));
		}
		else
		{
			ppRCW = null;
		}
	}

	public void IsArray(out int pIsArray)
	{
		pIsArray = (m_type.IsArray ? 1 : 0);
	}

	public void IsFree(out int pIsFree)
	{
		pIsFree = (m_type.IsFree ? 1 : 0);
	}

	public void IsException(out int pIsException)
	{
		pIsException = (m_type.IsException ? 1 : 0);
	}

	public void IsEnum(out int pIsEnum)
	{
		pIsEnum = (m_type.IsEnum ? 1 : 0);
	}

	public void GetEnumElementType(out int pValue)
	{
		pValue = (int)m_type.GetEnumElementType();
	}

	public void GetEnumNames(out IMDStringEnum ppEnum)
	{
		ppEnum = new MDStringEnum(m_type.GetEnumNames().ToArray());
	}

	public void GetEnumValueInt32(string name, out int pValue)
	{
		if (!m_type.TryGetEnumValue(name, out pValue))
		{
			new InvalidOperationException("Mismatched type.");
		}
	}

	public void GetFieldCount(out int pCount)
	{
		pCount = m_type.Fields.Count;
	}

	public void GetField(int index, out IMDField ppField)
	{
		ppField = new MDField(m_type.Fields[index]);
	}

	public int GetFieldData(ulong obj, int interior, int count, MD_FieldData[] fields, out int pNeeded)
	{
		int count2 = m_type.Fields.Count;
		if (fields == null || count == 0)
		{
			pNeeded = count2;
			return 1;
		}
		for (int i = 0; i < count && i < count2; i++)
		{
			ClrInstanceField clrInstanceField = m_type.Fields[i];
			fields[i].name = clrInstanceField.Name;
			fields[i].type = clrInstanceField.Type.Name;
			fields[i].offset = clrInstanceField.Offset;
			fields[i].size = clrInstanceField.Size;
			fields[i].corElementType = (int)clrInstanceField.ElementType;
			if (clrInstanceField.ElementType == ClrElementType.Struct || clrInstanceField.ElementType == ClrElementType.String || clrInstanceField.ElementType == ClrElementType.Float || clrInstanceField.ElementType == ClrElementType.Double)
			{
				fields[i].value = clrInstanceField.GetFieldAddress(obj, interior != 0);
				continue;
			}
			object fieldValue = clrInstanceField.GetFieldValue(obj, interior != 0);
			if (fieldValue == null)
			{
				fields[i].value = 0uL;
			}
			else if (fieldValue is int)
			{
				fields[i].value = (ulong)(int)fieldValue;
			}
			else if (fieldValue is uint)
			{
				fields[i].value = (uint)fieldValue;
			}
			else if (fieldValue is long)
			{
				fields[i].value = (ulong)(long)fieldValue;
			}
			else if (fieldValue is ulong)
			{
				fields[i].value = (ulong)fieldValue;
			}
			else if (fieldValue is byte)
			{
				fields[i].value = (byte)fieldValue;
			}
			else if (fieldValue is sbyte)
			{
				fields[i].value = (ulong)(sbyte)fieldValue;
			}
			else if (fieldValue is ushort)
			{
				fields[i].value = (ushort)fieldValue;
			}
			else if (fieldValue is short)
			{
				fields[i].value = (ulong)(short)fieldValue;
			}
			else if (fieldValue is bool)
			{
				fields[i].value = (ulong)(((bool)fieldValue) ? 1 : 0);
			}
		}
		if (count < count2)
		{
			pNeeded = count;
			return 1;
		}
		pNeeded = count2;
		return 0;
	}

	public void GetStaticFieldCount(out int pCount)
	{
		pCount = m_type.StaticFields.Count;
	}

	public void GetStaticField(int index, out IMDStaticField ppStaticField)
	{
		ppStaticField = new MDStaticField(m_type.StaticFields[index]);
	}

	public void GetThreadStaticFieldCount(out int pCount)
	{
		pCount = m_type.ThreadStaticFields.Count;
	}

	public void GetThreadStaticField(int index, out IMDThreadStaticField ppThreadStaticField)
	{
		ppThreadStaticField = new MDThreadStaticField(m_type.ThreadStaticFields[index]);
	}

	public void GetArrayLength(ulong objRef, out int pLength)
	{
		pLength = m_type.GetArrayLength(objRef);
	}

	public void GetArrayElementAddress(ulong objRef, int index, out ulong pAddr)
	{
		pAddr = m_type.GetArrayElementAddress(objRef, index);
	}

	public void GetArrayElementValue(ulong objRef, int index, out IMDValue ppValue)
	{
		object arrayElementValue = m_type.GetArrayElementValue(objRef, index);
		ClrElementType cet = ((m_type.ArrayComponentType != null) ? m_type.ArrayComponentType.ElementType : ClrElementType.Unknown);
		ppValue = new MDValue(arrayElementValue, cet);
	}

	public void EnumerateReferences(ulong objRef, out IMDReferenceEnum ppEnum)
	{
		List<MD_Reference> refs = new List<MD_Reference>();
		m_type.EnumerateRefsOfObject(objRef, delegate(ulong child, int offset)
		{
			if (child != 0L)
			{
				MD_Reference item = default(MD_Reference);
				item.address = child;
				item.offset = offset;
				refs.Add(item);
			}
		});
		ppEnum = new ReferenceEnum(refs);
	}

	public void EnumerateInterfaces(out IMDInterfaceEnum ppEnum)
	{
		ppEnum = new InterfaceEnum(m_type.Interfaces);
	}
}
