namespace Microsoft.Diagnostics.Runtime.Desktop;

internal struct IMAGE_COR_ILMETHOD
{
	public uint FlagsSizeStack;

	public uint CodeSize;

	public uint LocalVarSignatureToken;

	public const uint FormatShift = 3u;

	public const uint FormatMask = 7u;

	public const uint TinyFormat = 2u;

	public const uint mdSignatureNil = 285212672u;
}
