using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DebugDiag.DotNet.Reports;

/// <summary>
/// Class to add JavaScript code dynamically to the report, this class is instantiated by the NetScriptManager and the way to add
/// Javascript is through the methods provided on the NetScriptManager
/// </summary>
internal sealed class ReportJsManager
{
	private NetScriptManager _manager;

	private List<string> javaScripts = new List<string>();

	private List<string> docReadyCode = new List<string>();

	private Dictionary<string, string> AnchorsReplaced = new Dictionary<string, string>();

	internal ReportJsManager(NetScriptManager manager)
	{
		_manager = manager;
	}

	/// <summary>
	/// This function creates the .js file that will be appended on the report mht
	/// </summary>
	/// <returns></returns>
	internal MemoryStream CreateReportFunctions()
	{
		MemoryStream resourceFromAssembly = _manager.GetResourceFromAssembly("ReportFunctions.js");
		resourceFromAssembly.Position = resourceFromAssembly.Length;
		if (AnchorsReplaced.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("var _map = {\r\n");
			int num = AnchorsReplaced.Count;
			foreach (KeyValuePair<string, string> item in AnchorsReplaced)
			{
				num--;
				stringBuilder.AppendFormat("'{0}' : '{1}'{2}", item.Key, item.Value, (num > 0) ? ",\r\n" : "\r\n");
			}
			stringBuilder.AppendLine("}");
			ReportSection.AppendToStream(resourceFromAssembly, "\r\n" + stringBuilder.ToString() + "\r\n");
			docReadyCode.Add("$(\"a\").click(function(){var ob = _map[this.hash];if (ob != null) this.href = ob;});");
		}
		ReportSection.AppendToStream(resourceFromAssembly, "\r\n$(window).load(function()  {\r\n");
		CheckForHandlers(resourceFromAssembly, _manager._details.InnerSections);
		foreach (string item2 in docReadyCode)
		{
			ReportSection.AppendToStream(resourceFromAssembly, item2 + "\r\n");
		}
		ReportSection.AppendToStream(resourceFromAssembly, "\r\n});\r\n");
		foreach (string javaScript in javaScripts)
		{
			ReportSection.AppendToStream(resourceFromAssembly, javaScript + "\r\n");
		}
		return resourceFromAssembly;
	}

	internal void AppendJSFunction(string jsCode)
	{
		javaScripts.Add(jsCode);
	}

	internal void AppendCallOnDocumentReady(string jsCode)
	{
		docReadyCode.Add(jsCode);
	}

	internal void ReplaceAnchorReference(string AnchorHref, string NewHref)
	{
		if (AnchorsReplaced.ContainsKey(AnchorHref))
		{
			AnchorsReplaced[AnchorHref] = NewHref;
		}
		else
		{
			AnchorsReplaced.Add(AnchorHref, NewHref);
		}
	}

	/// <summary>
	/// Navigates the ReportSections to add Handlers for the content referenced on the TOC and collapsible sections
	/// </summary>
	/// <param name="ms">MemoryStream that has the JS code</param>
	/// <param name="sections">ReportSections being navigated</param>
	private void CheckForHandlers(MemoryStream ms, ReportSections sections)
	{
		if (sections.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, ReportSection> section in sections)
		{
			if (section.Value.Collapsible)
			{
				string toggleClass = "expandCollapseButton-collapsed21x21";
				AppendHandlerToStream(ms, $"btn{section.Value.GetUID}", $"{section.Value.GetUID}group", toggleClass);
			}
			if (section.Value.InnerSections.Count > 0)
			{
				AppendHandlerToStream(ms, $"toc{section.Value.GetUID}", $"{section.Value.GetUID}tocGroup", "tocCollapsed");
				CheckForHandlers(ms, section.Value.InnerSections);
			}
		}
		ReportSection.AppendToStream(ms, "\r\n");
	}

	private void AppendHandlerToStream(MemoryStream ms, string ControlId, string DivGroupName, string toggleClass)
	{
		string value = string.Format("$(\"#{0}\").click(function(){{$(\"#{1}\").toggle(\"slow\",function(){{$(\"#{0}\").toggleClass(\"{2}\");}});}});\r\n", ControlId, DivGroupName, toggleClass);
		ReportSection.AppendToStream(ms, value);
	}
}
