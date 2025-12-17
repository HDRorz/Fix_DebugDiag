using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DebugDiag.DotNet;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.AnalysisRules.inc;

public class AnalyzeManagedWCF
{
	public class WcfClientRequest
	{
		private string connectionPool;

		public string Endpoint { get; set; }

		public string MethodCalled { get; set; }

		public string ServiceType { get; set; }

		public string State { get; set; }

		public string ConnectionPool { get; set; }

		public string ThreadID { get; set; }

		public bool IsQueued { get; internal set; }

		public TimeSpan CallDuration { get; internal set; }

		public TimeSpan RequestTimeout { get; internal set; }

		public string DebugInfo { get; internal set; }

		public bool IsRunningOnThread { get; internal set; }
	}

	private NetDbgObj debugger;

	private List<OperationContextPerThread> opertationsContextList = new List<OperationContextPerThread>();

	public bool DisplayWcfRequestLimitReached { get; set; }

	public bool DisplayWcfServiceHostLimitReached { get; set; }

	public AnalyzeManagedWCF(NetDbgObj debugger)
	{
		this.debugger = debugger;
	}

	public List<WCFServiceSummary> GetWCFSummary(bool loadServiceConfigurationDetail, int maxNumberOfWcfServiceToList = 25)
	{
		IEnumerable<ClrObject> enumerable = from w in debugger.EnumerateHeapObjects()
			where ClrHelper.IsSubclassOf(w.GetHeapType(), "System.ServiceModel.ServiceHostBase")
			select w;
		List<WCFServiceSummary> list = new List<WCFServiceSummary>(enumerable.Count());
		int num = 0;
		DisplayWcfServiceHostLimitReached = false;
		foreach (dynamic item2 in enumerable)
		{
			if (num++ >= maxNumberOfWcfServiceToList)
			{
				DisplayWcfServiceHostLimitReached = true;
				break;
			}
			string contractName = GetServiceContractTypeName(item2);
			string text = ClrHelper.GetTypeName(item2);
			string state = ClrHelper.EnumValueAsString(item2, "state");
			string isThrottled = item2.serviceThrottle.isActive;
			int num2 = (int)item2.serviceThrottle.calls.count;
			int num3 = (int)item2.serviceThrottle.calls.capacity;
			int queued = (int)item2.serviceThrottle.calls.waiters._size;
			int num4 = (int)item2.serviceThrottle.sessions.count;
			int num5 = (int)item2.serviceThrottle.sessions.capacity;
			int queued2 = (int)item2.serviceThrottle.sessions.waiters._size;
			int num6 = 0;
			int num7 = 0;
			int queued3 = 0;
			if ((!ClrHelper.IsNull(item2.serviceThrottle.instanceContexts)))
			{
				num6 = (int)item2.serviceThrottle.instanceContexts.count;
				num7 = (int)item2.serviceThrottle.instanceContexts.capacity;
				queued3 = (int)item2.serviceThrottle.instanceContexts.waiters._size;
			}
			else
			{
				num6 = num4 + num2;
				num7 = (((long)num5 + (long)num3 > int.MaxValue) ? int.MaxValue : (num5 + num3));
			}
			string instaceMode = "---";
			string concurrencyMode = "---";
			Dictionary<string, object> dictionary = GetServiceBehaviorAttribute(item2, "System.ServiceModel.ServiceBehaviorAttribute", new string[2] { "instanceMode", "concurrencyMode" });
			if (!ClrHelper.IsNull((object)dictionary) && dictionary.Count > 0)
			{
				dynamic val = (from w in dictionary
					where w.Key.Equals("instanceMode")
					select w into s
					select s.Value).FirstOrDefault();
				dynamic val2 = (from w in dictionary
					where w.Key.Equals("concurrencyMode")
					select w into s
					select s.Value).FirstOrDefault();
				instaceMode = ClrHelper.EnumValueAsString(debugger.ClrHeap.GetTypeByName("System.ServiceModel.InstanceContextMode"), (object)(int)val);
				concurrencyMode = ClrHelper.EnumValueAsString(debugger.ClrHeap.GetTypeByName("System.ServiceModel.ConcurrencyMode"), (object)(int)val2);
			}
			int num8 = text.Split('.').Count();
			if (num8 > 1)
			{
				text = text.Split('.')[num8 - 1];
			}
			WCFServiceConfiguration wCFServiceConfiguration = null;
			if (loadServiceConfigurationDetail)
			{
				wCFServiceConfiguration = new WCFServiceConfiguration();
				long value = (long)item2.openTimeout._ticks;
				long value2 = (long)item2.closeTimeout._ticks;
				TimeSpan value3 = TimeSpan.FromTicks(value);
				TimeSpan value4 = TimeSpan.FromTicks(value2);
				wCFServiceConfiguration.ServiceTimeout.Timeouts.Add("OpenTimeout", value3);
				wCFServiceConfiguration.ServiceTimeout.Timeouts.Add("CloseTimeout", value4);
				Dictionary<string, object> dictionary2 = GetServiceBehaviorAttribute(item2, "System.ServiceModel.Description.ServiceMetadataBehavior", new string[3] { "httpGetEnabled", "httpsGetEnabled", "mexContract" });
				if (!ClrHelper.IsNull((object)dictionary2))
				{
					foreach (KeyValuePair<string, object> item3 in dictionary2)
					{
						string empty = string.Empty;
						empty = ((!item3.Key.Equals("mexContract")) ? ((string)(dynamic)item3.Value) : ((string)Convert.ToString(!ClrHelper.IsNull((dynamic)item3.Value))));
						wCFServiceConfiguration.Behaviors.Add(item3.Key, empty);
					}
				}
				List<WCFServiceChannel> channels = RetrieveChannelInformation(item2);
				wCFServiceConfiguration.Channels = channels;
				dynamic val3 = (int)item2.baseAddresses.items._size;
				dynamic val4 = item2.baseAddresses.items._items;
				for (int i = 0; i < val3; i++)
				{
					dynamic val5 = val4[i];
					string item = (string)val5.m_String;
					wCFServiceConfiguration.BaseAddress.Add(item);
				}
			}
			list.Add(new WCFServiceSummary(contractName, text, isThrottled, state, new WCFServiceThrottle(num4, num5, queued2), new WCFServiceThrottle(num2, num3, queued), new WCFServiceThrottle(num6, num7, queued3), instaceMode, concurrencyMode, wCFServiceConfiguration));
		}
		return list;
	}

	public List<WCFRequestItem> GetWCFRequest(int maxNumberOfWcfRequestsToList = 100)
	{
		opertationsContextList.Clear();
		foreach (NetDbgThread thread in debugger.Threads)
		{
			IEnumerable<OperationContextPerThread> operations = from p in thread.EnumerateStackObjects("System.ServiceModel.OperationContext+Holder")
				where !ClrHelper.IsNull((dynamic)ClrHelper.SafeGetObj(p, "context"))
				select p into s
				select (dynamic)ClrHelper.SafeGetObj(s, "context") into g
				group g by g.GetValue() into p
				select new OperationContextPerThread(thread, p.First());
			AddOperationToList(operations);
			IEnumerable<OperationContextPerThread> operations2 = from g in thread.EnumerateStackObjects("System.ServiceModel.OperationContext")
				group g by g.GetValue() into s
				select new OperationContextPerThread(thread, s.First());
			AddOperationToList(operations2);
		}
		IEnumerable<OperationContextPerThread> operations3 = from s in debugger.EnumerateHeapObjects("System.ServiceModel.OperationContext")
			select new OperationContextPerThread(null, s);
		AddOperationToList(operations3);
		List<WCFRequestItem> list = new List<WCFRequestItem>(opertationsContextList.Count);
		int num = 0;
		DisplayWcfRequestLimitReached = false;
		foreach (OperationContextPerThread opertationsContext in opertationsContextList)
		{
			if (num++ >= maxNumberOfWcfRequestsToList)
			{
				DisplayWcfRequestLimitReached = true;
				break;
			}
			dynamic stackObject = opertationsContext.StackObject;
			if (!((!ClrHelper.IsNull(stackObject) && !ClrHelper.IsNull(stackObject.host)) ? true : false))
			{
				continue;
			}
			string threadID = "---";
			if (!ClrHelper.IsNull((object)opertationsContext.Thread))
			{
				threadID = Globals.HelperFunctions.GetThreadIDWithLink(opertationsContext.Thread.ThreadID);
			}
			string endpoint = stackObject.channel.endpointDispatcher.listenUri.m_String;
			string contractType = GetServiceContractTypeName(stackObject.host);
			string aborted = stackObject.instanceContext.aborted;
			string operationTimeout = "---";
			long num2 = stackObject.channel.operationTimeout._ticks;
			if (num2 > 0 && num2 < long.MaxValue)
			{
				operationTimeout = TimeSpan.FromTicks(num2).ToString();
			}
			dynamic requestContext = GetRequestContext(stackObject);
			dynamic val = requestContext == null || requestContext is ClrNullValue;
			if (!(val ? true : false) && !((val | requestContext.IsNull()) ? true : false))
			{
				string text = ClrHelper.EnumValueAsString(requestContext, "state");
				string text2 = requestContext.replySent;
				string channelState = ClrHelper.EnumValueAsString(stackObject.channel, "state");
				ClrObject val2 = (ClrObject)GetHttpContextForSvc(stackObject.requestContext);
				string httpContext = (ClrHelper.IsNull((object)val2) ? "---" : val2.GetValue().ToString("X"));
				string methodName = GetServiceMethodCalled(stackObject, opertationsContext.Thread);
				double callDuration = 0.0;
				dynamic val3 = stackObject.channel.idleManager;
				if (!Convert.ToBoolean(text2) && !text.Equals("Closed"))
				{
					callDuration = GetServiceCallDuration(val2, val3);
				}
				WCFRequestItem item = new WCFRequestItem
				{
					Endpoint = endpoint,
					RequestState = text,
					ContractType = contractType,
					OperationTimeout = operationTimeout,
					ReplySent = text2,
					ChannelState = channelState,
					ThreadID = threadID,
					Aborted = aborted,
					HttpContext = httpContext,
					MethodName = methodName,
					CallDuration = callDuration,
					OperationContext = (ClrObject)stackObject
				};
				list.Add(item);
			}
		}
		return list;
	}

	public List<WCFSMSvchostRoute> GetSMSvchostRoutingTable()
	{
		List<WCFSMSvchostRoute> list = new List<WCFSMSvchostRoute>();
		foreach (dynamic item in debugger.EnumerateHeapObjects("System.ServiceModel.Activation.RoutingTable+MessageQueueAndPath"))
		{
			string endpoint = (string)item.uri.m_String;
			dynamic val = item.messageQueue;
			if ((!ClrHelper.IsNull(val)))
			{
				int sessionMessageQueue = (int)val.sessionMessages._size;
				int sessionWorker = (int)val.sessionWorkers._size;
				int queueSize = (int)val.maxQueueSize;
				_ = (string)ClrHelper.EnumValueAsString(val, "transportType");
				string state = ClrHelper.EnumValueAsString(val, "queueState");
				int num = (int)val.workers._size;
				dynamic val2 = val.workers._items;
				string[] array = new string[num];
				for (int i = 0; i < num; i++)
				{
					dynamic val3 = val2[i];
					array[i] = $"{((int)val3.processId).ToString()}";
				}
				dynamic val4 = val.app;
				string webSite = "---";
				string webAppPool = "---";
				if ((!ClrHelper.IsNull(val4)))
				{
					webSite = (string)val4.appKey;
					webAppPool = (string)val4.appPool.appPoolId;
				}
				list.Add(new WCFSMSvchostRoute(endpoint, sessionWorker, sessionMessageQueue, queueSize, state, webSite, webAppPool, array));
			}
		}
		return list;
	}

	public List<WcfClientConnectionSummary> GetWcfClientConnectionSumary(List<WcfClientRequest> clientRequests)
	{
		IEnumerable<ClrObject> enumerable = debugger.EnumerateHeapObjects("System.ServiceModel.Channels.TcpConnectionPoolRegistry+TcpConnectionPool");
		List<WcfClientConnectioPool> list = new List<WcfClientConnectioPool>();
		foreach (dynamic item2 in enumerable)
		{
			string poolName = (string)ClrHelper.SafeGetObj(item2, "name");
			int maxConnection = (int)ClrHelper.SafeGetObj(item2, "maxCount");
			int num = (int)ClrHelper.SafeGetObj(item2.endpointPools, "count");
			dynamic val = ClrHelper.SafeGetObj(item2.endpointPools, "entries");
			for (int i = 0; i < num; i++)
			{
				dynamic val2 = val[i];
				string connectionKey = (string)val2.key;
				list.Add(new WcfClientConnectioPool
				{
					ConnectionKey = connectionKey,
					MaxConnection = maxConnection,
					PoolName = poolName
				});
			}
		}
		foreach (dynamic item3 in debugger.EnumerateHeapObjects("System.Net.ServicePoint"))
		{
			string connectionKey2 = (string)ClrHelper.SafeGetObj(item3, "m_LookupString");
			int maxConnection2 = (int)ClrHelper.SafeGetObj(item3, "m_ConnectionLimit");
			list.Add(new WcfClientConnectioPool
			{
				ConnectionKey = connectionKey2,
				MaxConnection = maxConnection2,
				PoolName = "---"
			});
		}
		List<WcfClientConnectionSummary> list2 = new List<WcfClientConnectionSummary>();
		foreach (var item in from x in clientRequests
			group x by new { x.Endpoint, x.ConnectionPool } into s
			select new
			{
				Endpoint = s.First().Endpoint,
				ConnectionPool = s.First().ConnectionPool,
				ConnectionCount = s.Count()
			})
		{
			string endpoint = item.Endpoint;
			WcfClientConnectioPool wcfClientConnectioPool = list.Where((WcfClientConnectioPool w) => w.ConnectionKey.Equals(item.ConnectionPool)).FirstOrDefault();
			string connectionPoolName = ((wcfClientConnectioPool == null) ? "---" : wcfClientConnectioPool.PoolName);
			int currentConnection = clientRequests.Where((WcfClientRequest w) => w != null && w.Endpoint != null && w.Endpoint.Equals(item.Endpoint) && w.ConnectionPool != null && w.ConnectionPool.Equals(item.ConnectionPool) && w.IsRunningOnThread && !w.IsQueued).Count();
			int maxConnection3 = list.Where((WcfClientConnectioPool w) => w.ConnectionKey.Equals(item.ConnectionPool)).FirstOrDefault()?.MaxConnection ?? 0;
			int waitList = clientRequests.Where((WcfClientRequest w) => w.Endpoint == item.Endpoint && w.ConnectionPool == item.ConnectionPool && w.IsRunningOnThread && w.IsQueued).Count();
			list2.Add(new WcfClientConnectionSummary
			{
				Endpoint = endpoint,
				ConnectionPoolName = connectionPoolName,
				CurrentConnection = currentConnection,
				MaxConnection = maxConnection3,
				WaitList = waitList
			});
		}
		return list2;
	}

	public List<WcfClientRequest> GetWcfClientRequest()
	{
		//IL_1c78: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c7f: Expected O, but got Unknown
		//IL_1c9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ca5: Expected O, but got Unknown
		List<OperationContextPerThread> requests = new List<OperationContextPerThread>();
		foreach (NetDbgThread thread in debugger.Threads)
		{
			IEnumerable<OperationContextPerThread> source = from g in thread.EnumerateStackObjects("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel")
				group g by g.GetValue() into p
				select new OperationContextPerThread(thread, p.First());
			IEnumerable<OperationContextPerThread> source2 = from g in thread.EnumerateStackObjects("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel<System.ServiceModel.Channels.IRequestChannel>")
				group g by g.GetValue() into p
				select new OperationContextPerThread(thread, p.First());
			IEnumerable<OperationContextPerThread> source3 = from w in thread.EnumerateStackObjects()
				where ClrHelper.IsSubclassOf(w.GetHeapType(), "System.ServiceModel.Channels.TransportOutputChannel")
				select w into g
				group g by ((object)g).GetType() into p
				select new OperationContextPerThread(thread, p.First());
			requests.AddRange(source.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
			requests.AddRange(source2.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
			requests.AddRange(source3.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
		}
		IEnumerable<OperationContextPerThread> source4 = from s in debugger.EnumerateHeapObjects("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel")
			select new OperationContextPerThread(null, s);
		IEnumerable<OperationContextPerThread> source5 = from s in debugger.EnumerateHeapObjects("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel<System.ServiceModel.Channels.IRequestChannel>")
			select new OperationContextPerThread(null, s);
		IEnumerable<OperationContextPerThread> source6 = from w in debugger.EnumerateHeapObjects()
			where ClrHelper.IsSubclassOf(w.GetHeapType(), "System.ServiceModel.Channels.TransportOutputChannel")
			select w into s
			select new OperationContextPerThread(null, s);
		requests.AddRange(source4.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
		requests.AddRange(source5.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
		requests.AddRange(source6.Where((OperationContextPerThread p) => !requests.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
		List<WcfClientRequest> list = new List<WcfClientRequest>();
		foreach (OperationContextPerThread item in requests)
		{
			dynamic stackObject = item.StackObject;
			NetDbgThread thread2 = item.Thread;
			if (!((ClrHelper.GetTypeName(stackObject).Equals("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel") || ClrHelper.GetTypeName(stackObject).Equals("System.ServiceModel.Channels.ClientFramingDuplexSessionChannel") || ClrHelper.GetTypeName(stackObject).Equals("System.ServiceModel.Channels.HttpChannelFactory+HttpRequestChannel<System.ServiceModel.Channels.IRequestChannel>")) ? true : false))
			{
				continue;
			}
			string endpoint = (string)stackObject.to.uri.m_String;
			string state = ClrHelper.EnumValueAsString(stackObject, "state");
			bool flag = !ClrHelper.IsNull((object)thread2);
			string connectionPool = "---";
			string serviceType = "---";
			string methodCalled = "---";
			bool isQueued = false;
			TimeSpan callDuration = TimeSpan.Zero;
			TimeSpan requestTimeout = TimeSpan.Zero;
			_ = string.Empty;
			if ((!ClrHelper.IsNull(ClrHelper.SafeGetObj(stackObject, "connectionPoolHelper"))))
			{
				connectionPool = stackObject.connectionPoolHelper.connectionKey;
			}
			else if (flag)
			{
				dynamic val = thread2.EnumerateStackObjects("System.Net.HttpWebRequest").FirstOrDefault();
				if (!ClrHelper.IsNull(val) && !ClrHelper.IsNull(ClrHelper.SafeGetObj(val, "_ServicePoint")) && !ClrHelper.IsNull(ClrHelper.SafeGetObj(val._ServicePoint, "m_LookupString")))
				{
					connectionPool = (string)ClrHelper.SafeGetObj(val._ServicePoint, "m_LookupString");
				}
			}
			if (flag)
			{
				dynamic val2 = thread2.EnumerateStackObjects("System.ServiceModel.Dispatcher.ProxyOperationRuntime").FirstOrDefault();
				if ((!ClrHelper.IsNull(val2)))
				{
					if ((!ClrHelper.IsNull(ClrHelper.SafeGetObj(val2, "name"))))
					{
						methodCalled = (string)ClrHelper.SafeGetObj(val2, "name");
					}
					if (!ClrHelper.IsNull(ClrHelper.SafeGetObj(val2, "syncMethod")) && !ClrHelper.IsNull(ClrHelper.SafeGetObj(val2.syncMethod, "m_reflectedTypeCache")) && !ClrHelper.IsNull(ClrHelper.SafeGetObj(val2.syncMethod.m_reflectedTypeCache, "m_name")))
					{
						serviceType = (string)ClrHelper.SafeGetObj(val2.syncMethod.m_reflectedTypeCache, "m_name");
					}
				}
				foreach (ClrStackFrame item2 in thread2.ManagedThread.StackTrace)
				{
					string text = ((item2.Method != null) ? item2.Method.GetFullSignature() : "");
					if (item2.DisplayString.Contains("System.Net.ServicePoint.SubmitRequest") || text.Contains("System.Net.ServicePoint.SubmitRequest"))
					{
						isQueued = true;
					}
					if (item2.DisplayString.Contains("DuplexConnectionPoolHelper.AcceptPooledConnection") || text.Contains("DuplexConnectionPoolHelper.AcceptPooledConnection"))
					{
						isQueued = true;
					}
					if (item2.DisplayString.Contains("System.ServiceModel.Channels.SocketConnection.ReadCore") || text.Contains("System.ServiceModel.Channels.SocketConnection.ReadCore"))
					{
						ulong num = item2.StackPointer + 56;
						ClrType typeByName = debugger.ClrHeap.GetTypeByName("System.ServiceModel.TimeoutHelper");
						if (typeByName == null)
						{
							typeByName = debugger.ClrHeap.GetTypeByName("System.Runtime.TimeoutHelper");
						}
						ulong address = typeByName.GetFieldByName("deadline").GetAddress(num, true);
						ulong address2 = typeByName.GetFieldByName("originalTimeout").GetAddress(num, true);
						dynamic val3 = (dynamic)new ClrObject(debugger.ClrHeap, ((ClrField)typeByName.GetFieldByName("deadline")).Type, address, true);
						dynamic val4 = (dynamic)new ClrObject(debugger.ClrHeap, ((ClrField)typeByName.GetFieldByName("originalTimeout")).Type, address2, true);
						ulong dateData = (ulong)val3.dateData;
						long value = (long)val4._ticks;
						DateTime dateTime = DateTime.FromBinary((long)dateData);
						TimeSpan timeSpan = TimeSpan.FromTicks(value);
						DateTime dateTime2 = dateTime - timeSpan;
						callDuration = debugger.DumpCreationTime.ToUniversalTime() - dateTime2;
						requestTimeout = timeSpan;
					}
				}
				dynamic val5 = thread2.EnumerateStackObjects("System.Net.HttpWebRequest").FirstOrDefault();
				if ((!ClrHelper.IsNull(val5)))
				{
					int num2 = (int)val5._Timer.m_StartTimeMilliseconds;
					int num3 = (int)val5._Timer.m_DurationMilliseconds;
					TimeSpan timeSpan2 = TimeSpan.FromMilliseconds(num2);
					TimeSpan timeSpan3 = TimeSpan.FromSeconds(debugger.SystemUpTime);
					if (timeSpan3 > timeSpan2)
					{
						callDuration = timeSpan3 - timeSpan2;
					}
					requestTimeout = TimeSpan.FromMilliseconds(num3);
				}
			}
			ulong num4 = (ulong)stackObject.GetValue();
			list.Add(new WcfClientRequest
			{
				Endpoint = endpoint,
				State = state,
				ConnectionPool = connectionPool,
				ThreadID = (ClrHelper.IsNull((object)thread2) ? "---" : thread2.ThreadID.ToString()),
				ServiceType = serviceType,
				MethodCalled = methodCalled,
				IsQueued = isQueued,
				CallDuration = callDuration,
				RequestTimeout = requestTimeout,
				IsRunningOnThread = flag,
				DebugInfo = $"0x{num4:X}"
			});
		}
		return list;
	}

	private TimeSpan UnwrapTickCount(int tickCount)
	{
		return TimeSpan.FromMilliseconds((ulong)(tickCount & 0xFFFFFFFFu));
	}

	private TimeSpan WrappedSystemTime()
	{
		return TimeSpan.FromMilliseconds(TimeSpan.FromSeconds(debugger.SystemUpTime).TotalMilliseconds % 4294967295.0);
	}

	public string GenerateHTMLReportWCFRequest(List<WCFRequestItem> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string arg = ((results.Where((WCFRequestItem w) => !w.HttpContext.Equals("---")).Count() > 0) ? "" : "none");
		string text = ((results.Where((WCFRequestItem w) => w.Aborted.ToLower().Equals("true")).Count() > 0) ? "" : "none");
		string arg2 = ((results.Where((WCFRequestItem w) => !w.MethodName.Equals("---")).Count() > 0) ? "" : "none");
		string arg3 = ((results.Where((WCFRequestItem w) => !w.CallDuration.Equals("---")).Count() > 0) ? "" : "none");
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=10 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td><b>Service Type</b></td>");
		stringBuilder.Append("<td><b>Request Uri</b></td>");
		stringBuilder.AppendFormat("<td style='display:{0}'><b>Method</b></td>", arg2);
		stringBuilder.AppendFormat("<td style='display:{0}'><b>HttpContext</b></td>", arg);
		stringBuilder.Append("<td><b>State</b></td>");
		stringBuilder.AppendFormat("<td style='display:{0}'><b>Duration</b></td>", arg3);
		stringBuilder.Append("<td><b>Completed</b></td>");
		stringBuilder.AppendFormat("<td style='display:{0}'><b>Aborted</b></td>", text);
		stringBuilder.Append("<td><b>ThreadID</b></td>");
		stringBuilder.Append("</tr>");
		foreach (WCFRequestItem result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.ContractType);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.Endpoint);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td style='display:{0}'>", arg2);
			stringBuilder.Append(result.MethodName);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='center' style='display:{0}'>", arg);
			stringBuilder.Append(result.HttpContext);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.RequestState);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='right' style='display:{0}'>", arg3);
			stringBuilder.Append((result.CallDuration == 0.0) ? "---" : $"{result.CallDuration:0.00}s");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			stringBuilder.Append(result.ReplySent);
			stringBuilder.Append("</td>");
			if (!text.Equals("none"))
			{
				stringBuilder.Append(Convert.ToBoolean(result.Aborted) ? "<td align='center' style='color:red;'>" : "<td align='center'>");
				stringBuilder.Append(result.Aborted);
				stringBuilder.Append("</td>");
			}
			stringBuilder.Append("<td align='right'>");
			stringBuilder.Append(result.ThreadID);
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFSummary(List<WCFServiceSummary> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=0 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td width='100'><b>Service Host</b></td>");
		stringBuilder.Append("<td width='100'><b>Service Type</b></td>");
		stringBuilder.Append("<td width='80' align='left'><b>State</b></td>");
		stringBuilder.Append("<td width='100' align='left'><b>InstanceMode</b></td>");
		stringBuilder.Append("<td width='100' align='left'><b>ConcurrencyMode</b></td>");
		stringBuilder.Append("<td width='100' align='left'><b>IsThrottled?</b></td>");
		stringBuilder.Append("<td width='100' align='center'><b>Call/Max/Queue</b></td>");
		stringBuilder.Append("<td width='100' align='center'><b>Session/Max/Queue</b></td>");
		stringBuilder.Append("<td width='80' align='center'><b>Instance/Max/Queue</b></td>");
		stringBuilder.Append("</tr>");
		foreach (WCFServiceSummary result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.ServiceTypeHost);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.AppendFormat("<a href='#{0}'>{1}</a>", result.ServiceType + Globals.g_UniqueReference, result.ServiceType);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.State);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.InstanceMode);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.ConcurrencyMode);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.IsThrottled);
			stringBuilder.Append("</td>");
			stringBuilder.Append((result.Call.Usage == 1.0) ? "<td align='center' style='color:red;'>" : "<td align='center'>");
			stringBuilder.Append(result.Call.ToString());
			stringBuilder.Append("</td>");
			stringBuilder.Append((result.Session.Usage == 1.0) ? "<td align='center' style='color:red;'>" : "<td align='center'>");
			stringBuilder.Append(result.Session.ToString());
			stringBuilder.Append("</td>");
			stringBuilder.Append((result.Instance.Usage == 1.0) ? "<td align='center' style='color:red;'>" : "<td align='center'>");
			stringBuilder.Append(result.Instance.ToString());
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFConfiguration(WCFServiceConfiguration wcfConfiguration)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=0 class=myCustomText>");
		IEnumerable<KeyValuePair<string, TimeSpan>> source = wcfConfiguration.ServiceTimeout.Timeouts.Where((KeyValuePair<string, TimeSpan> w) => w.Key.Equals("OpenTimeout"));
		IEnumerable<KeyValuePair<string, TimeSpan>> source2 = wcfConfiguration.ServiceTimeout.Timeouts.Where((KeyValuePair<string, TimeSpan> w) => w.Key.Equals("CloseTimeout"));
		IEnumerable<KeyValuePair<string, string>> source3 = wcfConfiguration.Behaviors.Where((KeyValuePair<string, string> w) => w.Key.Equals("httpGetEnabled"));
		IEnumerable<KeyValuePair<string, string>> source4 = wcfConfiguration.Behaviors.Where((KeyValuePair<string, string> w) => w.Key.Equals("httpsGetEnabled"));
		IEnumerable<KeyValuePair<string, string>> source5 = wcfConfiguration.Behaviors.Where((KeyValuePair<string, string> w) => w.Key.Equals("mexContract"));
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td>Open Timeout: </td>");
		stringBuilder.AppendFormat("<td>{0}</td>", (source.Count() > 0) ? source.Select((KeyValuePair<string, TimeSpan> s) => s.Value).FirstOrDefault().ToString() : "---");
		stringBuilder.Append("</tr>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td>Close Timeout: </td>");
		stringBuilder.AppendFormat("<td>{0}</td>", (source2.Count() > 0) ? source2.Select((KeyValuePair<string, TimeSpan> s) => s.Value).FirstOrDefault().ToString() : "---");
		stringBuilder.Append("</tr>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td>Http Get Enabled: </td>");
		stringBuilder.AppendFormat("<td>{0}</td>", (source3.Count() > 0) ? source3.Select((KeyValuePair<string, string> s) => s.Value).FirstOrDefault().ToString() : "---");
		stringBuilder.Append("</tr>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td>Https Get Enabled: </td>");
		stringBuilder.AppendFormat("<td>{0}</td>", (source4.Count() > 0) ? source4.Select((KeyValuePair<string, string> s) => s.Value).FirstOrDefault().ToString() : "---");
		stringBuilder.Append("</tr>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td>Mex Enabled: </td>");
		stringBuilder.AppendFormat("<td>{0}</td>", (source5.Count() > 0) ? source5.Select((KeyValuePair<string, string> s) => s.Value).FirstOrDefault().ToString() : "---");
		stringBuilder.Append("</tr>");
		stringBuilder.Append("</table>");
		stringBuilder.Append("<br>");
		if (wcfConfiguration.BaseAddress.Count > 0)
		{
			stringBuilder.Append("<h2><b>Base Addresses</b></h2>");
			foreach (string item in wcfConfiguration.BaseAddress)
			{
				stringBuilder.Append(item);
				stringBuilder.Append("<br>");
			}
		}
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFChannel(List<WCFServiceChannel> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=0 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<th width='150' align='left'><b>Binding Name</b></th>");
		stringBuilder.Append("<th width='80'  align='left'><b>State</b></th>");
		stringBuilder.Append("<th width='150' align='left'><b>SessionMode</b></th>");
		stringBuilder.Append("<th width='150' align='left'><b>RealibleSession</b></th>");
		stringBuilder.Append("<th width='150' align='center'><b>Send<br>Timeout</b></th>");
		stringBuilder.Append("<th width='150' align='center'><b>Open<br>Timeout</b></th>");
		stringBuilder.Append("<th width='150' align='center'><b>Close<br>Timeout</b></th>");
		stringBuilder.Append("<th width='150' align='center'><b>Receive<br>Timeout</b></th>");
		stringBuilder.Append("<th width='150' align='center'><b>Inactivity<br>Timeout</b></th>");
		stringBuilder.Append("<th width='400' align='left'><b>ListenUri</b></th>");
		stringBuilder.Append("</tr></thead>");
		foreach (WCFServiceChannel result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.Name);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.State);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.SessionMode);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='left'>");
			stringBuilder.Append(result.ReliableSession);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			TimeSpan timeSpan = (from w in result.channelTimeout.Timeouts
				where w.Key.Equals("OpenTimeout")
				select w into s
				select s.Value).FirstOrDefault();
			stringBuilder.Append((timeSpan != TimeSpan.Zero) ? timeSpan.ToString() : "---");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			TimeSpan timeSpan2 = (from w in result.channelTimeout.Timeouts
				where w.Key.Equals("CloseTimeout")
				select w into s
				select s.Value).FirstOrDefault();
			stringBuilder.Append((timeSpan2 != TimeSpan.Zero) ? timeSpan2.ToString() : "---");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			TimeSpan timeSpan3 = (from w in result.channelTimeout.Timeouts
				where w.Key.Equals("SendTimeout")
				select w into s
				select s.Value).FirstOrDefault();
			stringBuilder.Append((timeSpan3 != TimeSpan.Zero) ? timeSpan3.ToString() : "---");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			TimeSpan timeSpan4 = (from w in result.channelTimeout.Timeouts
				where w.Key.Equals("ReceiveTimeout")
				select w into s
				select s.Value).FirstOrDefault();
			stringBuilder.Append((timeSpan4 != TimeSpan.Zero) ? timeSpan4.ToString() : "---");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='center'>");
			TimeSpan timeSpan5 = (from w in result.channelTimeout.Timeouts
				where w.Key.Equals("InactivityTimeout")
				select w into s
				select s.Value).FirstOrDefault();
			stringBuilder.Append((timeSpan5 != TimeSpan.Zero) ? timeSpan5.ToString() : "---");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td nowrap>");
			stringBuilder.Append(result.ListenURI);
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFSMSvcHostRoutingTable(List<WCFSMSvchostRoute> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=10 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td><b>Endpoint</b></td>");
		stringBuilder.Append("<td><b>State</b></td>");
		stringBuilder.Append("<td><b>WebSite</b></td>");
		stringBuilder.Append("<td><b>AppPool</b></td>");
		stringBuilder.Append("<td><b>Session Workers</b></td>");
		stringBuilder.Append("<td><b>Messages/QeueSize</b></td>");
		stringBuilder.Append("<td><b>Dest. Process</b></td>");
		stringBuilder.Append("</tr>");
		foreach (WCFSMSvchostRoute result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.Endpoint);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.State);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.WebSite);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.WebAppPool);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.SessionWorker);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.AppendFormat("{0} / {1}", result.SessionMessageQueue, result.QueueSize);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			string[] process = result.Process;
			foreach (string arg in process)
			{
				stringBuilder.AppendFormat("ID: {0}", arg);
			}
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFClientSummary(List<WcfClientConnectionSummary> results)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=10 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td><b>Endpoint</b></td>");
		stringBuilder.Append("<td align='right' width='80'><b>ConnectionPool</b></td>");
		stringBuilder.Append("<td align='right' width='80'><b>Current/Max</b></td>");
		stringBuilder.Append("<td align='right' width='80'><b>Waiting</b></td>");
		stringBuilder.Append("</tr>");
		foreach (WcfClientConnectionSummary result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.Endpoint);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='right'>");
			stringBuilder.Append(result.ConnectionPoolName);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='right'>");
			stringBuilder.Append($"{result.CurrentConnection}/{result.MaxConnection}");
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='right'>");
			stringBuilder.Append(result.WaitList);
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public string GenerateHTMLReportWCFClientRequest(List<WcfClientRequest> results)
	{
		string arg = ((results.Where((WcfClientRequest w) => w.CallDuration != TimeSpan.Zero).Count() > 0) ? "" : "none");
		string arg2 = ((results.Where((WcfClientRequest w) => w.RequestTimeout != TimeSpan.Zero).Count() > 0) ? "" : "none");
		string arg3 = ((results.Where((WcfClientRequest w) => w.IsQueued).Count() > 0) ? "" : "none");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<table border=0 cellspacing=0 cellpadding=10 class=myCustomText>");
		stringBuilder.Append("<tr>");
		stringBuilder.Append("<td><b>Service Type</b></td>");
		stringBuilder.Append("<td><b>Request Uri</b></td>");
		stringBuilder.Append("<td><b>State</b></td>");
		stringBuilder.AppendFormat("<td style='display:{0}'><b>Timeout</b></td>", arg2);
		stringBuilder.AppendFormat("<td style='display:{0}'><b>Duration</b></td>", arg);
		stringBuilder.AppendFormat("<td align='center' style='display:{0}'><b>Queued</b></td>", arg3);
		stringBuilder.Append("<td><b>Method</b></td>");
		stringBuilder.Append("<td align='right'><b>ThreadID</b></td>");
		stringBuilder.Append("<td><b>Debug</b></td>");
		stringBuilder.Append("</tr>");
		foreach (WcfClientRequest result in results)
		{
			stringBuilder.Append("<tr>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.ServiceType);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td>");
			stringBuilder.Append(result.Endpoint);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td>");
			stringBuilder.Append(result.State);
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='right' style='display:{0}'>", arg2);
			stringBuilder.Append((result.RequestTimeout == TimeSpan.Zero) ? "---" : result.RequestTimeout.ToString("mm\\:ss"));
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='right' style='display:{0}'>", arg);
			stringBuilder.Append((result.CallDuration == TimeSpan.Zero) ? "---" : result.CallDuration.ToString("mm\\:ss\\.ff"));
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td align='center' style='display:{0}'>", arg3);
			stringBuilder.Append((!result.IsQueued) ? "No" : "Yes");
			stringBuilder.Append("</td>");
			stringBuilder.AppendFormat("<td>");
			stringBuilder.Append(result.MethodCalled);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='right'>");
			stringBuilder.Append(result.ThreadID);
			stringBuilder.Append("</td>");
			stringBuilder.Append("<td align='right'>");
			stringBuilder.Append(result.DebugInfo);
			stringBuilder.Append("</td>");
			stringBuilder.Append("</tr>");
		}
		stringBuilder.Append("</table>");
		return stringBuilder.ToString();
	}

	public List<DebugDiagReportIssue> CheckForThrottlingIssues(List<WCFServiceSummary> serviceSummary)
	{
		List<DebugDiagReportIssue> list = new List<DebugDiagReportIssue>();
		foreach (WCFServiceSummary item in serviceSummary)
		{
			if (item.Call.Usage == 1.0)
			{
				list.Add(new DebugDiagReportIssue("Error", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Call.Count) + " WCF Calls</b> currently running and the <b>MaxConcurrentCalls=" + Convert.ToString(item.Call.Max) + "</b> for the service <b>" + item.ServiceType + "</b>.<br><br>This will cause slow performance in the application.  Any additional incoming WCF requests will not be processed until these calls complete.<br/><br/>WCF Requests in Queue: <b>" + item.Call.Queued + "</b>", "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", 12345));
			}
			else if (item.Call.Usage > 0.5)
			{
				list.Add(new DebugDiagReportIssue("Warning", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Call.Count) + " WCF Calls</b> currently running and the <b>MaxConcurrentCalls = " + Convert.ToString(item.Call.Max) + "</b>", "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", 2));
			}
			if (item.Session.Usage == 1.0)
			{
				list.Add(new DebugDiagReportIssue("Error", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Session.Count) + " WCF Sessions</b> currently running and the <b>MaxConcurrentSessions=" + Convert.ToString(item.Session.Max) + "</b> for the service <b>" + item.ServiceType + "</b>.<br><br>This will cause slow performance in the application.  Any additional incoming WCF requests (which require additional Sessions) will not be processed until these calls complete.<br/><br/>WCF Sessions in Queue: <b>" + item.Session.Queued + "</b>", "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", item.Session.Queued));
			}
			else if (item.Session.Usage > 0.5)
			{
				list.Add(new DebugDiagReportIssue("Warning", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Session.Count) + " WCF Sessions</b> currently running and the MaxConcurrentSessions = " + Convert.ToString(item.Session.Max), "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", 2));
			}
			if (item.Instance.Usage == 1.0)
			{
				list.Add(new DebugDiagReportIssue("Error", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Instance.Count) + " WCF Instances</b> currently running and the <b>MaxConcurrentInstances=" + Convert.ToString(item.Instance.Max) + "</b> for the service <b>" + item.ServiceType + "</b>.<br><br>This will cause slow performance in the application.  Any additional incoming WCF requests (which require additional Instances) will not be processed until these calls complete.<br/><br/>WCF Instances in Queue: <b>" + item.Instance.Queued + "</b>", "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", item.Instance.Queued));
			}
			else if (item.Instance.Usage > 0.5)
			{
				list.Add(new DebugDiagReportIssue("Warning", "In <b>#DUMP_FILE#</b> there are <b>" + Convert.ToString(item.Instance.Count) + "</b> WCF Instances currently running and the MaxConcurrentInstances = " + Convert.ToString(item.Instance.Max), "Please check <a href='#WCFTHROTTLE#UR#'>The WCF Service Throttling Settings report </a> for more information and review the article <a href='http://blogs.msdn.com/distributedservices/archive/2010/03/23/wcf-services-appear-hung-due-to-maxconcurrentsessions-limit-being-hit.aspx'>WCF Service using wsHttpBinding hangs after 10 concurrent requests</a> for more information on hangs caused by WCF Service Throttling settings", 2));
			}
		}
		return list;
	}

	public List<DebugDiagReportIssue> CheckForExceptionInRequest(List<WCFRequestItem> wcfRequest)
	{
		List<DebugDiagReportIssue> list = new List<DebugDiagReportIssue>();
		List<ExceptionPerService> wCFRequestException = GetWCFRequestException(wcfRequest);
		string arg = "<a href = '#WCFREQUEST#UR#'>WCF Request report</a>";
		string arg2 = "<a href = '#ManagedExceptionsInHeapsReport#UR#'>Managed Exceptions</a>";
		foreach (ExceptionPerService item in wCFRequestException)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("In <b>{0}</b> was found <b>{1}</b> WCF requests aborted for <b>{2}</b>, this means that exceptions happened in the request.", Convert.ToString(Globals.g_ShortDumpFileName), item.ExceptionCount, item.ServiceContract);
			if (item.ExceptionList.Count > 0)
			{
				stringBuilder.Append("<br><br>Exceptions related to request:");
				stringBuilder.Append("<br><ul style='padding-left:15px'>");
				foreach (ClrException exception in item.ExceptionList)
				{
					stringBuilder.Append("<li style='padding-left:5'>");
					stringBuilder.AppendFormat("<b>Exception Type:</b> {0} - <b>Message:</b> {1}", exception.Type.Name.ToString(), exception.Message);
					stringBuilder.Append("</li>");
				}
				stringBuilder.Append("</ul>");
			}
			stringBuilder.AppendFormat("<br>See request details in {0}", arg);
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendFormat("Please check {0} for detailed information about the exceptions and fix them.", arg2);
			list.Add(new DebugDiagReportIssue("Error", stringBuilder.ToString(), stringBuilder2.ToString()));
		}
		return list;
	}

	public List<DebugDiagReportIssue> CheckForWcfClientLimits(List<WcfClientConnectionSummary> wcfClient)
	{
		List<DebugDiagReportIssue> list = new List<DebugDiagReportIssue>();
		string arg = "<a href = '#WCFCLIENTCONN#UR#'>WCF Client Connection Summary</a>";
		string arg2 = "<a href = 'https://msdn.microsoft.com/en-us/library/ms731343(v=vs.110).aspx'>NetTcpBindig</a>";
		string arg3 = "<a href = 'https://msdn.microsoft.com/en-us/library/7af54za5.aspx'>Managing Connections</a>";
		wcfClient.Where((WcfClientConnectionSummary c) => c.WaitList > 0).Any();
		foreach (WcfClientConnectionSummary item in wcfClient)
		{
			if (item.CurrentConnection > 0 && item.CurrentConnection == item.MaxConnection)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("In <b>#DUMP_FILE#</b> there are WCF client requests waiting to be sent to remote service due to outbound connection limits defined per endpoint/host.");
				stringBuilder.AppendFormat("<br><br> {0}", arg);
				StringBuilder stringBuilder2 = new StringBuilder();
				stringBuilder2.AppendFormat("If endpoint is http, you can increase the number of connections available by either modifying the 'maxconnection' parameter in the application configuration file (see <connectionManagement> Element), or by modifying the appropriate ConnectionLimit property programmatically (see {0}).<br><br>", arg3);
				stringBuilder2.AppendFormat("If endpoint is net.tcp, you can increase the number of connections availble by modifying 'NetTcpBinding/MaxConnections' (see {0}) in the application configuration file.", arg2);
				list.Add(new DebugDiagReportIssue("Error", stringBuilder.ToString(), stringBuilder2.ToString()));
				break;
			}
		}
		return list;
	}

	private void AddOperationToList(IEnumerable<OperationContextPerThread> operations)
	{
		opertationsContextList.AddRange(operations.Where((OperationContextPerThread p) => !opertationsContextList.Any((OperationContextPerThread p2) => p.StackObject.GetValue() == p2.StackObject.GetValue())));
	}

	private List<ExceptionPerService> GetWCFRequestException(List<WCFRequestItem> wcfRequest)
	{
		List<ExceptionPerService> list = new List<ExceptionPerService>();
		var enumerable = from w in wcfRequest
			where Convert.ToBoolean(w.Aborted)
			select w into g
			group g by g.ContractType into s
			select new
			{
				Service = s.First(),
				Count = s.Count()
			};
		List<ClrException> source = debugger.EnumerateHeapExceptionObjects().ToList();
		foreach (var item in enumerable)
		{
			string serviceContractType = item.Service.ContractType;
			List<ClrException> exceptionList = (from w in source
				where w.StackTrace.Any((ClrStackFrame s) => s.DisplayString.Contains(serviceContractType))
				select w into g
				group g by new { g.Type, g.Message } into s
				select s.First()).ToList();
			list.Add(new ExceptionPerService
			{
				ServiceContract = serviceContractType,
				ExceptionCount = item.Count,
				ExceptionList = exceptionList
			});
		}
		return list;
	}

	private static List<WCFServiceChannel> RetrieveChannelInformation(dynamic serviceHost)
	{
		List<Tuple<string, string, TimeoutSettings>> list = new List<Tuple<string, string, TimeoutSettings>>();
		dynamic val = serviceHost.description.endpoints.items._items;
		int num = (int)serviceHost.description.endpoints.items._size;
		if ((!ClrHelper.IsNull(val)))
		{
			for (int i = 0; i < num; i++)
			{
				dynamic val2 = val[i];
				string item = (string)val2.binding.name;
				string text = ((ClrHelper.IsNull(ClrHelper.SafeGetObj(val2.binding, "reliableSession"))) ? "False" : ((string)val2.binding.reliableSession.enabled));
				TimeoutSettings timeoutSettings = new TimeoutSettings();
				TimeSpan value = TimeSpan.Zero;
				if (Convert.ToBoolean(text))
				{
					value = TimeSpan.FromTicks((long)val2.binding.session.inactivityTimeout._ticks);
				}
				long value2 = (long)val2.binding.openTimeout._ticks;
				long value3 = (long)val2.binding.closeTimeout._ticks;
				long value4 = (long)val2.binding.sendTimeout._ticks;
				long value5 = (long)val2.binding.receiveTimeout._ticks;
				timeoutSettings.Timeouts.Add("OpenTimeout", TimeSpan.FromTicks(value2));
				timeoutSettings.Timeouts.Add("CloseTimeout", TimeSpan.FromTicks(value3));
				timeoutSettings.Timeouts.Add("SendTimeout", TimeSpan.FromTicks(value4));
				timeoutSettings.Timeouts.Add("ReceiveTimeout", TimeSpan.FromTicks(value5));
				timeoutSettings.Timeouts.Add("InactivityTimeout", value);
				list.Add(new Tuple<string, string, TimeoutSettings>(item, text, timeoutSettings));
			}
		}
		dynamic val3 = serviceHost.channelDispatchers;
		List<WCFServiceChannel> list2 = new List<WCFServiceChannel>();
		if ((!ClrHelper.IsNull(val3)))
		{
			int num2 = (int)val3.items._size;
			dynamic val4 = val3.items._items;
			for (int j = 0; j < num2; j++)
			{
				dynamic val5 = val4[j];
				string channelName = (string)val5.bindingName;
				if (string.IsNullOrEmpty(channelName))
				{
					channelName = "---";
				}
				if (channelName.Split(':').Count() >= 3)
				{
					channelName = channelName.Split(':')[2];
				}
				dynamic val6 = ClrHelper.SafeGetObj(val5.listener, "uri");
				if (ClrHelper.IsNull(val6))
				{
					val6 = ClrHelper.SafeGetObj(val5.listener.innerChannelListener, "uri");
				}
				string listenURI = (string)(((!ClrHelper.IsNull(val6))) ? val6.m_String : "---");
				_ = (string)val5.aborted;
				string sessionMode = (string)val5.session;
				string state = ClrHelper.EnumValueAsString(val5, "state");
				string reliableSession = "False";
				IEnumerable<Tuple<string, string, TimeoutSettings>> source = list.Where((Tuple<string, string, TimeoutSettings> w) => w.Item1.Equals(channelName));
				TimeoutSettings channelTimeout = new TimeoutSettings();
				if (source.Count() > 0)
				{
					reliableSession = source.Select((Tuple<string, string, TimeoutSettings> s) => s.Item2).First();
					channelTimeout = source.Select((Tuple<string, string, TimeoutSettings> s) => s.Item3).First();
				}
				list2.Add(new WCFServiceChannel
				{
					Name = channelName,
					ListenURI = listenURI,
					SessionMode = sessionMode,
					State = state,
					ReliableSession = reliableSession,
					channelTimeout = channelTimeout
				});
			}
		}
		return list2;
	}

	private static Dictionary<string, dynamic> GetServiceBehaviorAttribute(dynamic host, string behaviorTypeName, string[] fields)
	{
		if (ClrHelper.IsNull(host.description))
		{
			return null;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dynamic val = ClrHelper.SafeGetObj(host.description.behaviors.items, "_items");
		int num = (int)ClrHelper.SafeGetObj(host.description.behaviors.items, "_size");
		if ((!ClrHelper.IsNull(val)))
		{
			for (int i = 0; i < num; i++)
			{
				dynamic val2 = val[i];
				if (!((string)(((!ClrHelper.IsNull(val2))) ? ClrHelper.GetTypeName(val2) : "")).Equals(behaviorTypeName))
				{
					continue;
				}
				foreach (string text in fields)
				{
					dynamic val3 = ClrHelper.SafeGetObj(val2, text);
					if ((!ClrHelper.IsNull(val3)))
					{
						dictionary.Add(text, val3);
					}
				}
				break;
			}
		}
		return dictionary;
	}

	private static ClrObject GetHttpContextForSvc(dynamic requestContext)
	{
		dynamic val = null;
		switch (ClrHelper.GetTypeName(requestContext))
		{
		case "System.ServiceModel.Channels.ContextChannelRequestContext":
			val = requestContext.innerContext.result;
			break;
		case "System.ServiceModel.Activation.HostedHttpRequestAsyncResult":
			val = requestContext;
			break;
		case "System.ServiceModel.Activation.HostedHttpContext":
			val = requestContext.result;
			break;
		case "System.ServiceModel.Security.SecuritySessionServerSettings+ServerSecuritySessionChannel+ReceiveRequestAsyncResult":
			val = requestContext.innerRequestContext.result;
			break;
		case "System.ServiceModel.Dispatcher.MessageRpc+Wrapper":
			val = requestContext.rpc.RequestContext.result;
			break;
		}
		if (val != null)
		{
			val = val.context._context;
		}
		return (ClrObject)val;
	}

	private static dynamic GetRequestContext(ClrObject operationContext)
	{
		if (!((string)ClrHelper.GetTypeName(((dynamic)operationContext).requestContext)).Equals("System.ServiceModel.Channels.ContextChannelRequestContext"))
		{
			return ((dynamic)operationContext).requestContext;
		}
		return ((dynamic)operationContext).requestContext.innerContext;
	}

	private static string GetServiceContractTypeName(dynamic host)
	{
		string text = (string)host.description.configurationName;
		if (string.IsNullOrEmpty(text))
		{
			text = ClrHelper.GetTypeName(host);
		}
		return text;
	}

	private static string GetServiceMethodCalled(dynamic operationContext, NetDbgThread netDbgThread)
	{
		if (!ClrHelper.IsNull((object)netDbgThread))
		{
			dynamic val = netDbgThread.FindFirstStackObject("System.ServiceModel.Dispatcher.SyncMethodInvoker");
			if (val == null)
			{
				dynamic val2 = netDbgThread.FindFirstStackObject("System.ServiceModel.Dispatcher.DispatchOperationRuntime");
				val = ((val2 != null) ? val2.invoker : null);
			}
			if ((!ClrHelper.IsNull(val)))
			{
				return ClrHelper.ToString(val.method.m_name);
			}
		}
		string result = "---";
		if (ClrHelper.IsNull(operationContext.request))
		{
			return result;
		}
		dynamic val3 = ((!((string)ClrHelper.GetTypeName(operationContext.request)).Equals("System.ServiceModel.Security.SecurityVerifiedMessage")) ? operationContext.request : operationContext.request.innerMessage);
		dynamic val4 = val3.headers;
		string text = string.Empty;
		if ((!ClrHelper.IsNull(val4)))
		{
			int num = (int)val4.headerCount;
			dynamic val5 = val4.headers;
			for (int i = 0; i < num; i++)
			{
				dynamic val6 = val5[i];
				if (!ClrHelper.IsNull(val6) && (byte)val6.kind == 0 && !ClrHelper.IsNull(ClrHelper.SafeGetObj(val6.info, "action")))
				{
					text = (string)val6.info.action;
					break;
				}
			}
		}
		int num2 = 0;
		dynamic val7 = operationContext.instanceContext.behavior.immutableRuntime.demuxer.map.list;
		if ((!ClrHelper.IsNull(val7)))
		{
			num2 = (int)val7.count;
		}
		if (num2 > 0)
		{
			dynamic val8 = val7.head;
			while ((!ClrHelper.IsNull(val8)))
			{
				string text2 = (string)val8.key;
				dynamic val9 = val8.value;
				if (text2.ToLower().Equals(text.ToLower()))
				{
					result = (string)val9.name;
					break;
				}
				val8 = val8.next;
			}
		}
		return result;
	}

	private double GetServiceCallDuration(ClrObject svcHttpContext, dynamic sessionIdleManager)
	{
		double result = 0.0;
		if (!ClrHelper.IsNull(sessionIdleManager) && ClrHelper.GetTypeName(sessionIdleManager) == "System.ServiceModel.Channels.ServiceChannel+SessionIdleManager")
		{
			dynamic val = ClrHelper.SafeGetObj(sessionIdleManager, "timer");
			long num = (long)sessionIdleManager.idleTicks;
			long fileTime = (long)val.dueTime;
			if (num > 0 && num < long.MaxValue)
			{
				TimeSpan timeSpan = TimeSpan.FromTicks(num);
				DateTime dateTime = DateTime.FromFileTimeUtc(fileTime) - timeSpan;
				result = (debugger.DumpCreationTime.ToUniversalTime() - dateTime).TotalSeconds;
			}
		}
		else if (!ClrHelper.IsNull((object)svcHttpContext))
		{
			DateTime dateTime2 = DateTime.FromBinary((long)(ulong)((dynamic)svcHttpContext)._utcTimestamp.dateData);
			result = (debugger.DumpCreationTime.ToUniversalTime() - dateTime2).TotalSeconds;
		}
		return result;
	}
}
