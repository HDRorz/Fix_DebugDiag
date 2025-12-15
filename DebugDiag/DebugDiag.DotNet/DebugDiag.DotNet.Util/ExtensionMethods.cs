using Microsoft.Diagnostics.Runtime;

namespace DebugDiag.DotNet.Util;

public static class ExtensionMethods
{
	public static string GetSafely(this string[] array, int index, string valueIfOutOfBounds = "N/A")
	{
		if (array.GetUpperBound(0) < index)
		{
			return valueIfOutOfBounds;
		}
		return array[index];
	}

	public static ClrType GetObjectTypeSafe(this ClrHeap heap, ulong endOfGCRootList)
	{
		ClrType result = null;
		try
		{
			result = heap.GetObjectType(endOfGCRootList);
		}
		catch
		{
		}
		return result;
	}
}
