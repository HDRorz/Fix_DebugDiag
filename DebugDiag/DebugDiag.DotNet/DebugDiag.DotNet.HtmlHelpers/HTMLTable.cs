using System;
using System.Collections.Generic;
using System.Text;

namespace DebugDiag.DotNet.HtmlHelpers;

public class HTMLTable
{
	public bool InferNameValueStyle = true;

	public bool DrawTopLine = true;

	public bool ToggleRowBackgrounds = true;

	public static bool FirstWrite = true;

	private List<string> columns;

	private List<List<Tuple<string, bool>>> rows;

	private Dictionary<string, string> columnStyles = new Dictionary<string, string>();

	private Dictionary<List<Tuple<string, bool>>, string> rawRows = new Dictionary<List<Tuple<string, bool>>, string>();

	private string id = string.Empty;

	public string ClassName = "tableClass";

	public HTMLTable()
	{
		columns = new List<string>();
		rows = new List<List<Tuple<string, bool>>>();
	}

	public HTMLTable(string id)
		: this()
	{
		this.id = id;
	}

	public HTMLTable(string id, string className)
		: this(id)
	{
		InferNameValueStyle = false;
		ClassName = className;
	}

	public void AddColumns(params object[] cols)
	{
		foreach (object obj in cols)
		{
			columns.Add(obj.ToString());
		}
	}

	public List<Tuple<string, bool>> AddRow(params object[] cols)
	{
		return AddRowInternal(noWrap: false, cols);
	}

	public List<Tuple<string, bool>> AddRowNoWrap(params object[] cols)
	{
		return AddRowInternal(noWrap: true, cols);
	}

	private List<Tuple<string, bool>> AddRowInternal(bool noWrap, params object[] cols)
	{
		List<Tuple<string, bool>> list = new List<Tuple<string, bool>>();
		foreach (object obj in cols)
		{
			list.Add(new Tuple<string, bool>(ToString(obj), noWrap));
		}
		rows.Add(list);
		return rows[rows.Count - 1];
	}

	private string ToString(object c)
	{
		if (c != null)
		{
			return c.ToString();
		}
		return string.Empty;
	}

	public void AddColumnStyle(string column, string style)
	{
		columnStyles.Add(column, style);
	}

	public void AddRawRow(List<string> row, string html)
	{
		if (row == null || row.Count == 0)
		{
			return;
		}
		List<Tuple<string, bool>> list = new List<Tuple<string, bool>>(row.Count);
		foreach (string item in row)
		{
			list.Add(new Tuple<string, bool>(item, item2: false));
		}
		rawRows.Add(list, html);
	}

	public override string ToString()
	{
		List<List<Tuple<string, bool>>> list = rows;
		if (list != null && list.Count == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (InferNameValueStyle && columns.Count == 0 && rows.Count > 0 && rows[0].Count == 2)
		{
			ClassName += " nameValueTable";
		}
		string text = (ToggleRowBackgrounds ? "data-is-row='true' " : "");
		stringBuilder.AppendLine(string.Format("<table data-debug-type='table' class='{1}' id='{2}'>", text, ClassName, id));
		stringBuilder.AppendLine("<tr>");
		if (columns.Count == 0 && DrawTopLine)
		{
			stringBuilder.Append($"<th colspan='{rows[0].Count}'/>");
		}
		else
		{
			foreach (string column in columns)
			{
				if (columnStyles.ContainsKey(column))
				{
					stringBuilder.Append($"<th style='{columnStyles[column]}' {text}>{column}</th>");
				}
				else
				{
					stringBuilder.Append($"<th {text}>{column}</th>");
				}
			}
		}
		stringBuilder.AppendLine("</tr>");
		foreach (List<Tuple<string, bool>> row in rows)
		{
			if (rawRows.ContainsKey(row))
			{
				stringBuilder.AppendLine(rawRows[row]);
				continue;
			}
			stringBuilder.AppendLine($"<tr {text}>");
			foreach (Tuple<string, bool> item in row)
			{
				stringBuilder.Append(string.Format("<td{0} style='padding-left:5px; vertical-align:top'>{1}</td>", item.Item2 ? " nowrap" : "", item.Item1));
			}
			stringBuilder.AppendLine("</tr>");
		}
		stringBuilder.AppendLine("</table>");
		return stringBuilder.ToString();
	}

	public override int GetHashCode()
	{
		return id.GetHashCode();
	}
}
