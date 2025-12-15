using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DebugDiag.DotNet.HtmlHelpers;

namespace DebugDiag.DotNet.Reports;

/// <summary>
/// Class used to build the report details section, each rule details is contained on a ReportSection, in the same way, for each dump analyzed on the
/// execution of the rule a section will be created.
/// </summary>
public class ReportSection : IDisposable
{
	private string _title;

	protected string _InternalID;

	private string _UID;

	private string _sectionID;

	protected MemoryStream _content;

	private int _zOrder;

	private bool _includeInTOC;

	private bool _showTOCofChildSections;

	private ReportSection _parentSection;

	private ReportSections _innerSections;

	private const string LINE_BREAK = "<BR>";

	private const string SPACE = "&nbsp;";

	protected int _level;

	protected string _ruleName = "";

	protected string _dumpName = "";

	protected string _threadName = "";

	protected SectionType _type;

	private bool _isDisposed;

	private bool _collapsible;

	private bool _collapsed;

	/// <summary>
	/// Property to set/get the title for the ReportSection
	/// </summary>
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
		}
	}

	/// <summary>
	/// Property to get the ReportId that identify the ReportSection in the collection, the Title is initialized with this value by default.
	/// </summary>
	public string SectionID
	{
		get
		{
			return _sectionID;
		}
		private set
		{
			_sectionID = value;
		}
	}

	/// <summary>
	/// Property to keep the order of the sections, if you need to change the order in which the sections are written on the report you can use
	/// this property to change it.
	/// </summary>
	public int ZOrder
	{
		get
		{
			return _zOrder;
		}
		set
		{
			_zOrder = value;
		}
	}

	/// <summary>
	/// Property that indicates whether the title of the section should be included on the Table of Contents, by default the table of contents
	/// only dispaly titles for 3 levels below the root.
	/// </summary>
	public bool IncludeInTOC
	{
		get
		{
			return _includeInTOC;
		}
		set
		{
			_includeInTOC = value;
		}
	}

	/// <summary>
	/// Property that returns the Collection of ReportSections contained in the current ReportSection.
	/// </summary>
	public ReportSections InnerSections
	{
		get
		{
			if (_innerSections == null)
			{
				_innerSections = new ReportSections();
			}
			return _innerSections;
		}
	}

	/// <summary>
	/// Property that indicates wheter or not the section will be collapsible on the report, the default is false for custom ReportSections
	/// </summary>
	public bool Collapsible
	{
		get
		{
			return _collapsible;
		}
		set
		{
			_collapsible = value;
		}
	}

	/// <summary>
	/// Property that indicates wheter or not the section will be shown collapsed by default on the report, the default is false
	/// </summary>
	public bool Collapsed
	{
		get
		{
			return _collapsed;
		}
		set
		{
			_collapsed = value;
		}
	}

	/// <summary>
	/// Property that returns a reference of the parent ReportSection of the current ReportSection
	/// </summary>
	public ReportSection Parent => _parentSection;

	/// <summary>
	/// Returns a Unique ID used for shortcuts on the HTML to go to the section using anchor tags
	/// </summary>
	public string GetUID => _InternalID;

	/// <summary>
	/// Returns the SectionType of the current section
	/// </summary>
	public SectionType SectionType => _type;

	/// <summary>
	/// Constructor for the main ResportSection
	/// </summary>
	/// <param name="SectionID">Name for the Section, the root Section is called Default</param>
	internal ReportSection(string SectionID)
	{
		_sectionID = SectionID;
		_type = SectionType.Default;
		_zOrder = 0;
		_InternalID = "";
		_includeInTOC = false;
		_showTOCofChildSections = false;
	}

	/// <summary>
	/// Constructor for the main ResportSection
	/// </summary>
	/// <param name="SectionID">Name for the Section, the root Section is called Default</param>
	/// <param name="IncludeInTOC">Name for the Section, the root Section is called Default</param>
	protected ReportSection(string SectionID, bool IncludeInTOC)
	{
		_sectionID = SectionID;
		_type = SectionType.Custom;
		_includeInTOC = IncludeInTOC;
		_showTOCofChildSections = false;
		_zOrder = -1;
	}

	/// <summary>
	/// Constructor for the ReportSection
	/// </summary>
	/// <param name="ParentSection">Parent ReportSection where the new ReportSection will be added</param>
	/// <param name="SectionID">Default ID for the section, this parameter is also used to initialize the Title property.</param>
	/// <param name="Info">Type of ReportSection being added.</param>
	private ReportSection(ReportSection ParentSection, string SectionID, SectionType Info)
	{
		if (ParentSection == null)
		{
			throw new ArgumentException("The ParentSection parameter cannot be null, use the NetScriptManaget.GetCurrentSection property to get a reference of the current section");
		}
		if (ParentSection._innerSections.ContainsKey(SectionID))
		{
			throw new ArgumentException("The ParentSection already contains a Section with SectionID " + SectionID);
		}
		_title = SectionID;
		_dumpName = ParentSection._dumpName;
		_ruleName = ParentSection._ruleName;
		_threadName = ParentSection._threadName;
		_includeInTOC = true;
		switch (Info)
		{
		case SectionType.Rule:
			if (ParentSection._level > 0)
			{
				throw new ArgumentException($"A section of type Rule cannot be added at Level {ParentSection._level}, you can only add Rule Sections at the default Section level (0)");
			}
			_ruleName = SectionID;
			_collapsible = true;
			break;
		case SectionType.Dump:
			if (ParentSection._level != 1)
			{
				throw new ArgumentException($"A section of type Dump cannot be added at Level {ParentSection._level}, you can only add Dump Sections at the Rule Section level (1)");
			}
			_dumpName = SectionID;
			_collapsible = true;
			break;
		case SectionType.ThreadSummary:
			if (ParentSection._level != 2)
			{
				throw new ArgumentException($"A section of type ThreadSummary cannot be added at Level {ParentSection._level}, you can only add ThreadSummary Sections at the Dump Section level (2)");
			}
			_collapsible = true;
			_title = "Threads Summary";
			break;
		case SectionType.Thread:
			if (ParentSection._level != 3)
			{
				throw new ArgumentException($"A section of type Thread cannot be added at Level {ParentSection._level}, you can only add Thread Sections at the Thread Summary section level (3)");
			}
			_threadName = SectionID;
			break;
		default:
			throw new ArgumentException(string.Format("The report can only contain a section of type default that is automatically created as the root section.", ParentSection._level));
		case SectionType.Custom:
			break;
		}
		if (string.IsNullOrEmpty(SectionID))
		{
			throw new ArgumentException("The SectionID parameter cannot be null or an empty string");
		}
		_type = Info;
		_parentSection = ParentSection;
		_sectionID = SectionID;
		_parentSection.InnerSections.Add(SectionID, this);
		_zOrder = _parentSection.InnerSections.Count;
		_level = ParentSection._level + 1;
		SetInternalId(ParentSection, SectionID, Info);
	}

	private void SetInternalId(ReportSection ParentSection, string SectionID, SectionType Info)
	{
		if (_level >= 3)
		{
			switch (Info)
			{
			case SectionType.ThreadSummary:
				break;
			case SectionType.Thread:
				_InternalID = ParentSection.Parent._InternalID + SectionID;
				return;
			default:
				_InternalID = SectionID + ParentSection._UID;
				_UID = ParentSection._UID;
				return;
			}
		}
		_UID = ParentSection._UID + GenerateUID(_level, _zOrder);
		_InternalID = _UID;
	}

	/// <summary>
	/// Adds a new Child ReportSection in the current ReportSection, If there is a ReportSection with the same SectionID
	/// this method will return a reference to the ReportSection with the same SectionID instead of adding a new ReportSection
	/// </summary>
	/// <param name="SectionID">Id for the new ReportSection</param>
	/// <param name="Info">Type for the new ReportSection, by default it is set to Custom</param>
	/// <returns>The created ReportSection</returns>
	public ReportSection AddChildSection(string SectionID, SectionType Info = SectionType.Custom)
	{
		if (InnerSections.ContainsKey(SectionID))
		{
			return InnerSections[SectionID];
		}
		return new ReportSection(this, SectionID, Info);
	}

	/// <summary>
	/// Adds a new Custom Child ReportSection that could be created by a derived class from ReportSection
	/// </summary>
	/// <param name="section"></param>
	/// <returns>true if it successfully added the section, false otherwise</returns>
	public bool AddChildSection(ReportSection section)
	{
		if (section == null)
		{
			return false;
		}
		section._parentSection = this;
		section._dumpName = _dumpName;
		section._ruleName = _ruleName;
		section._threadName = _threadName;
		section._level = _level + 1;
		section.SetInternalId(this, section.SectionID, section.SectionType);
		InnerSections.Add(section.SectionID, section);
		if (section._zOrder < 0)
		{
			section._zOrder = InnerSections.Count;
		}
		return true;
	}

	/// <summary>
	/// This method returns a ReportSection from the InnerSections collection based on his SectionID, if the SectionID is not found, a new ReportSection
	/// is created, added to the collection, and then returned.
	/// </summary>
	/// <param name="SectionID">SectionID of the ReportSection to be returned</param>
	/// <param name="Info"></param>
	/// <returns></returns>
	public ReportSection GetInnerSection(string SectionID, SectionType Info)
	{
		if (!InnerSections.ContainsKey(SectionID))
		{
			return AddChildSection(SectionID, Info);
		}
		return InnerSections[SectionID];
	}

	/// <summary>
	/// This method is used to report detailed information to the user about the analysis.   
	/// </summary>
	/// <param name="value">Content to write on the report</param>
	public void Write(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			if (_content == null)
			{
				_content = new MemoryStream();
			}
			byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(value);
			_content.Write(bytes, 0, bytes.Length);
		}
	}

	/// <summary>
	/// Method to write data on the report after formatting via String.Format.
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void Write(string format, params object[] args)
	{
		Write(string.Format(format, args));
	}

	/// <summary>
	/// Method to write data on the report
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="bold">Boolean value indicating if the text should be formated as bold</param>
	/// <param name="color">String indicating the color to apply on the text</param>
	/// <param name="size">Integer indicating the size of the text</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void Write(string format, bool bold, string color, int size = 0, params object[] args)
	{
		Write(string.Format(format, args), bold, color, size);
	}

	/// <summary>
	/// Method to write data on the report
	/// </summary>
	/// <param name="output">String value to add on the report</param>
	/// <param name="bold">Boolean value indicating if the text should be formated as bold</param>
	/// <param name="color">String indicating the color to apply on the text</param>
	/// <param name="size">Integer indicating the size of the text</param>
	public void Write(string output, bool bold, string color, int size = 0)
	{
		bool flag = !string.IsNullOrWhiteSpace(color);
		bool flag2 = size > 0;
		string text = "";
		if (flag || flag2)
		{
			text = string.Format("<font {0}{1}{2} {3}{4}{5}", flag ? " color='" : "", flag ? color : "", flag ? "'" : "", flag2 ? " size=" : "", flag2 ? size.ToString() : "", ">");
		}
		Write(string.Format("{0}{1}{2}{3}{4}", bold ? "<b>" : "", text, output, flag ? "</font>" : "", bold ? "</b>" : ""));
	}

	/// <summary>
	/// Method to write an Empty line on the Report
	/// </summary>
	/// <overloads>This method has two more overloads</overloads>
	public void WriteLine()
	{
		WriteLine("");
	}

	/// <summary>
	/// Method to write string with a libe break on the report
	/// </summary>
	/// <param name="output">String value to write on the report</param>
	/// <param name="bold">Boolean to indicate if the string should be formated as Bold</param>
	/// <param name="color">String value to apply a color to the text</param>
	/// <param name="size">Integer value to indicate the size for the text</param>
	public void WriteLine(string output, bool bold, string color = null, int size = 0)
	{
		Write(string.Format("{0}{1}", output, "<BR>"), bold, color, size);
	}

	/// <summary>
	/// Method to write string with a libe break on the report
	/// </summary>
	/// <param name="output">String value to write on the report</param>
	/// <param name="bold">Boolean to indicate if the string should be formated as Bold</param>
	/// <param name="color">String value to apply a color to the text</param>
	/// <param name="size">Integer value to indicate the size for the text</param>
	internal void WriteLine(string format, bool bold, string color, int size, params object[] args)
	{
		WriteLine(string.Format(format, args), bold, color, size);
	}

	/// <summary>
	/// Method to write string with a libe break on the report
	/// </summary>
	/// <param name="output">String value to write on the report</param>
	public void WriteLine(string output)
	{
		Write(string.Format("{0}{1}", output, "<BR>"));
	}

	/// <summary>
	/// Method to write string with a line break on the report after formatting via String.Format.
	/// </summary>
	/// <param name="format">A composite format string.  See String.Format documentation for details.</param>
	/// <param name="args">An object array that contains zero or more objects to format.  See String.Format documentation for details.</param>
	public void WriteLine(string format, params object[] args)
	{
		WriteLine(string.Format(format, args));
	}

	/// <summary>
	/// Method to write a Name-Value pair on the report file using some basic formating
	/// </summary>
	/// <param name="name">String representing the Name of the pair</param>
	/// <param name="value">String representing the Value of the pair</param>
	/// <param name="bold">Optional Boolean value indicating if the text should be formated as bold, the default is true</param>
	/// <param name="color">Optional string indicating the color for the text, default value is null</param>
	/// <param name="size">Optional text size</param>
	public void WriteNameValuePair(string name, string value, bool bold = true, string color = null, int size = 0)
	{
		Write(string.Format("{0}:{1}{1}", name, "&nbsp;"), bold, color, size);
		WriteLine(value);
	}

	/// <summary>
	/// Dispose Method that will clean up the MemoryStream used for the Content
	/// </summary>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose Method that will clean up unmanaged resources used.
	/// </summary>
	/// <param name="disposing"></param>
	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed || !disposing)
		{
			return;
		}
		if (_content != null)
		{
			_content.Dispose();
		}
		if (_innerSections != null)
		{
			foreach (KeyValuePair<string, ReportSection> innerSection in _innerSections)
			{
				if (!innerSection.Value._isDisposed)
				{
					innerSection.Value.Dispose();
				}
			}
			_innerSections.Clear();
		}
		_isDisposed = true;
	}

	internal string GenerateTOC(int levels = 3)
	{
		if (levels < 1)
		{
			return "";
		}
		if (_innerSections == null)
		{
			return "";
		}
		if (_innerSections.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (_showTOCofChildSections)
		{
			stringBuilder.Append("<div class=\"group mt20\">");
		}
		stringBuilder.Append("<div class=\"groupTitle\" style=\"padding-top: 3px;\">Table of Contents</div>");
		if (!_showTOCofChildSections)
		{
			stringBuilder.Append("<div style=\"overflow-y:auto; height:350px; margin-top:10px\">");
		}
		GetLevelData(_innerSections, levels, stringBuilder, IsTopLevel: true);
		stringBuilder.Append("</div>");
		return stringBuilder.ToString();
	}

	private void GetLevelData(ReportSections sections, int depth, StringBuilder sb, bool IsTopLevel = false)
	{
		if (depth < 1 || IsEmpty())
		{
			return;
		}
		string arg = (IsTopLevel ? "tableContentLevel1" : "tableContentLevel2");
		foreach (KeyValuePair<string, ReportSection> section in sections)
		{
			if (!section.Value.IncludeInTOC || string.IsNullOrEmpty(section.Value.Title) || section.Value.IsEmpty())
			{
				continue;
			}
			string text = section.Value._level switch
			{
				0 => string.Empty, 
				1 => "parent", 
				_ => section.Value._type.ToString().ToLower(), 
			};
			string arg2 = "";
			string text2 = (string.IsNullOrEmpty(section.Value._ruleName) ? "" : $" data-debug-rule=\"{section.Value._ruleName}\"");
			string text3 = (string.IsNullOrEmpty(section.Value._dumpName) ? "" : $" data-debug-dump=\"{section.Value._dumpName}\"");
			string text4 = (string.IsNullOrEmpty(section.Value._threadName) ? "" : $" data-debug-thread=\"{section.Value._threadName}\"");
			text = (string.IsNullOrEmpty(text) ? "" : $" data-debug-details=\"{text}\"");
			string text5 = $"{text}{text2}{text3}{text4}";
			if (section.Value._innerSections != null && section.Value._innerSections.Count > 0)
			{
				if (section.Value._level < 3)
				{
					sb.AppendFormat("<div class=\"toc_line\"{1}><div class=\"tocExpanded\" id=\"toc{0}\"></div>", section.Value._InternalID, text5);
				}
				else
				{
					sb.AppendFormat("<div class=\"toc_line\"{1}><div class=\"tocExpanded tocCollapsed\" id=\"toc{0}\"></div>", section.Value._InternalID, text5);
					arg2 = "style=\"display: none;\"";
				}
			}
			else
			{
				sb.AppendFormat("<div class=\"toc_line\" {0}><div class=\"toc_empty\"></div>", text5);
			}
			sb.Append(string.Format("<div class=\"{2} normalText ml20\"><a href=\"#{0}\">{1}</a></div>", section.Value.GetUID, section.Value.Title, arg));
			if (section.Value._innerSections != null && section.Value._innerSections.Count > 0)
			{
				sb.AppendFormat("<div class=\"normalText ml15\" id=\"{0}tocGroup\" {1}>", section.Value._InternalID, arg2);
				GetLevelData(section.Value._innerSections, depth - 1, sb);
				sb.Append("</div>");
			}
			sb.Append("</div>");
		}
	}

	/// <summary>
	/// This function returns a memorystream with the rendered content of the ReportSection and inner ReportSections.
	/// You may override this function if you want to implement your custom formated
	/// </summary>
	/// <returns>HTML formater report sections and inner sections</returns>
	protected internal virtual MemoryStream RenderHTML()
	{
		MemoryStream memoryStream = new MemoryStream();
		if (IsEmpty())
		{
			return memoryStream;
		}
		int num = 1;
		string text;
		string text2;
		switch (_level)
		{
		case 0:
			AppendToStream(memoryStream, "<div id=\"analysisDetailsGroup\">");
			text = string.Empty;
			text2 = string.Empty;
			break;
		case 1:
			text = "parent";
			text2 = " class=\"mt20\"";
			break;
		default:
			text = _type.ToString().ToLower();
			num = ((_level <= 4) ? (_level - 1) : 4);
			text2 = " class=\"mt20 " + text + "\"";
			break;
		}
		if (_level > 0)
		{
			string text3 = (string.IsNullOrEmpty(_ruleName) ? "" : $" data-debug-rule=\"{_ruleName}\"");
			string text4 = (string.IsNullOrEmpty(_dumpName) ? "" : $" data-debug-dump=\"{_dumpName}\"");
			string text5 = (string.IsNullOrEmpty(_threadName) ? "" : $" data-debug-thread=\"{_threadName}\"");
			AppendToStream(memoryStream, string.Format("<div{4} data-debug-details=\"{0}\"{1}{2}{3} id=\"{5}\">", text, text3, text4, text5, text2, _InternalID));
		}
		if (_collapsible && (_content != null || _innerSections != null))
		{
			string arg = (_collapsed ? "expandCollapseButton-expanded21x21 expandCollapseButton-collapsed21x21" : "expandCollapseButton-expanded21x21");
			AppendToStream(memoryStream, string.Format("<div class=\"{1}\" id=\"btn{0}\"></div>", _InternalID, arg));
		}
		if (!string.IsNullOrEmpty(_title))
		{
			AppendToStream(memoryStream, string.Format("<div class=\"groupTitle mt20{3}\"><H{2}>{0}</H{2}></div>", _title, _InternalID, num, _collapsible ? " ml35" : string.Empty));
		}
		if (_showTOCofChildSections)
		{
			AppendToStream(memoryStream, GenerateTOC());
		}
		if (_content != null || _innerSections != null)
		{
			string arg2 = (_collapsed ? "style=\"display: none;\"" : "");
			AppendToStream(memoryStream, $"<div class=\"normalText group\" id=\"{_InternalID}group\" {arg2}>");
			if (_content != null)
			{
				AppendToStream(memoryStream, "<div class=\"mt10 ml15 normalText\">");
				_content.WriteTo(memoryStream);
				AppendToStream(memoryStream, "</div>");
			}
			if (_innerSections != null)
			{
				foreach (KeyValuePair<string, ReportSection> item in _innerSections.OrderBy((KeyValuePair<string, ReportSection> i) => i.Value._zOrder))
				{
					using MemoryStream memoryStream2 = item.Value.RenderHTML();
					memoryStream2.WriteTo(memoryStream);
				}
			}
			AppendToStream(memoryStream, "</div>");
		}
		AppendToStream(memoryStream, "</div>");
		if (_level == 0)
		{
			AppendToStream(memoryStream, "\r\n<!-- End Analysis Summary Section -->\r\n<!-- 5Begin Analysis Rule Summary -->");
		}
		return memoryStream;
	}

	private bool IsEmpty()
	{
		if (_content != null && _content.Length > 0)
		{
			return false;
		}
		foreach (ReportSection value in InnerSections.Values)
		{
			if (!value.IsEmpty())
			{
				return false;
			}
		}
		return true;
	}

	internal static void AppendToStream(MemoryStream _response, string value)
	{
		byte[] encodedBytes = MIMEHelperFunctions.GetEncodedBytes(value);
		_response.Write(encodedBytes, 0, encodedBytes.Length);
	}

	private static string GenerateUID(int level, int zOrder)
	{
		if (level < 4)
		{
			return Convert.ToChar(65 + level).ToString() + zOrder;
		}
		throw new ArgumentOutOfRangeException("Level is greater than 3, SectionID should be used for the UID at this level");
	}
}
