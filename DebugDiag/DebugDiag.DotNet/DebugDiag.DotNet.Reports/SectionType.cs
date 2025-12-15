namespace DebugDiag.DotNet.Reports;

/// <summary>
/// Types of Sections you can Add to a report, not all types of sections can be added inside a ParentSecion, for example
/// there is only one default section that is the root of all sections and cannot be added inside any other Sections.
/// </summary>
public enum SectionType
{
	/// <summary>
	/// This ReportSection is intended for creating your custom sections inside any ReportSection
	/// </summary>
	Custom,
	/// <summary>
	/// Rule ReportSection can be added into the Default ReportSection, it will be automatically created for every Rule executed on the Analysis
	/// </summary>
	Dump,
	/// <summary>
	/// This is a Custom Section added on the Dump ReportSection, for grouping the Thread data, all Thread ReporSection are created inside this section.
	/// </summary>
	Rule,
	/// <summary>
	/// Dump ReportSection can be added into a Rule ReportSection, it will be automatically created for each IHangDumpRule 
	/// </summary>     
	ThreadSummary,
	/// <summary>
	/// Thread ReportSection can be added on the ThreadSummary ReportSection, it will be automatically created for each thread for IHangThreadRule and IExceptionThreadRule.
	/// </summary>
	Thread,
	/// <summary>
	/// Default ReportSection added at the Root Level, is a field of the NetScriptManager object and there is only one ReportSection of this type in the report
	/// </summary>
	Default
}
