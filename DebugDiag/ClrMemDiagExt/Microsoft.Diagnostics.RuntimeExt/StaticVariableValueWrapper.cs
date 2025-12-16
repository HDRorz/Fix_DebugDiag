using System.Dynamic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public class StaticVariableValueWrapper : DynamicObject
{
	private ClrHeap m_heap;

	private ClrStaticField m_field;

	public StaticVariableValueWrapper(ClrHeap heap, ClrStaticField field)
	{
		m_heap = heap;
		m_field = field;
	}

	public dynamic GetValue(ClrAppDomain appDomain)
	{
		if (m_field.IsPrimitive)
		{
			object fieldValue = m_field.GetValue(appDomain);
			if (fieldValue != null)
			{
				return new ClrPrimitiveValue(fieldValue, m_field.ElementType);
			}
		}
		else if (m_field.IsValueClass)
		{
			ulong fieldAddress = m_field.GetAddress(appDomain);
			if (fieldAddress != 0L)
			{
				return new ClrObject(m_heap, m_field.Type, fieldAddress, inner: true);
			}
		}
		else if (m_field.ElementType == ClrElementType.String)
		{
			ulong value = m_field.GetAddress(appDomain);
			if (m_heap.Runtime.ReadPointer(value, out value))
			{
				return new ClrObject(m_heap, m_field.Type, value);
			}
		}
		else
		{
			object fieldValue2 = m_field.GetValue(appDomain);
			if (fieldValue2 != null)
			{
				return new ClrObject(m_heap, m_field.Type, (ulong)fieldValue2);
			}
		}
		return new ClrNullValue(m_heap);
	}
}
