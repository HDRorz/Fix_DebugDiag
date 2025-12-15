using System.Collections.Generic;
using System.Linq;

namespace DebugDiag.DotNet.Mex;

internal class UdeScanRule : MexWrapperBase
{
	public class ReportedData
	{
		private readonly dynamic _realMexObject;

		public string OptionalData
		{
			get
			{
				return _realMexObject.VerboseOutput;
			}
			set
			{
				_realMexObject.VerboseOutput = value;
			}
		}

		public string SSID
		{
			get
			{
				return _realMexObject.SSID;
			}
			set
			{
				_realMexObject.SSID = value;
			}
		}

		public uint KB
		{
			get
			{
				return _realMexObject.KB;
			}
			set
			{
				_realMexObject.KB = value;
			}
		}

		public uint Bemis
		{
			get
			{
				return _realMexObject.Bemis;
			}
			set
			{
				_realMexObject.Bemis = value;
			}
		}

		public string BugDB
		{
			get
			{
				return _realMexObject.BugDB;
			}
			set
			{
				_realMexObject.BugDB = value;
			}
		}

		public uint BugID
		{
			get
			{
				return _realMexObject.BugID;
			}
			set
			{
				_realMexObject.BugID = value;
			}
		}

		public ReportedData(dynamic realMexObject)
		{
			_realMexObject = realMexObject;
		}
	}

	public string VerboseOutput
	{
		get
		{
			return RealMexObject.VerboseOutput;
		}
		set
		{
			RealMexObject.VerboseOutput = value;
		}
	}

	public string HtmlOutput
	{
		get
		{
			return RealMexObject.HtmlOutput;
		}
		set
		{
			RealMexObject.HtmlOutput = value;
		}
	}

	public bool SkipWinde
	{
		get
		{
			return RealMexObject.SkipWinde;
		}
		set
		{
			RealMexObject.SkipWinde = value;
		}
	}

	public IEnumerable<ReportedData> ReportedDatas => ((IEnumerable<object>)RealMexObject.ReportedDatas).Select((object o) => new ReportedData(o));

	public UdeScanRule(object realMexObject)
		: base(realMexObject)
	{
		SkipWinde = true;
	}

	public void RunRule()
	{
		RealMexObject.RunRule();
	}
}
