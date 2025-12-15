using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DebugDiag.DbgLib;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents a NetResult object.  An instance of this object is obtained from the <c>NetResults</c> List.
/// </summary>
[DataContract]
public class NetResult : IComparable<NetResult>, IResult
{
	private class SortbyWeightorType : IComparer<NetResult>
	{
		int IComparer<NetResult>.Compare(NetResult x, NetResult y)
		{
			if (x._type == y._type)
			{
				if (x._type == "Error" || x._type == "Warning" || x._type == "Information" || (x._type == "Other" && x._typeLabel == y._typeLabel))
				{
					if (x._weight > y._weight)
					{
						return -1;
					}
					if (x._weight < y._weight)
					{
						return 1;
					}
					return 0;
				}
				if (x._typeLabel == "Recommended")
				{
					return -1;
				}
				if (y._typeLabel == "Recommended")
				{
					return 1;
				}
				if (x._typeLabel != "Notification")
				{
					return -1;
				}
				if (y._typeLabel != "Notification")
				{
					return 1;
				}
				if (x._typeLabel == "Notification")
				{
					return -1;
				}
				if (y._typeLabel == "Notification")
				{
					return 1;
				}
			}
			else
			{
				if (x._type == "Other" && x._typeLabel == "Recommended")
				{
					return -1;
				}
				if (y._type == "Other" && y._typeLabel == "Recommended")
				{
					return 1;
				}
				if (x._type == "Other" && x._typeLabel != "Notification")
				{
					return -1;
				}
				if (y._type == "Other" && y._typeLabel != "Notification")
				{
					return 1;
				}
				if (x._type == "Error")
				{
					return -1;
				}
				if (y._type == "Error")
				{
					return 1;
				}
				if (x._type == "Warning")
				{
					return -1;
				}
				if (y._type == "Warning")
				{
					return 1;
				}
				if (x._type == "Information")
				{
					return -1;
				}
				if (y._type == "Information")
				{
					return 1;
				}
				if (x._type == "Other" && x._typeLabel == "Notification")
				{
					return -1;
				}
				if (y._type == "Other" && y._typeLabel == "Notification")
				{
					return 1;
				}
			}
			return 0;
		}
	}

	private string _source;

	private string _type;

	private string _description;

	private string _recommendation;

	private int _weight;

	private string _solutionSourceID;

	private string _dumpName;

	private string _iconFileName;

	private string _typeLabel;

	/// <summary>
	/// This property returns a string value for the source of the result.
	/// </summary>
	[DataMember]
	public string Source
	{
		get
		{
			return _source;
		}
		private set
		{
			_source = value;
		}
	}

	/// <summary>
	/// This property returns a string value for Type of results (Error/Warning/Information). 
	/// </summary>
	[DataMember]
	public string Type
	{
		get
		{
			return _type;
		}
		private set
		{
			_type = value;
		}
	}

	/// <summary>
	/// This property returns a string value of the result description. 
	/// </summary>
	[DataMember]
	public string Description
	{
		get
		{
			return _description;
		}
		internal set
		{
			_description = value;
		}
	}

	/// <summary>
	/// This property returns a string value for the recommendations to make based on results.
	/// </summary>
	[DataMember]
	public string Recommendation
	{
		get
		{
			return _recommendation;
		}
		private set
		{
			_recommendation = value;
		}
	}

	/// <summary>
	/// This property used as a means to sort the results based on importance. 
	/// For Example: Result A with weight 10 will appear higher than a result B with weight 5 in the analysis summary.
	/// </summary>
	[DataMember]
	public int Weight
	{
		get
		{
			return _weight;
		}
		private set
		{
			_weight = value;
		}
	}

	[DataMember]
	public string SolutionSourceID
	{
		get
		{
			return _solutionSourceID;
		}
		private set
		{
			_solutionSourceID = value;
		}
	}

	/// <summary>
	/// This property is used to get the associated dump for this result if any dump is associated.
	/// </summary>
	[DataMember]
	public string DumpName
	{
		get
		{
			return _dumpName;
		}
		private set
		{
			_dumpName = value;
		}
	}

	/// <summary>
	/// This property allows to customize the icon that is shown on the report if the icon is displayed
	/// </summary>
	[DataMember]
	public string IconFileName
	{
		get
		{
			if (string.IsNullOrEmpty(_iconFileName))
			{
				string text = string.Empty;
				switch (_type)
				{
				case "Error":
					text = "erroricon.png";
					break;
				case "Warning":
					text = "warningicon.png";
					break;
				case "Information":
					text = "informationicon.png";
					break;
				}
				if (!string.IsNullOrEmpty(text))
				{
					_iconFileName = text;
				}
			}
			return _iconFileName;
		}
		private set
		{
			_iconFileName = value;
		}
	}

	/// <summary>
	/// This property Allows to write a custom type of result, "Notification"  is an example of a custom result
	/// </summary>
	[DataMember]
	public string TypeLabel
	{
		get
		{
			return _typeLabel;
		}
		private set
		{
			_typeLabel = value;
		}
	}

	internal NetResult(IResult legacyResult)
	{
		_source = legacyResult.Source;
		_type = legacyResult.Type;
		_description = legacyResult.Description;
		_recommendation = legacyResult.Recommendation;
		_weight = legacyResult.Weight;
		_solutionSourceID = legacyResult.SolutionSourceID;
	}

	internal NetResult(string source, string type, string description, string recommendation, int weight, string solutionSourceID, string dumpName = "")
	{
		_source = source;
		_type = type;
		_description = description;
		_recommendation = recommendation;
		_weight = weight;
		_solutionSourceID = solutionSourceID;
		_dumpName = dumpName;
	}

	internal NetResult(string source, string typeLabel, string description, string recommendation, int weight, string solutionSourceID, string dumpName = "", string iconFileName = "")
	{
		_source = source;
		_type = "Other";
		_description = description;
		_recommendation = recommendation;
		_weight = weight;
		_solutionSourceID = solutionSourceID;
		_dumpName = dumpName;
		_typeLabel = typeLabel;
		_iconFileName = iconFileName;
	}

	/// <summary>
	/// Returns a Comparer for ordering Results by type and weight
	/// </summary>
	/// <returns></returns>
	public static IComparer<NetResult> SortResult()
	{
		return new SortbyWeightorType();
	}

	int IComparable<NetResult>.CompareTo(NetResult other)
	{
		if (_type == other._type)
		{
			if (_weight > other._weight)
			{
				return -1;
			}
			if (_weight < other._weight)
			{
				return 1;
			}
			return 0;
		}
		if (_type == "Other")
		{
			return -1;
		}
		if (other._type == "Other")
		{
			return 1;
		}
		if (_type == "Error")
		{
			return -1;
		}
		if (other._type == "Error")
		{
			return 1;
		}
		if (_type == "Warning")
		{
			return -1;
		}
		if (other._type == "Warning")
		{
			return 1;
		}
		return 0;
	}
}
