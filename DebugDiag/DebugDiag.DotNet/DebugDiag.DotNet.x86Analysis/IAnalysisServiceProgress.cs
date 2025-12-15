using System.ServiceModel;

namespace DebugDiag.DotNet.x86Analysis;

internal interface IAnalysisServiceProgress
{
	[OperationContract]
	void SetOverallRange(int Low, int High);

	[OperationContract]
	void SetCurrentRange(int Low, int High);

	[OperationContract]
	void SetOverallPosition(int value);

	[OperationContract]
	void SetCurrentPosition(int value);

	[OperationContract]
	void SetOverallStatus(string value);

	[OperationContract]
	void SetCurrentStatus(string value);

	[OperationContract]
	void SetDebuggerStatus(string value);
}
