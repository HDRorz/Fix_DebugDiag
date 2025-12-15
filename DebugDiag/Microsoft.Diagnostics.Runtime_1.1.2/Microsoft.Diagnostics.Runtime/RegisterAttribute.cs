using System;

namespace Microsoft.Diagnostics.Runtime;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RegisterAttribute : Attribute
{
	public string Name { get; set; }

	public RegisterType RegisterType { get; }

	public RegisterAttribute(RegisterType registerType)
	{
		RegisterType = registerType;
	}
}
