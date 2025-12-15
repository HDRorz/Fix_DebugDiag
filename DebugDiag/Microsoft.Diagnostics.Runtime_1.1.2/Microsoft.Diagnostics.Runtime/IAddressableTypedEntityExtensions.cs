using System;

namespace Microsoft.Diagnostics.Runtime;

public static class IAddressableTypedEntityExtensions
{
	public static IAddressableTypedEntity GetFieldFrom(this IAddressableTypedEntity entity, string fieldName)
	{
		ClrType clrType = entity?.Type ?? throw new ArgumentNullException("entity", "No associated type");
		if (!(clrType.GetFieldByName(fieldName) ?? throw new ArgumentException($"Type '{clrType}' does not contain a field named '{fieldName}'")).IsObjectReference)
		{
			return entity.GetValueClassField(fieldName);
		}
		return entity.GetObjectField(fieldName);
	}
}
