using System;

namespace Microsoft.Diagnostics.Runtime;

public interface IAddressableTypedEntity : IEquatable<IAddressableTypedEntity>
{
	ulong Address { get; }

	ClrType Type { get; }

	T GetField<T>(string fieldName) where T : struct;

	string GetStringField(string fieldName);

	ClrValueClass GetValueClassField(string fieldName);

	ClrObject GetObjectField(string fieldName);
}
