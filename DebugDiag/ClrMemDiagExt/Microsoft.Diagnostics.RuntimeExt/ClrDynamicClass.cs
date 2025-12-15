using System;
using System.Dynamic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public class ClrDynamicClass : DynamicObject
{
	private ClrHeap m_heap;

	private ClrType m_type;

	public ClrDynamicClass(ClrHeap heap, ClrType type)
	{
		if (heap == null)
		{
			throw new ArgumentNullException("heap");
		}
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		m_heap = heap;
		m_type = type;
	}

	public override bool TryConvert(ConvertBinder binder, out object result)
	{
		if (binder.Type == typeof(ClrType))
		{
			result = m_type;
			return true;
		}
		return base.TryConvert(binder, out result);
	}

	public override bool TryGetMember(GetMemberBinder binder, out object result)
	{
		if (GetStaticField(m_heap, m_type, binder, out result))
		{
			return true;
		}
		throw new InvalidOperationException($"Type '{m_type.Name}' does not contain a static '{binder.Name}' field.");
	}

	internal static bool GetStaticField(ClrHeap heap, ClrType type, GetMemberBinder binder, out object result)
	{
		result = null;
		bool result2 = false;
		ClrStaticField clrStaticField = null;
		StringComparison comparisonType = (binder.IgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
		foreach (ClrStaticField staticField in type.StaticFields)
		{
			if (staticField.Name.Equals(binder.Name, comparisonType))
			{
				clrStaticField = staticField;
				break;
			}
		}
		if (clrStaticField != null)
		{
			result = new StaticVariableValueWrapper(heap, clrStaticField);
			result2 = true;
		}
		return result2;
	}
}
