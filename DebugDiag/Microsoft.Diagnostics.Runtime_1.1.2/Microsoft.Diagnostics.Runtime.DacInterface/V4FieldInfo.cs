using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

public readonly struct V4FieldInfo : IFieldInfo
{
	public readonly short NumInstanceFields;

	public readonly short NumStaticFields;

	public readonly short NumThreadStaticFields;

	public readonly ulong FirstFieldAddress;

	public readonly short ContextStaticOffset;

	public readonly short ContextStaticsSize;

	uint IFieldInfo.InstanceFields => (uint)NumInstanceFields;

	uint IFieldInfo.StaticFields => (uint)NumStaticFields;

	uint IFieldInfo.ThreadStaticFields => (uint)NumThreadStaticFields;

	ulong IFieldInfo.FirstField => FirstFieldAddress;
}
