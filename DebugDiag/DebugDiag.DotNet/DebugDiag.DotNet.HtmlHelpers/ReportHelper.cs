using System.Collections.Generic;
using System.Text;

namespace DebugDiag.DotNet.HtmlHelpers;

internal static class ReportHelper
{
	internal static string BuildFilter(List<string> values, string controlId, string dataTagName)
	{
		if (controlId != "memoryDump" && controlId != "rule")
		{
			return "";
		}
		string arg = ((controlId == "memoryDump") ? "memory dumps" : "rules");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendFormat("<div id=\"{0}FilterSection\" style=\"display:none;left: 0px; top: 0px; right: 0px; bottom: 0px; overflow: auto; position: fixed; z-index: 500;\" ms.cmpgrp=\"overlay\"> <div style=\"background: rgb(0, 0, 0); left: 0px; top: 0px; right: 0px; bottom: 0px; position: fixed; opacity: 0.85;\"></div><div style=\"background: rgb(255, 255, 255); left: 0px; top: 202.5px; width: 100%; height: 550px; position: absolute;\">   <div class=\"overlay-container\" style=\"width: 920px;\">   <div class=\"overlay-header\">    <div class=\"overlay-close\">     <div class=\"OverlayCloseArea\">      <a class=\"hpImage_Link\" href=\"javascript:void(0);\">       <img width=\"21\" height=\"21\" class=\"hpImage_Img\" alt=\"close\" id=\"close{0}FilterSelection\" src=\"res/closeButton.png\">      </a>     </div>    </div>   </div>   <div class=\"download-wizard\">    <div class=\"multifiles\">     <h2>Choose the {1} you want</h2>      <div class=\"dl-selector\">       <div class=\"multifile-th\">        <div><input class=\"wizard-check\" checked=\"checked\" type=\"checkbox\" value=\"0\" id=\"{0}FilterSelectAll\" data-debug-dump=\"all\">All</div>       </div>       <div class=\"multifile-list\" id=\"{0}FilterTable\" style=\"height: 302.8px;\">        <table data-debug-type=\"table\">         <tbody>", controlId, arg);
		int num = 1;
		foreach (string value in values)
		{
			stringBuilder.AppendFormat("<tr data-is-row=\"true\"><td class=\"co1\" style=\"width: 530px;\"><input type=\"checkbox\" checked=\"checked\" value=\"{0}\" {1}=\"{2}\"><span>{2}</span></td></tr>", num, dataTagName, value);
			num++;
		}
		stringBuilder.AppendFormat("         </tbody>        </table>       </div>      </div>      <div class=\"wizard-footer\">       <a tabindex=\"-1\" class=\"button next\" id=\"apply{0}FilterSelection\" href=\"javascript:void(0);\">Apply</a>      </div>     </div>    </div>  </div> </div></div>", controlId);
		return stringBuilder.ToString();
	}

	internal static string BuildReportSummaryTable(string summaryLabel, List<NetResult> results, string iconFile)
	{
		if (results.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<div class=\"summaryGroup mt20 normalText\" id=\"");
		stringBuilder.AppendFormat("{0}sAnalysisSummarySection\">", summaryLabel.ToLower());
		if (!string.IsNullOrEmpty(iconFile))
		{
			stringBuilder.Append("<div style=\"padding-top: 5px; float: left;\"></div>");
		}
		stringBuilder.AppendFormat("<div class=\"groupTitle summaryGroupLabel {0}\"><img src=\"res/{1}\"/> {0}  </div>", summaryLabel, iconFile);
		stringBuilder.Append($"<div class=\"group mt20\"><table data-debug-type=\"table\" width=\"100%\"><thead><tr class=\"gridheader{summaryLabel}\" data-is-header=\"true\"><td class=\"gridheaderspacing\">Description</td><td class=\"gridheaderspacing finaltd\">Recommendation</td></tr></thead><tbody>");
		foreach (NetResult result in results)
		{
			stringBuilder.Append($"<tr class=\"gridrowspacing\" data-is-row=\"true\" data-debug-dump=\"{result.DumpName}\" data-debug-rule=\"{result.Source}\">");
			stringBuilder.Append($"<td class=\"gridrowspacing\">{result.Description}</td>");
			stringBuilder.Append($"<td class=\"gridrowspacing finaltd\">{result.Recommendation}</td></tr>");
		}
		stringBuilder.Append("</tbody></table></div></div>");
		return stringBuilder.ToString();
	}
}
