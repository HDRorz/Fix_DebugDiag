namespace Microsoft.Diagnostics.Runtime;

public interface IDataReader2 : IDataReader
{
	uint ProcessId { get; }
}
