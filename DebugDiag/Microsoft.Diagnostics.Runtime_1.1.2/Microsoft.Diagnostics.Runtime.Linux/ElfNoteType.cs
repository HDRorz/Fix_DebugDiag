namespace Microsoft.Diagnostics.Runtime.Linux;

internal enum ElfNoteType : uint
{
	PrpsStatus = 1u,
	PrpsFpreg = 2u,
	PrpsInfo = 3u,
	TASKSTRUCT = 4u,
	Aux = 6u,
	File = 1179208773u
}
