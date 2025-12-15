using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DebugDiag.DbgLib;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents a global List of <c>NetResult</c> objects. An instance of this object is returned by running <c>AnalysisService.RunAnalysisRules</c> 
/// function of the <c>AnalysisService</c> object.
/// </summary>
[KnownType(typeof(NetResult))]
public class NetResults : List<NetResult>, IResults, IEnumerable
{
	/// <summary>
	/// This property returns an Integer value of number of results (Errors/Warnings/Information) reported by Analyzer. 
	/// </summary>
	public new int Count => base.Count;

	/// <summary>
	/// This property returns the NetResult based on the index parameter
	/// </summary>
	/// <param name="Index">integer value</param>
	/// <returns>NetResult located on index</returns>
	public new IResult this[int Index] => base[Index];

	public NetResults()
	{
	}

	/// <summary>
	/// Default Constructor
	/// </summary>
	/// <param name="results">Collection of results obtained from Dbglib</param>
	public NetResults(IResults results)
	{
		foreach (IResult result in results)
		{
			Add(new NetResult(result));
		}
	}

	/// <summary>
	/// Method that returns the collection enumerator
	/// </summary>
	/// <returns>IEnumerator object to access the items in the List</returns>
	public new IEnumerator GetEnumerator()
	{
		return base.GetEnumerator();
	}
}
