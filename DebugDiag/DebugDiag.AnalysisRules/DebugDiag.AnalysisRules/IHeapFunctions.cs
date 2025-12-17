using MemoryExtLib;

namespace DebugDiag.AnalysisRules;

public interface IHeapFunctions
{
	void AnalyzeAndReportHeapInfo();

	void ShowHeapInfoNoneDetectedIfNecessary();

	void PrintHeapInfo(INTHeap Heap);

	string GetHeapLink(INTHeap Heap);

	bool IsHeapFunction(double Address);

	CacheFunctions.ScriptModuleClass AnalyzeHeapCorruption(CacheFunctions.ScriptThreadClass ExceptionThread);

	string GetHeapOwnerModule(string HeapName);

	string GetGlobalFlagDescription(int GFValue);

	bool IsPageHeapEnabled(int GFValue);
}
