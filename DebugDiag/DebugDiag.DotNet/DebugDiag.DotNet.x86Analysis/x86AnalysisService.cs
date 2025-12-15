using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows.Threading;
using DebugDiag.DotNet.AnalysisRules;

namespace DebugDiag.DotNet.x86Analysis;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
internal class x86AnalysisService : NetProgress, IAnalysisService
{
	private IAnalysisServiceProgress _progressCallback;

	private string _reportFileName;

	private string _symbolPath;

	private string _imagePath;

	private bool _timedOut;

	private List<string> _dumpFiles;

	public ManualResetEvent _analysisStartedEvent;

	public ManualResetEvent _analysisCompletedEvent;

	private List<AnalysisRuleInfo> _analysisRulesInfos;

	private bool _twoTabs;

	private Exception _analysisException;

	private string _reportFileFullPath;

	private NetResults _results;

	private bool _includeHttpHeadersInClientConns;

	private bool _setContextOnCrashDumps;

	private bool _doHangAnalysisOnCrashDumps;

	private bool _groupIdenticalStacks;

	private bool _includeSourceAndLineInformationInAnalysisReports;

	private bool _includeInstructionPointerInAnalysisReports;

	private Thread _analyzerThread;

	private List<object> _facts;

	private Thread AnalyzerThread
	{
		get
		{
			if (_analyzerThread == null)
			{
				ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
				_analyzerThread = new Thread(AnalyzerThreadProc, 10485760);
				_analyzerThread.IsBackground = true;
				_analyzerThread.Name = "AnalyzerThread";
				_analyzerThread.SetApartmentState(ApartmentState.STA);
				_analyzerThread.Start(manualResetEvent);
				manualResetEvent.WaitOne();
			}
			return _analyzerThread;
		}
	}

	public override int OverallPosition
	{
		set
		{
			if (_progressCallback != null)
			{
				_progressCallback.SetOverallPosition(value);
			}
		}
	}

	public override int CurrentPosition
	{
		set
		{
			if (_progressCallback != null)
			{
				_progressCallback.SetOverallPosition(value);
			}
		}
	}

	public override string OverallStatus
	{
		set
		{
			if (_progressCallback != null)
			{
				_progressCallback.SetOverallStatus(value);
			}
		}
	}

	public override string CurrentStatus
	{
		set
		{
			if (_progressCallback != null)
			{
				_progressCallback.SetCurrentStatus(value);
			}
		}
	}

	public override string DebuggerStatus
	{
		set
		{
			if (_progressCallback != null)
			{
				_progressCallback.SetDebuggerStatus(value);
			}
		}
	}

	public x86AnalysisService(ManualResetEvent analysisStartedEvent, ManualResetEvent analysisCompletedEvent)
	{
		_analysisCompletedEvent = analysisCompletedEvent;
		_analysisStartedEvent = analysisStartedEvent;
	}

	public static void ConfigureAnalysisServiceEndpoint(ContractDescription analysisServiceContractDescription)
	{
		OperationDescription operationDescription = analysisServiceContractDescription.Operations.Find("RunAnalysisRules");
		operationDescription.Behaviors.Remove<DataContractSerializerOperationBehavior>();
		operationDescription.Behaviors.Add(new DataContractSerializerOperationBehavior(operationDescription)
		{
			DataContractResolver = new SharedTypeResolver(),
			MaxItemsInObjectGraph = int.MaxValue
		});
	}

	internal void Shutdown()
	{
		if (_analyzerThread != null)
		{
			Dispatcher.FromThread(_analyzerThread)?.BeginInvokeShutdown(DispatcherPriority.Normal);
			_analyzerThread = null;
		}
	}

	private void AnalyzerThreadProc(object state)
	{
		ManualResetEvent obj = (ManualResetEvent)state;
		SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
		obj.Set();
		Dispatcher.Run();
	}

	private void StaThreadStart(ManualResetEvent evt, NetAnalyzer analyzer)
	{
		try
		{
			if (evt == null)
			{
				throw new ArgumentNullException("evt");
			}
			if (analyzer == null)
			{
				throw new ArgumentNullException("analyzer");
			}
			analyzer.Initialize(_includeSourceAndLineInformationInAnalysisReports, _setContextOnCrashDumps, _doHangAnalysisOnCrashDumps, _includeHttpHeadersInClientConns, _groupIdenticalStacks, _includeInstructionPointerInAnalysisReports);
			analyzer.AddDumpFiles(_dumpFiles, _symbolPath);
			analyzer.AnalysisRuleInfos.AddRange(_analysisRulesInfos);
			analyzer.RunAnalysisRules(this, _symbolPath, _imagePath, _reportFileFullPath, _twoTabs);
			while (!analyzer.ReportReady & !_timedOut)
			{
				Thread.Sleep(500);
			}
			_reportFileName = analyzer.ReportFileNames[0];
			_results = new NetResults(analyzer.Manager.GetResults(0));
			_facts = analyzer.Manager.GetFacts(0);
		}
		catch (Exception analysisException)
		{
			_analysisException = analysisException;
		}
		finally
		{
			evt.Set();
		}
	}

	private void progress_OnSetCurrentPositionChanged(object sender, SetCurrentPositionEventArgs e)
	{
		throw new NotImplementedException();
	}

	public NetResults RunAnalysisRules(List<AnalysisRuleInfo> analysisRulesInfos, List<string> dumpFiles, string symbolPath, string imagePath, string reportFileFullPath, TimeSpan timeout, bool twoTabs, bool includeSourceAndLineInformationInAnalysisReports, bool setContextOnCrashDumps, bool doHangAnalysisOnCrashDumps, bool includeHttpHeadersInClientConns, bool groupIdenticalStacks, bool includeInstructionPointerInAnalysisReports, out List<object> facts)
	{
		facts = null;
		_analysisStartedEvent.Set();
		_progressCallback = OperationContext.Current.GetCallbackChannel<IAnalysisServiceProgress>();
		_groupIdenticalStacks = groupIdenticalStacks;
		_analysisRulesInfos = analysisRulesInfos;
		_dumpFiles = dumpFiles;
		_symbolPath = symbolPath;
		_imagePath = imagePath;
		_twoTabs = twoTabs;
		_reportFileFullPath = reportFileFullPath;
		_includeSourceAndLineInformationInAnalysisReports = includeSourceAndLineInformationInAnalysisReports;
		_includeInstructionPointerInAnalysisReports = includeInstructionPointerInAnalysisReports;
		_setContextOnCrashDumps = setContextOnCrashDumps;
		_doHangAnalysisOnCrashDumps = doHangAnalysisOnCrashDumps;
		_includeHttpHeadersInClientConns = includeHttpHeadersInClientConns;
		ManualResetEvent evt = new ManualResetEvent(initialState: false);
		Thread analyzerThread = AnalyzerThread;
		NetAnalyzer analyzer = new NetAnalyzer();
		try
		{
			Dispatcher.FromThread(analyzerThread).BeginInvoke((Action<ManualResetEvent, NetAnalyzer>)delegate
			{
				StaThreadStart(evt, analyzer);
			}, evt, analyzer);
			if (!evt.WaitOne(TimeSpan.FromHours(2.0)))
			{
				if (analyzer != null)
				{
					analyzer.Kill();
				}
				Shutdown();
				analyzerThread.Abort();
				_timedOut = true;
				_analysisCompletedEvent.Set();
				throw new FaultException<AnalysisTimeoutException>(new AnalysisTimeoutException(timeout));
			}
		}
		finally
		{
			if (analyzer != null)
			{
				((IDisposable)analyzer).Dispose();
			}
		}
		Shutdown();
		_analysisCompletedEvent.Set();
		if (_analysisException != null)
		{
			throw _analysisException;
		}
		facts = _facts;
		return _results;
	}

	public override void SetOverallRange(int Low, int High)
	{
		if (_progressCallback != null)
		{
			_progressCallback.SetOverallRange(Low, High);
		}
	}

	public override void SetCurrentRange(int Low, int High)
	{
		if (_progressCallback != null)
		{
			_progressCallback.SetCurrentRange(Low, High);
		}
	}
}
