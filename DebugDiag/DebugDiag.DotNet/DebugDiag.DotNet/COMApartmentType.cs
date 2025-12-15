namespace DebugDiag.DotNet;

/// <summary>
/// Enumaration for the COM threading Apartment type
/// </summary>
public enum COMApartmentType
{
	Uninitialized,
	Neutral,
	MTA,
	STA,
	Unknown
}
