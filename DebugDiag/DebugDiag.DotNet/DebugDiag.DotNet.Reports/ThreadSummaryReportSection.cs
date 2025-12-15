using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DebugDiag.DotNet.Reports;

internal sealed class ThreadSummaryReportSection : ReportSection
{
	private Dictionary<Guid, ReportSections> SectionGroups = new Dictionary<Guid, ReportSections>();

	private NetScriptManager ManagerInstance;

	private bool _removeDuplicated = true;

	public ThreadSummaryReportSection(string SectionID, NetScriptManager manager)
		: base(SectionID, IncludeInTOC: true)
	{
		_type = SectionType.ThreadSummary;
		ManagerInstance = manager;
		_removeDuplicated = manager.GroupIdenticalStacks;
	}

	protected internal override MemoryStream RenderHTML()
	{
		MemoryStream memoryStream = new MemoryStream();
		string text = _type.ToString().ToLower();
		string text2 = (string.IsNullOrEmpty(_ruleName) ? "" : $" data-debug-rule=\"{_ruleName}\"");
		string text3 = (string.IsNullOrEmpty(_dumpName) ? "" : $" data-debug-dump=\"{_dumpName}\"");
		string text4 = " class=\"mt20\"";
		int num = 1;
		num = ((_level <= 4) ? (_level - 1) : 4);
		ReportSection.AppendToStream(memoryStream, string.Format("<div{3} data-debug-details=\"{0}\"{1}{2} >", text, text2, text3, text4));
		if (_content != null || base.InnerSections.Count > 0)
		{
			string arg = (base.Collapsed ? "expandCollapseButton-expanded21x21 expandCollapseButton-collapsed21x21" : "expandCollapseButton-expanded21x21");
			ReportSection.AppendToStream(memoryStream, string.Format("<div class=\"{1}\" id=\"btn{0}\"></div>", _InternalID, arg));
		}
		if (!string.IsNullOrEmpty(base.Title))
		{
			ReportSection.AppendToStream(memoryStream, string.Format("<div class=\"groupTitle mt20{3}\"><a name='{1}'><H{2}>{0}</H{2}></a></div>", base.Title, _InternalID, num, base.Collapsible ? " ml35" : string.Empty));
		}
		if (_content != null || base.InnerSections.Count > 0)
		{
			string arg2 = (base.Collapsed ? "style=\"display: none;\"" : "");
			ReportSection.AppendToStream(memoryStream, $"<div class=\"normalText group\" id=\"{_InternalID}group\" {arg2}>");
			if (_content != null)
			{
				ReportSection.AppendToStream(memoryStream, "<div class=\"mt20 normalText\">");
				_content.WriteTo(memoryStream);
				ReportSection.AppendToStream(memoryStream, "</div>");
			}
			if (base.InnerSections.Count > 0)
			{
				if (_removeDuplicated)
				{
					LoadUniqueStackSections();
					int num2 = 0;
					int totalThreadCount = SectionGroups.Sum((KeyValuePair<Guid, ReportSections> i) => i.Value.Count);
					string getUID = base.Parent.GetUID;
					foreach (KeyValuePair<Guid, ReportSections> item in SectionGroups.OrderByDescending((KeyValuePair<Guid, ReportSections> i) => i.Value.Count))
					{
						string text5 = string.Empty;
						if (item.Key != Guid.Empty)
						{
							text5 = getUID + "_Tabs" + num2;
							num2++;
							ReportSection.AppendToStream(memoryStream, $"<div id=\"{text5}\">");
							ManagerInstance.RegisterJSDocReadyHandler(string.Format("$(\"#{0}\").tabs();/*$(\"#{0}\").tabs('paging');*/", text5));
							ReportSection.AppendToStream(memoryStream, GetSectionTabs(item.Value, totalThreadCount));
						}
						if (item.Value.Count == 1)
						{
							using (MemoryStream memoryStream2 = ((ThreadReportSection)item.Value.First().Value).RenderHTML())
							{
								memoryStream2.WriteTo(memoryStream);
							}
							continue;
						}
						MemoryStream memoryStream3 = null;
						bool flag = false;
						foreach (KeyValuePair<string, ReportSection> item2 in item.Value)
						{
							if (item.Key != Guid.Empty)
							{
								ManagerInstance.ReplaceAnchorReference("#" + item2.Value.GetUID, "#" + text5);
							}
							using (MemoryStream memoryStream4 = ((ThreadReportSection)item2.Value).RenderThreadHeaderHTML())
							{
								memoryStream4.WriteTo(memoryStream);
							}
							if (!flag)
							{
								memoryStream3 = ((ThreadReportSection)item2.Value).RenderThreadStackStringOnlyHTML();
								flag = true;
							}
						}
						if (item.Key != Guid.Empty)
						{
							ReportSection.AppendToStream(memoryStream, "</div>");
						}
						if (flag)
						{
							ReportSection.AppendToStream(memoryStream, "<div class=\"ml35 mb20\">");
							memoryStream3?.WriteTo(memoryStream);
							ReportSection.AppendToStream(memoryStream, "</div>");
						}
						memoryStream3?.Close();
					}
				}
				else
				{
					foreach (ThreadReportSection value in base.InnerSections.Values)
					{
						using MemoryStream memoryStream5 = value.RenderHTML();
						memoryStream5.WriteTo(memoryStream);
					}
				}
			}
			ReportSection.AppendToStream(memoryStream, "</div>");
		}
		ReportSection.AppendToStream(memoryStream, "</div>");
		if (_level == 0)
		{
			ReportSection.AppendToStream(memoryStream, "\r\n<!-- End Analysis Summary Section -->\r\n<!-- Begin Analysis Rule Summary -->");
		}
		return memoryStream;
	}

	private string GetSectionTabs(ReportSections reportSections, int totalThreadCount)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (reportSections.Values.Count > 1)
		{
			string arg = "Thread ";
			stringBuilder.Append("<ul>");
			foreach (KeyValuePair<string, ReportSection> reportSection in reportSections)
			{
				stringBuilder.AppendFormat("<li><a href=\"#{0}\">{1}{2}</a></li>", reportSection.Value.GetUID, arg, reportSection.Value.Title.Split(' ')[1]);
				arg = "";
			}
			stringBuilder.Append("</ul>");
			stringBuilder.AppendFormat("<br><b>{0} Threads</b> ({1}% of all threads) have this same call stack.<br><i>Note: Grouping of identical threads can be disabled in the 'Preferences' tab of the Analysis Options</i>", reportSections.Values.Count, 100 * reportSections.Values.Count / totalThreadCount);
		}
		return stringBuilder.ToString();
	}

	private void LoadUniqueStackSections()
	{
		Guid key = Guid.Empty;
		foreach (KeyValuePair<string, ReportSection> innerSection in base.InnerSections)
		{
			if (innerSection.Value is ThreadReportSection)
			{
				key = ((ThreadReportSection)innerSection.Value).ThreadHash;
			}
			if (SectionGroups.ContainsKey(key))
			{
				SectionGroups[key].Add(innerSection.Key, innerSection.Value);
				continue;
			}
			ReportSections reportSections = new ReportSections();
			reportSections.Add(innerSection.Key, innerSection.Value);
			SectionGroups.Add(((ThreadReportSection)innerSection.Value).ThreadHash, reportSections);
		}
	}
}
