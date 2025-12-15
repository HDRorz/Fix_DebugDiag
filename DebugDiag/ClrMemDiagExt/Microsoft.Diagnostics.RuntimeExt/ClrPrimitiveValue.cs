using System;
using System.Dynamic;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public class ClrPrimitiveValue : DynamicObject
{
	private object m_value;

	private ClrElementType m_type;

	public ClrPrimitiveValue(object value, ClrElementType type)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		m_value = value;
		m_type = type;
	}

	public override bool TryConvert(ConvertBinder binder, out object result)
	{
		if (binder.Type == typeof(string))
		{
			result = m_value.ToString();
		}
		else
		{
			result = m_value;
		}
		return true;
	}

	public object GetValue()
	{
		return m_value;
	}
}
