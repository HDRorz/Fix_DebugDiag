using System;

namespace DebugDiag.DotNet;

internal class DacNotFoundException : Exception
{
	public string SymbolProbeInfo { get; set; }

	public string DacFileName { get; set; }

	public string DacLocation { get; set; }

	public DacNotFoundException(string dacFileName, string dacLocation, string symbolProbeInfo)
		: base($"CLR is loaded in the target, but the correct dac file cannot be found.  DacFileName:  {dacFileName}.  DacLocation:  {dacLocation}")
	{
		DacFileName = dacFileName;
		DacLocation = dacLocation;
		SymbolProbeInfo = symbolProbeInfo;
	}
}
