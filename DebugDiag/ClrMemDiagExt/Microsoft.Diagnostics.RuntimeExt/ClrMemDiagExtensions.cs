using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace Microsoft.Diagnostics.RuntimeExt;

public static class ClrMemDiagExtensions
{
	public static dynamic GetDynamicObject(this ClrHeap heap, ulong addr)
	{
		ClrType objectType = heap.GetObjectType(addr);
		if (objectType == null)
		{
			return null;
		}
		return new ClrObject(heap, objectType, addr);
	}

	public static dynamic GetDynamicClass(this ClrHeap heap, string typeName)
	{
		ClrType clrType = (from t in heap.EnumerateTypes()
			where t != null && t.Name == typeName
			select t).FirstOrDefault();
		if (clrType == null)
		{
			return null;
		}
		return new ClrDynamicClass(heap, clrType);
	}
}
