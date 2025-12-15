namespace DebugDiag.DotNet;

/// <summary>
/// Enumeration for the Search Options
/// </summary>
public enum FrameSearchOptions
{
	/// <summary>
	/// Return a match is searchText is found anywhere in the frame text
	/// </summary>
	PartialMatch,
	/// <summary>
	/// Return a match if the searchText matches the entire FrameText line including module name, function name, and parameter list - but ignoring offsets.  
	/// Module names can be ommtted for CLR frames.  
	/// Parameter lists apply only to CLR frames.
	/// </summary>
	CompleteMatchIgnoreOffsets,
	/// <summary>
	/// Return a match if the searchText matches the entire FrameText line including module name, function name, parameter list, AND offsets.   
	/// Module names can be ommtted for CLR frames.  
	/// Parameter lists apply only to CLR frames.
	/// </summary>
	CompleteMatchIncludingOffsets
}
