using System;
using System.Collections.Generic;
using System.Text;

namespace DebugDiag.AnalysisRules;

public class BarGraph
{
	public string vbCRLF = Environment.NewLine;

	public string[] Colors;

	public int Width;

	public GraphRow[] Rows;

	public BarGraph()
	{
		Width = 400;
		Colors = new string[1] { "steelblue" };
	}

	public void InitFromArray(GraphRow[] graphRowArray)
	{
		Rows = graphRowArray;
	}

	public void InitFromDict(Dictionary<int, GraphRow> graphRowDict)
	{
		if (graphRowDict.Count <= 0)
		{
			return;
		}
		Rows = new GraphRow[graphRowDict.Count];
		int num = 0;
		foreach (GraphRow value in graphRowDict.Values)
		{
			Rows[num] = value;
			num++;
		}
	}

	public void DrawGraph()
	{
		double num = 0.0;
		Globals.HelperFunctions.UBound(Colors);
		IHelperFunctions helperFunctions = Globals.HelperFunctions;
		object[] rows = Rows;
		int[] array = new int[helperFunctions.UBound(rows) + 1];
		int num2 = 0;
		while (true)
		{
			int num3 = num2;
			IHelperFunctions helperFunctions2 = Globals.HelperFunctions;
			rows = Rows;
			if (num3 > helperFunctions2.UBound(rows))
			{
				break;
			}
			array[num2] = CountBRs(Rows[num2].Caption);
			num2++;
		}
		Globals.Manager.Write("<table border=0><tr><td><table border=0>" + vbCRLF);
		num2 = 0;
		while (true)
		{
			int num4 = num2;
			IHelperFunctions helperFunctions3 = Globals.HelperFunctions;
			rows = Rows;
			if (num4 > helperFunctions3.UBound(rows))
			{
				break;
			}
			if (Rows[num2].Value > num)
			{
				num = Rows[num2].Value;
			}
			Globals.Manager.Write("<tr><td nowrap>" + vbCRLF);
			if (!string.IsNullOrEmpty(Rows[num2].Link))
			{
				Globals.Manager.Write("<a href='" + Rows[num2].Link + "'");
				if (Rows[num2].OnClick != "")
				{
					Globals.Manager.Write(" onclick='" + Rows[num2].OnClick + "'");
				}
				Globals.Manager.Write(">" + Rows[num2].Caption + "</a> " + vbCRLF);
			}
			else
			{
				Globals.Manager.Write(Rows[num2].Caption);
			}
			Globals.Manager.Write("</td></tr>" + vbCRLF);
			num2++;
		}
		Globals.Manager.Write("</table></td>" + vbCRLF);
		Globals.Manager.Write("<td><table border=0 >" + vbCRLF);
		num2 = 0;
		while (true)
		{
			int num5 = num2;
			IHelperFunctions helperFunctions4 = Globals.HelperFunctions;
			rows = Rows;
			if (num5 > helperFunctions4.UBound(rows))
			{
				break;
			}
			Globals.Manager.Write("<tr><td nowrap>" + vbCRLF);
			Globals.Manager.Write(Rows[num2].Caption2 + AddBRs(array[num2] + 1));
			Globals.Manager.Write("</td></tr>" + vbCRLF);
			num2++;
		}
		Globals.Manager.Write("</table></td>");
		Globals.Manager.Write("<td><table width=" + Width + " border=0 >" + vbCRLF);
		num2 = 0;
		while (true)
		{
			int num6 = num2;
			IHelperFunctions helperFunctions5 = Globals.HelperFunctions;
			rows = Rows;
			if (num6 > helperFunctions5.UBound(rows))
			{
				break;
			}
			if (num != 0.0)
			{
				Rows[num2].Value = Convert.ToInt32(Rows[num2].Value / num * 100.0);
			}
			Globals.Manager.Write("<tr>");
			if (Rows[num2].Value > 0.0)
			{
				Globals.Manager.Write("<td><div class=\"bargraph\" style='width:" + Rows[num2].Value + "%;'>&nbsp;" + AddBRs(array[num2] + 1) + "</div></td>" + vbCRLF);
			}
			else
			{
				Globals.Manager.Write("<td>&nbsp;</td>" + vbCRLF);
			}
			Globals.Manager.Write("</tr>" + vbCRLF);
			num2++;
		}
		Globals.Manager.Write("</table></td>" + vbCRLF);
		Globals.Manager.Write("</tr></table>" + vbCRLF);
	}

	public void SetRowCount(int RowCount)
	{
		Rows = new GraphRow[RowCount];
		for (int i = 0; i < RowCount; i++)
		{
			Rows[i] = new GraphRow();
		}
	}

	public int CountBRs(string caption)
	{
		int num = 0;
		caption = Globals.HelperFunctions.UCase(caption);
		for (int num2 = Globals.HelperFunctions.InStr(caption, "<BR"); num2 > 0; num2 = Globals.HelperFunctions.InStr(num2 + 3, caption, "<BR"))
		{
			num++;
		}
		return num;
	}

	public string AddBRs(int count)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 1; i <= count; i++)
		{
			stringBuilder.Append("<BR>");
		}
		return stringBuilder.ToString();
	}
}
