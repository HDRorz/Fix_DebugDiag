using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Web;
using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using DebugDiag.DotNet.HtmlHelpers;
using DebugDiag.DotNet.Reports;
using DebugDiag.DotNet.Util;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.RuntimeExt;

namespace DebugDiag.AnalysisRules;

public class DotNetMemoryAnalysis : IMultiDumpRule, IAnalysisRuleBase, IAnalysisRuleMetadata, IMultiDumpRuleFilter
{
	public struct HeapStatsEntry
	{
		public string TypeName;

		public int NumObjects;

		public long TotalSize;

		public HeapStatsEntry(IGrouping<ClrType, ClrObject> group)
		{
			TypeName = group.Key.Name;
			NumObjects = group.Count();
			TotalSize = group.Sum((ClrObject o) => (long)group.Key.GetSize(o.GetValue()));
		}
	}

	private class ObjSizeHelper
	{
		public static ulong ObjSize(ClrRuntime clr, ulong objAddress)
		{
			if (objAddress == 0L)
			{
				throw new ArgumentException("objAddress cannot be 0x00");
			}
			ClrHeap heap = clr.GetHeap();
			ClrType objectTypeSafe = ExtensionMethods.GetObjectTypeSafe(heap, objAddress);
			if (objectTypeSafe == null)
			{
				throw new InvalidOperationException("Specified address is not a managed object.");
			}
			return ObjSize(heap, objectTypeSafe, objAddress);
		}

		public static ulong ObjSize(ClrHeap heap, ClrType objType, ulong objAddress)
		{
			ulong num = 0uL;
			ObjRef item = new ObjRef(objAddress, objType, interior: false);
			HashSet<ulong> hashSet = new HashSet<ulong>();
			Queue<ObjRef> queue = new Queue<ObjRef>();
			queue.Enqueue(item);
			while (queue.Count > 0)
			{
				item = queue.Dequeue();
				if (!hashSet.Contains(item.Address))
				{
					hashSet.Add(item.Address);
					ClrType type = item.Type;
					if (!type.IsValueClass || (type.IsValueClass && !item.Interior))
					{
						num += item.RawSize();
					}
					item.QueueReferencedObjects(heap, queue, hashSet);
				}
			}
			return num;
		}
	}

	private struct ObjRef
	{
		public ulong Address { get; set; }

		public ClrType Type { get; set; }

		public bool Interior { get; set; }

		public ObjRef(ulong address, ClrType type, bool interior)
		{
			this = default(ObjRef);
			if (address != 0L && type == null)
			{
				throw new ArgumentNullException("type");
			}
			Address = address;
			Type = type;
			Interior = interior;
		}

		public int ArraySize()
		{
			return Type.BaseSize + Type.GetArrayLength(Address) * Type.ElementSize;
		}

		public ulong RawSize()
		{
			ulong num = 0uL;
			num = (Type.IsArray ? ((ulong)ArraySize()) : ((!Type.IsString) ? ((ulong)Type.BaseSize) : Type.GetSize(Address)));
			return Align(num);
		}

		private ulong Align(ulong size)
		{
			ulong num = AlignConst(Type.Heap.GetSegmentByAddress(Address).Large);
			return (size + num) & ~num;
		}

		private ulong AlignConst(bool loh)
		{
			if (Type.Heap.GetRuntime().PointerSize == 4 && !loh)
			{
				return 3uL;
			}
			return 7uL;
		}

		public bool IsObject()
		{
			return Type.IsObjectReference;
		}

		public bool ElemIsPrimitive()
		{
			if (Type.ArrayComponentType != null)
			{
				return Type.ArrayComponentType.IsPrimitive;
			}
			return false;
		}

		private ClrType FindArrayComponentType(ClrHeap heap)
		{
			if (Type.ArrayComponentType != null)
			{
				return Type.ArrayComponentType;
			}
			string utypeName = Type.Name.Substring(0, Type.Name.Length);
			if (utypeName.EndsWith("[]"))
			{
				return null;
			}
			ClrType val = (from x in heap.EnumerateTypes()
				where x.Name == utypeName
				select x).FirstOrDefault();
			if (val != null && !val.IsPrimitive && !val.IsValueClass)
			{
				return val;
			}
			return null;
		}

		private void GetArrayReferences(ClrHeap heap, Queue<ObjRef> queue, HashSet<ulong> seenObjs)
		{
			if (ElemIsPrimitive() || !Type.ContainsPointers)
			{
				return;
			}
			ClrType val = FindArrayComponentType(heap);
			if (val != null && (val.IsPrimitive || (val.IsValueClass && !val.ContainsPointers)))
			{
				return;
			}
			int arrayLength = Type.GetArrayLength(Address);
			for (int i = 0; i < arrayLength; i++)
			{
				if (val == null || val.IsObjectReference)
				{
					Enqueue(Dereference(Type.GetArrayElementAddress(Address, i), heap), heap, null, queue, seenObjs);
				}
				else if (val.IsValueClass && val.ContainsPointers)
				{
					ulong arrayElementAddress = Type.GetArrayElementAddress(Address, i);
					queue.Enqueue(new ObjRef(arrayElementAddress, Type.ArrayComponentType, interior: true));
				}
			}
		}

		private void GetFieldReferences(ClrHeap heap, Queue<ObjRef> queue, HashSet<ulong> seenObjs)
		{
			if (Type.IsPrimitive || Type.IsString || Type.IsEnum || !Type.ContainsPointers)
			{
				return;
			}
			for (int i = 0; i < Type.Fields.Count; i++)
			{
				ClrInstanceField val = Type.Fields[i];
				if (((ClrField)val).IsObjectReference())
				{
					Enqueue(Dereference(val.GetAddress(Address, Interior), heap), heap, null, queue, seenObjs);
				}
				else if (((ClrField)val).IsValueClass() && ((ClrField)val).Type != null && ((ClrField)val).Type.ContainsPointers)
				{
					ulong address = val.GetAddress(Address, Interior);
					new ObjRef(address, ((ClrField)val).Type, interior: true).GetFieldReferences(heap, queue, seenObjs);
				}
			}
		}

		public void QueueReferencedObjects(ClrHeap heap, Queue<ObjRef> queue, HashSet<ulong> seenObjs)
		{
			if (!Type.IsPrimitive && !Type.IsArray)
			{
				GetFieldReferences(heap, queue, seenObjs);
			}
			else if (Type.IsArray)
			{
				GetArrayReferences(heap, queue, seenObjs);
			}
		}

		private static void Enqueue(ulong addr, ClrHeap heap, ClrType possibleType, Queue<ObjRef> queue, HashSet<ulong> seenObjs)
		{
			if (addr != 0L && !seenObjs.Contains(addr))
			{
				ClrType val = ((possibleType != null && possibleType.IsSealed) ? possibleType : null);
				if (val == null)
				{
					val = ExtensionMethods.GetObjectTypeSafe(heap, addr);
				}
				if (val == null)
				{
					throw new InvalidOperationException("Reading invalid pointer");
				}
				queue.Enqueue(new ObjRef(addr, val, interior: false));
			}
		}

		private static ulong Dereference(ulong addr, ClrHeap heap)
		{
			if (heap.GetRuntime().ReadPointer(addr, ref addr))
			{
				return addr;
			}
			return 0uL;
		}

		public override string ToString()
		{
			return $"[{Address:x16} {Type.Name}]";
		}
	}

	public class GCRootWalker
	{
		private const int MAX_OBJ_VISITS = 100;

		private const int MAX_ROOT_CHAIN_DEPTH = 100;

		private static Dictionary<ulong, int> visitCounts = new Dictionary<ulong, int>();

		private static CancellationToken cancellationToken;

		public static bool gcRootWarning = false;

		private static string gcRootChainBoxClassName = "gcRootChainBox firstGcRootChainBox";

		private static Dictionary<string, Dictionary<string, ClrRootChainSample>> FindRootChains(ClrHeap heap, HashSet<string> typesOfInterest, Action<int> preEnumerate, Action<ClrRoot, ClrRoot> onRootVisit)
		{
			Dictionary<string, Dictionary<string, ClrRootChainSample>> dictionary = new Dictionary<string, Dictionary<string, ClrRootChainSample>>();
			foreach (string item in typesOfInterest)
			{
				dictionary[item] = new Dictionary<string, ClrRootChainSample>();
			}
			IEnumerable<ClrRoot> enumerable = heap.EnumerateRoots();
			int obj = enumerable.Count();
			preEnumerate?.Invoke(obj);
			HashSet<ulong> allObjects = new HashSet<ulong>();
			foreach (ClrRoot item2 in enumerable)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				onRootVisit?.Invoke(item2, null);
				ClrRootChain rootChain = new ClrRootChain(item2, heap);
				Dictionary<string, Dictionary<string, ClrRootChainSample>> newRootChainsByRootedType = FindRootChains(heap, typesOfInterest, rootChain, allObjects, onRootVisit);
				MergeRootChains(dictionary, newRootChainsByRootedType);
			}
			return dictionary;
		}

		private static void MergeRootChains(Dictionary<string, Dictionary<string, ClrRootChainSample>> existingRootChainsByRootedType, Dictionary<string, Dictionary<string, ClrRootChainSample>> newRootChainsByRootedType)
		{
			foreach (KeyValuePair<string, Dictionary<string, ClrRootChainSample>> item in newRootChainsByRootedType)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				string key = item.Key;
				Dictionary<string, ClrRootChainSample> value = item.Value;
				Dictionary<string, ClrRootChainSample> value2 = null;
				if (!existingRootChainsByRootedType.TryGetValue(key, out value2))
				{
					value2 = (existingRootChainsByRootedType[key] = new Dictionary<string, ClrRootChainSample>());
				}
				foreach (KeyValuePair<string, ClrRootChainSample> item2 in value)
				{
					string key2 = item2.Key;
					ClrRootChainSample value3 = item2.Value;
					ClrRootChainSample value4 = null;
					if (!value2.TryGetValue(key2, out value4))
					{
						value2[key2] = value3;
					}
					else
					{
						value4.Add(value3);
					}
				}
			}
		}

		private static Dictionary<string, Dictionary<string, ClrRootChainSample>> FindRootChains(ClrHeap heap, HashSet<string> typesOfInterest, ClrRootChain rootChain, HashSet<ulong> allObjects, Action<ClrRoot, ClrRoot> onRootVisit)
		{
			Dictionary<string, Dictionary<string, ClrRootChainSample>> rootChainsByRootedType = new Dictionary<string, Dictionary<string, ClrRootChainSample>>();
			ulong num = rootChain.Peek();
			if (allObjects.Contains(num))
			{
				return rootChainsByRootedType;
			}
			allObjects.Add(num);
			ClrType objectTypeSafe = ExtensionMethods.GetObjectTypeSafe(heap, num);
			if (objectTypeSafe != null)
			{
				string name = objectTypeSafe.Name;
				if (typesOfInterest.Contains(name))
				{
					Dictionary<string, ClrRootChainSample> value = null;
					if (!rootChainsByRootedType.TryGetValue(name, out value))
					{
						value = new Dictionary<string, ClrRootChainSample>();
						rootChainsByRootedType[name] = value;
					}
					ClrRootChainSample value2 = null;
					string chainKey = rootChain.ChainKey;
					if (!value.TryGetValue(chainKey, out value2))
					{
						value[chainKey] = new ClrRootChainSample
						{
							NumOccurrences = 1,
							RootChain = new ClrRootChain(rootChain),
							TotalSize = rootChain.RootedObjectSize
						};
					}
					else
					{
						value2.NumOccurrences++;
					}
				}
				Action<ulong, int> action = delegate(ulong child, int offset)
				{
					if (!cancellationToken.IsCancellationRequested && rootChain.Count < 100)
					{
						int value3 = 0;
						if (visitCounts.TryGetValue(child, out value3))
						{
							if (value3 >= 100)
							{
								return;
							}
							visitCounts[child] = value3 + 1;
						}
						else
						{
							visitCounts[child] = 1;
						}
						rootChain.Push(child, heap);
						Dictionary<string, Dictionary<string, ClrRootChainSample>> newRootChainsByRootedType = FindRootChains(heap, typesOfInterest, rootChain, allObjects, onRootVisit);
						MergeRootChains(rootChainsByRootedType, newRootChainsByRootedType);
						rootChain.Pop();
					}
				};
				objectTypeSafe.EnumerateRefsOfObject(num, action);
			}
			TrimSubsets(rootChainsByRootedType);
			return rootChainsByRootedType;
		}

		private static void TrimSubsets(Dictionary<string, Dictionary<string, ClrRootChainSample>> rootChainsByRootedType)
		{
		}

		public static void ShowRoots(NetScriptManager manager, NetDbgObj debugger, NetProgress progress, IEnumerable<HeapStatsEntry> top40Query)
		{
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Expected O, but got Unknown
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Expected O, but got Unknown
			//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Expected O, but got Unknown
			//IL_0529: Unknown result type (might be due to invalid IL or missing references)
			//IL_0530: Expected O, but got Unknown
			_ = debugger.ClrHeap;
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("GCROOTCHAINS", (SectionType)0);
			val.Title = "GC Root Chains";
			Globals.Manager.CurrentSection = val;
			new GCRootWalker();
			HashSet<string> typesOfInterest = GetTypesOfInterest(top40Query);
			if (!int.TryParse(ConfigHelper.GetSetting("GCRootTimeout"), out var timeout))
			{
				timeout = 120;
			}
			if (!int.TryParse(ConfigHelper.GetSetting("GCRootSampleCount"), out var result))
			{
				result = 10;
			}
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			Dictionary<ulong, ClrRootNode> allRoots = new Dictionary<ulong, ClrRootNode>();
			int rootNum = 0;
			int headRootCount = 0;
			DateTime startTime = DateTime.Now;
			Timer timer = null;
			Dictionary<string, Dictionary<string, ClrRootChainSample>> dictionary = FindRootChains(debugger.ClrHeap, typesOfInterest, delegate(int theHeadRootCount)
			{
				headRootCount = theHeadRootCount;
				progress.CurrentPosition = 0;
				progress.SetCurrentRange(0, theHeadRootCount);
			}, delegate(ClrRoot root, ClrRoot parentRoot)
			{
				int secondsRemaining = timeout - (int)((DateTime.Now - startTime).Ticks / 10000 / 1000);
				IncrementProgress(progress, ++rootNum, headRootCount, timeout, secondsRemaining);
				ulong address = root.Address;
				if (!allRoots.ContainsKey(address))
				{
					allRoots.Add(address, new ClrRootNode(root, parentRoot));
					if (parentRoot == null)
					{
						timer = new Timer(delegate
						{
							secondsRemaining = timeout - (int)((DateTime.Now - startTime).Ticks / 10000 / 1000);
							SetProgressStatus(progress, rootNum, headRootCount, timeout, secondsRemaining);
							if (secondsRemaining <= 0)
							{
								cancellationTokenSource.Cancel();
								timer.Dispose();
							}
						}, null, 1000, 1000);
					}
				}
			});
			if (dictionary.Any((KeyValuePair<string, Dictionary<string, ClrRootChainSample>> kvp) => kvp.Value.Count > 0))
			{
				new Dictionary<string, long>();
				new Dictionary<string, long>();
				new Dictionary<string, long>();
				HTMLTable val2 = new HTMLTable();
				val2.AddColumns(new object[3] { "Type Name", "Number of Roots Found", "Total Size" });
				val2.ToggleRowBackgrounds = true;
				HTMLTable val3 = new HTMLTable();
				val3.ClassName += " summaryTableClass";
				val3.InferNameValueStyle = false;
				val3.ToggleRowBackgrounds = false;
				manager.WriteLine();
				foreach (KeyValuePair<string, Dictionary<string, ClrRootChainSample>> item in dictionary)
				{
					string rootedTypeName = item.Key;
					Dictionary<string, ClrRootChainSample> source = dictionary[rootedTypeName];
					int num = source.Sum((KeyValuePair<string, ClrRootChainSample> rootChainSamplesByChainKeyKVP) => rootChainSamplesByChainKeyKVP.Value.NumOccurrences);
					if (num == 0)
					{
						continue;
					}
					long num2 = source.Sum((KeyValuePair<string, ClrRootChainSample> kvp2) => (long)kvp2.Value.TotalSize);
					if (num2 > 104857600)
					{
						gcRootWarning = true;
						gcRootChainBoxClassName = "gcRootChainBox firstGcRootChainBox";
						val3.AddRow(new object[2]
						{
							rootedTypeName,
							Globals.HelperFunctions.PrintMemory(num2)
						});
					}
					else
					{
						gcRootChainBoxClassName = "gcRootChainBox";
					}
					val2.AddRow(new object[3]
					{
						rootedTypeName,
						num,
						Globals.HelperFunctions.PrintMemory(num2)
					});
					string text = CleanRootedTypeName(rootedTypeName);
					ReportSection val4 = manager.AddReportSection(text, (SectionType)0);
					val4.Title = rootedTypeName;
					manager.CurrentSection = val4;
					ClrRootChainSample clrRootChainSample = item.Value.Values.Take(1).FirstOrDefault();
					if (clrRootChainSample == null)
					{
						continue;
					}
					ClrType heapType = clrRootChainSample.RootChain.GetRootedObject().GetHeapType();
					HeapStatsEntry heapStatsEntry = top40Query.FirstOrDefault((HeapStatsEntry hse) => hse.TypeName == rootedTypeName);
					if (heapStatsEntry.TypeName == null)
					{
						heapStatsEntry = (from obj in Globals.g_Debugger.EnumerateHeapObjects()
							let type = obj.GetHeapType()
							where type != null && type.Name == rootedTypeName
							group obj by type into g
							select new HeapStatsEntry(g)).FirstOrDefault();
					}
					HTMLTable val5 = new HTMLTable();
					val5.AddRow(new object[2] { "Type Name", rootedTypeName });
					val5.AddRow(new object[2]
					{
						"Module Path",
						heapType.Module.FileName
					});
					val5.AddRow(new object[2]
					{
						"Total size of all instances",
						Globals.HelperFunctions.PrintMemory(heapStatsEntry.TotalSize)
					});
					val5.AddRow(new object[2] { "Number of instances", heapStatsEntry.NumObjects });
					val5.AddRow(new object[2]
					{
						"Total size of all <b>rooted</b> instances",
						Globals.HelperFunctions.PrintMemory(num2)
					});
					val5.AddRow(new object[2] { "Number of <b>rooted</b> instances", num });
					val4.WriteLine(((object)val5).ToString());
					IEnumerable<KeyValuePair<string, ClrRootChainSample>> enumerable = source.OrderByDescending((KeyValuePair<string, ClrRootChainSample> kvp2) => kvp2.Value.TotalSize).Take(result);
					val4.Write($"<br><font size='+1'>Top GC Root Chains for {rootedTypeName}:</font><br><br><br><div class='ml35'>");
					foreach (KeyValuePair<string, ClrRootChainSample> item2 in enumerable)
					{
						ClrRootChainSample value = item2.Value;
						val5 = new HTMLTable();
						val5.ToggleRowBackgrounds = false;
						val5.AddRow(new object[2]
						{
							"Total size of all objects with this GC Root Chain",
							Globals.HelperFunctions.PrintMemory(value.TotalSize)
						});
						val5.AddRow(new object[2] { "Number of objects with this GC Root Chain", value.NumOccurrences });
						val5.AddRow(new object[2]
						{
							"Address of 1st object found with this GC Root Chain path",
							$"0x{value.RootChain.Peek():x}"
						});
						val4.WriteLine(((object)val5).ToString());
						string empty = string.Empty;
						int num3 = 0;
						string[] rootLabels = value.RootChain.RootLabels;
						val4.Write("<div class='" + gcRootChainBoxClassName + "'>");
						gcRootChainBoxClassName = "gcRootChainBox";
						string[] array = rootLabels;
						foreach (string arg in array)
						{
							num3++;
							val4.Write(string.Format("{0}{1}{2}<br>", empty, arg, (num3 == rootLabels.Length) ? "" : "-->"));
						}
						val4.Write("</div>");
					}
					val4.Write("</div>");
					manager.CurrentSection = val4.Parent;
				}
				string text2 = string.Empty;
				if (cancellationTokenSource.IsCancellationRequested)
				{
					text2 = $"<div class='summaryItemCallout'><i><b>Note:</b>  GC Root Chain detection is incomplete due to a timeout after {timeout} seconds</i>.  The <b>GCRootTimeout</b> and other settings can be modified in the DebugDiag.AnalysisRules.dll.config file.<br><br></div>";
				}
				string text3 = "<b>GC Root Chains</b> are available for the following types:<br><br>" + ((object)val2).ToString() + text2;
				manager.WriteLine(text3);
				if (gcRootWarning)
				{
					string text4 = "One or more <b>GC Root Chains</b> are rooting a large amount of managed objects<br><br><div class='ml35' style='display: inline-block;color:black'>" + ((object)val3).ToString() + "</div>";
					string text5 = "Review the <a href='#GCROOTCHAINS" + Globals.g_UniqueReference + "'>GC Root Chains</a> report to determine why these objects are rooted.";
					Globals.Manager.ReportWarning(text4, text5, 9999, "{74759e10-bed8-430a-9574-41633557df38}");
				}
			}
			timer.Dispose();
			Globals.Manager.CurrentSection = val.Parent;
		}

		private static string CleanRootedTypeName(string rootedTypeName)
		{
			return rootedTypeName.Replace("[]", "_Array").Replace("<", "_GenericOpen").Replace(">", "_GenericClose");
		}

		private static void IncrementProgress(NetProgress progress, int rootNum, int headRootCount, int timeout, int secondsRemaining)
		{
			progress.CurrentPosition = rootNum;
			SetProgressStatus(progress, rootNum, headRootCount, timeout, secondsRemaining);
		}

		private static void SetProgressStatus(NetProgress progress, int rootNum, int headRootCount, int timeout, int secondsRemaining)
		{
			if (secondsRemaining > 0)
			{
				progress.CurrentStatus = $"Processing GC Root {rootNum} of {headRootCount}.  (timeout in {secondsRemaining} seconds)";
			}
			else
			{
				progress.CurrentStatus = string.Format("Processing GC Root {0} of {1}.  (Canceling due to timeout...)", rootNum, headRootCount, secondsRemaining);
			}
		}
	}

	public class ClrRootNode
	{
		public ulong Address;

		public ulong Object;

		public ClrRoot Parent;

		public ClrRootNode(ClrRoot root, ClrRoot parentRoot)
		{
			Address = root.Address;
			Object = root.Object;
			Parent = parentRoot;
		}
	}

	public class ClrRootChain
	{
		private Stack<ulong> references;

		private string chainKey;

		private string rootedTypeName;

		private ClrHeap heap;

		public ulong RootedObjectSize
		{
			get
			{
				ulong num = references.Peek();
				ClrType objectTypeSafe = ExtensionMethods.GetObjectTypeSafe(heap, num);
				if (objectTypeSafe == null)
				{
					return 0uL;
				}
				return objectTypeSafe.GetSize(num);
			}
		}

		public string ChainKey => chainKey;

		public string[] RootLabels => chainKey.Split('|');

		public int Count => references.Count;

		public ClrObject GetRootedObject()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected O, but got Unknown
			return new ClrObject(heap, (ClrType)null, references.Peek());
		}

		public ClrRootChain(ClrRootChain copy)
		{
			chainKey = copy.chainKey;
			rootedTypeName = copy.rootedTypeName;
			references = new Stack<ulong>(copy.references.Count);
			foreach (ulong item in copy.references.Reverse())
			{
				references.Push(item);
			}
			heap = copy.heap;
		}

		public ClrRootChain(ClrRoot root, ClrHeap heap)
		{
			this.heap = heap;
			references = new Stack<ulong>();
			references.Push(root.Address);
			references.Push(root.Object);
			chainKey = root.Name;
			ClrType objectTypeSafe = ExtensionMethods.GetObjectTypeSafe(heap, root.Object);
			if (objectTypeSafe != null)
			{
				chainKey = chainKey + "|" + objectTypeSafe.Name;
			}
		}

		public void Push(ulong address, ClrHeap heap)
		{
			references.Push(address);
			ClrType objectTypeSafe = ExtensionMethods.GetObjectTypeSafe(heap, address);
			if (objectTypeSafe != null)
			{
				chainKey = chainKey + "|" + objectTypeSafe.Name;
			}
		}

		public ulong Pop()
		{
			ulong result = references.Pop();
			int num = chainKey.LastIndexOf('|');
			if (num > -1)
			{
				chainKey = chainKey.Substring(0, num);
			}
			return result;
		}

		public ulong Peek()
		{
			return references.Peek();
		}
	}

	public class ClrRootChainSample
	{
		public int NumOccurrences;

		public ulong TotalSize;

		public ClrRootChain RootChain;

		internal void Add(ClrRootChainSample newRootChainSample)
		{
			NumOccurrences += newRootChainSample.NumOccurrences;
			ulong num = newRootChainSample.TotalSize;
			if (num == 0L)
			{
				num = newRootChainSample.RootChain.RootedObjectSize;
			}
			TotalSize += num;
		}
	}

	private const string GC_DATA_STRUCTURES_INVALID = "GC_DATA_STRUCTURES_INVALID";

	private bool g_GcAlreadyRunningWarningReported = true;

	private AnalysisModes _analysisMode;

	public string Category => "Memory Pressure Analyzers";

	public string Description => "Managed Memory Analysis";

	public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
	{
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		Globals.Manager = manager;
		Globals.g_Progress = progress;
		Globals.g_Progress.OverallStatus = "";
		Globals.g_Progress.CurrentStatus = "";
		Globals.g_Progress.CurrentPosition = 0;
		Globals.g_OverallProgress = 0;
		Globals.g_StepCount = 3;
		List<string> dumpFiles = Globals.Manager.GetDumpFiles();
		Globals.g_Progress.OverallStatus = "Building the table of contents";
		Globals.g_Progress.SetOverallRange(0, dumpFiles.Count * (Globals.g_StepCount + 1));
		foreach (string item in dumpFiles)
		{
			Globals.g_DataFile = item;
			if (Globals.g_DataFile.Substring(Globals.g_DataFile.GetSafeLength() - 4).ToUpper() == ".DMP")
			{
				Globals.g_ShortDumpFileName = Globals.g_DataFile.Substring(Globals.g_DataFile.LastIndexOf("\\") + 1);
				Globals.HelperFunctions.ResetStatusNoIncrement("Analyzing " + Globals.g_ShortDumpFileName);
				Globals.g_Debugger = Globals.Manager.GetDebugger(Globals.g_DataFile);
				if (Globals.g_Debugger != null)
				{
					NetDbgObj g_Debugger = Globals.g_Debugger;
					try
					{
						string filterReason = null;
						if (!ShouldRunAnalysis(Globals.g_Debugger, _analysisMode, ref filterReason))
						{
							continue;
						}
						CacheFunctions.ResetCache();
						Globals.HelperFunctions.SetOSVersion();
						_ = Globals.Manager.GetResults(0).Count;
						ReportSection val = Globals.Manager.AddReportSection(Globals.g_ShortDumpFileName, (SectionType)1);
						val.Title = "CLR Memory Analysis for " + Globals.g_ShortDumpFileName;
						Globals.Manager.CurrentSection = val;
						Globals.g_UniqueReference = val.GetUID;
						if (Globals.g_Debugger.DumpType != "MINIDUMP")
						{
							Globals.HelperFunctions.GenerateReportHeader(Globals.g_DataFile, ".Net Memory Pressure Analysis");
							Globals.AnalyzeManaged.InitClrGlobals();
							InitMemoryAnalysisGlobals();
							Globals.AnalyzeManaged.LoadCLRInformation();
							if (Globals.AnalyzeManaged.IsClrExtensionExecuting())
							{
								DoDotNetMemoryAnalysis();
							}
						}
						Globals.Manager.CurrentSection = val.Parent;
						goto IL_0222;
					}
					finally
					{
						((IDisposable)g_Debugger)?.Dispose();
					}
				}
				for (int i = 0; i < Globals.g_StepCount; i++)
				{
					Globals.HelperFunctions.UpdateOverallProgress();
				}
				goto IL_0222;
			}
			for (int j = 0; j < Globals.g_StepCount; j++)
			{
				Globals.HelperFunctions.UpdateOverallProgress();
			}
			continue;
			IL_0222:
			Globals.g_Debugger = null;
		}
	}

	public void DoDotNetMemoryAnalysis()
	{
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting information about .NET object roots");
		IEnumerable<HeapStatsEntry> top40Query = GetTop40Query();
		PrintGCRoots(top40Query);
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting GC Heap information");
		PrintGCHeapInformation();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting information about .NET objects");
		Print40LargestObjects(top40Query);
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting Finalizequeue information");
		PrintFinalizequeue();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting LOH information");
		PrintLOHObjects();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Determining the size of the cache, this might take a while");
		PrintCacheSize();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Extracting cache contents, this might take a while");
		PrintCacheContents();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting Session statistics");
		PrintSessionStatistics();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Checking if the process is in the middle of a GC");
		CheckIfInGC();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Checking the size of DataTables in the memory");
		DumpDataTables();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Printing information about various GC Handles");
		PrintGCHandles();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Getting Dynamic Assembly Information");
		DumpDynamicAssemblies();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Finding all runtimes which have debug set to true");
		Globals.AnalyzeManaged.FindDebugTrue();
		Globals.HelperFunctions.UpdateOverallProgress();
		Globals.HelperFunctions.ResetStatusNoIncrement("Dumping out the application domain information");
		DumpDomainStat();
		Globals.HelperFunctions.UpdateOverallProgress();
	}

	private IEnumerable<HeapStatsEntry> GetTop40Query()
	{
		return (from obj in Globals.g_Debugger.EnumerateHeapObjects()
			let type = obj.GetHeapType()
			where type != null
			group obj by type into g
			select new HeapStatsEntry(g) into e
			orderby e.TotalSize descending
			select e).Take(40);
	}

	private void PrintGCRoots(IEnumerable<HeapStatsEntry> top40Query)
	{
		GCRootWalker.ShowRoots(Globals.Manager, Globals.g_Debugger, Globals.g_Progress, top40Query);
	}

	public void PrintGCHandles()
	{
	}

	public void PrintArray(string[] str)
	{
		Globals.Manager.Write("<pre>");
		foreach (string text in str)
		{
			Globals.Manager.WriteLine(text);
		}
		Globals.Manager.Write("</pre>");
	}

	public double BangObjSize(string obj)
	{
		return ObjSizeHelper.ObjSize(Globals.g_Debugger.ClrRuntime, (ulong)Globals.HelperFunctions.FromHex(obj));
	}

	public void InitMemoryAnalysisGlobals()
	{
		g_GcAlreadyRunningWarningReported = false;
	}

	public void PrintGCHeapInformation()
	{
		try
		{
			ClrRuntime clrRuntime = Globals.g_Debugger.ClrRuntime;
			ClrHeap clrHeap = Globals.g_Debugger.ClrHeap;
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("GCHEAPINFO", (SectionType)0);
			val.Title = ".NET GC Heap Information";
			Globals.Manager.CurrentSection = val;
			Globals.Manager.Write("<table cellpadding=0 cellspacing=0 border=0 class=myCustomText ID='TblInfo'>");
			Globals.Manager.Write("<tr><td colspan='2'>Number of GC Heaps: " + clrRuntime.HeapCount + "</td>");
			foreach (IGrouping<int, ClrSegment> item in from s in clrHeap.Segments
				group s by s.ProcessorAffinity into byCpu
				orderby byCpu.Key
				select byCpu)
			{
				ulong num = 0uL;
				foreach (ClrSegment item2 in item)
				{
					num += item2.Length;
				}
				Globals.Manager.Write(string.Format("<tr><td>Heap Size</td><td>0x{0:x} ({0:n0})</td></tr>", num));
			}
			Globals.Manager.Write("<tr><td colspan='2'><hr/></td></tr>");
			string text = Globals.HelperFunctions.PrintMemory(clrHeap.TotalHeapSize);
			Globals.Manager.Write("<tr><td>GC Heap Size</td><td> <b>" + text + " </b></td></tr>");
			string text2 = "<B>GC Heap usage:</B> " + text;
			if (!GCRootWalker.gcRootWarning)
			{
				string text3 = "Review these reports to understand which objects are consuming the most memory:<div style='margin-left:20px'><UL><LI><a href='#GCROOTCHAINS" + Globals.g_UniqueReference + "'>GC Root Chains</a></LI><LI><a href='#40MOSTMEMORYCONUMERS" + Globals.g_UniqueReference + "'>Most Memory-Consuming Objects</a><br></LI><LI><a href='#LOH" + Globals.g_UniqueReference + "'>Large .NET Objects</a></LI></UL>";
				if (Globals.g_webCache)
				{
					text3 = text3 + "<a href='#WEBCACHE" + Globals.g_UniqueReference + "'>HttpRuntime Caches &amp; Sessions</a>";
				}
				if (Globals.g_haveObjectsReadyForFinalization)
				{
					text3 = text3 + "<a href='#FINALIZEQUEUE" + Globals.g_UniqueReference + "'>Top Objects in the Finalizer queue (ready for finalization)</a>";
				}
				text3 += "</div>";
				if (clrHeap.TotalHeapSize > 524288000)
				{
					Globals.Manager.ReportWarning(text2, text3, 0, "{74759e10-bed8-430a-9574-41633557df36}");
				}
				else
				{
					Globals.Manager.ReportInformation(text2 + "<BR><BR>" + text3, 0, "{74759e10-bed8-430a-9574-41633557df37}");
				}
			}
			ulong num2 = 0uL;
			ulong num3 = 1048576uL;
			foreach (ClrSegment segment in clrHeap.Segments)
			{
				num2 += segment.CommittedEnd - (segment.Start & 0xFFFFFFFFFFFFFF00uL);
			}
			Globals.Manager.Write("<tr><td>Total Commit Size</td><td><b> " + num2 / num3 + " MB</b></td></tr>");
			ulong num4 = 0uL;
			foreach (ClrSegment segment2 in clrHeap.Segments)
			{
				num4 += segment2.ReservedEnd - segment2.CommittedEnd;
			}
			Globals.Manager.Write("<tr><td>Total Reserved Size&nbsp;&nbsp;</td><td><b> " + num4 / num3 + " MB</b></td></tr>");
			Globals.Manager.Write("</table>");
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "PrintGCHeapInformation");
		}
	}

	public void Print40LargestObjects(IEnumerable<HeapStatsEntry> query)
	{
		try
		{
			BarGraph barGraph = new BarGraph();
			barGraph.SetRowCount(40);
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("40MOSTMEMORYCONUMERS", (SectionType)0);
			val.Title = "40 most memory consuming .NET object types";
			Globals.Manager.CurrentSection = val;
			int num = 0;
			Dictionary<string, HeapStatsEntry> dictionary = new Dictionary<string, HeapStatsEntry>();
			foreach (HeapStatsEntry item in query)
			{
				string typeName = item.TypeName;
				long totalSize = item.TotalSize;
				int numObjects = item.NumObjects;
				dictionary[typeName] = item;
				typeName = HttpUtility.HtmlEncode(typeName);
				string caption = ((typeName.IndexOf("System.Web.UI") < 0) ? ((typeName.IndexOf("System.Data") < 0) ? ((typeName.IndexOf("System.Xml") < 0) ? ((typeName.IndexOf("System") >= 0 || typeName.IndexOf("Microsoft") >= 0 || typeName.IndexOf("Free") >= 0) ? typeName : ("<font color=purple>" + typeName + "</font>")) : ("<font color=green>" + typeName + "</font>")) : ("<font color=blue>" + typeName + "</font>")) : ("<font color=red>" + typeName + "</font>"));
				barGraph.Rows[num].Value = totalSize;
				barGraph.Rows[num].Caption = caption;
				barGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(totalSize) + "&nbsp;&nbsp;&nbsp;&nbsp;(" + numObjects + " objects )";
				num++;
			}
			barGraph.DrawGraph();
			Globals.Manager.Write("<BR/> <BR/>");
			Globals.Manager.Write("<Table><TR><TD><b>Color</B></TD><TD><B>Object type</B></TD></TR>");
			Globals.Manager.Write("<TR><TD><font color=red>Red</font></TD><TD>System.Web.UI... objects</TD></TR>");
			Globals.Manager.Write("<TR><TD><font color=blue>Blue</font></TD><TD>System.Data... objects</TD></TR>");
			Globals.Manager.Write("<TR><TD><font color=green>Green</font></TD><TD>System.XML... objects</TD></TR>");
			Globals.Manager.Write("<TR><TD><font color=purple>Purple</font></TD><TD>Custom objects</TD></TR>");
			Globals.Manager.Write("</Table>");
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "Print40LargestObjects");
		}
	}

	private static HashSet<string> GetTypesOfInterest(IEnumerable<HeapStatsEntry> query)
	{
		new Dictionary<string, HeapStatsEntry>();
		if (!int.TryParse(ConfigHelper.GetSetting("GCRootTypesCount"), out var result))
		{
			result = 5;
		}
		string text = ConfigHelper.GetSetting("GCRootTypesToAdd") ?? "";
		string text2 = ConfigHelper.GetSetting("GCRootTypesToIgnore") ?? "";
		IEnumerable<string> enumerable = null;
		if (text2 == "*")
		{
			enumerable = text.Split(';');
		}
		else
		{
			if (!string.IsNullOrEmpty(text2))
			{
				string[] ignoreList = text2.Split(';');
				enumerable = from a in query.Where((HeapStatsEntry a) => !ignoreList.Contains(a.TypeName)).Take(result)
					select a.TypeName;
			}
			else
			{
				enumerable = from a in query.Take(result)
					select a.TypeName;
			}
			enumerable = enumerable.Concat(text.Split(';'));
		}
		return new HashSet<string>(enumerable);
	}

	public void PrintFinalizequeue()
	{
		try
		{
			if (!Globals.g_Debugger.ClrHeap.CanWalkHeap)
			{
				return;
			}
			ReportSection currentSection = Globals.Manager.CurrentSection;
			PrintFinalizableObjects(liveObjects: true);
			Globals.Manager.CurrentSection = currentSection;
			int num = PrintFinalizableObjects(liveObjects: false);
			if (num > 0)
			{
				ClrThread val = Globals.g_Debugger.ClrRuntime.Threads.Where((ClrThread t) => t.IsFinalizer).FirstOrDefault();
				if (val != null)
				{
					NetDbgThread threadBySystemID = Globals.g_Debugger.GetThreadBySystemID((int)val.OSThreadId);
					if (threadBySystemID != null && ((List<NetDbgStackFrame>)(object)threadBySystemID.ManagedStackFrames).Count > 0)
					{
						string text = $"There are <B>{num}</B> objects ready for finalization: ";
						Globals.Manager.ReportWarning(text, "This is an indication that the finalizer thread may be blocked. Look at <a href='#FINALIZEQUEUE" + Globals.g_UniqueReference + "'>finalizequeue info and finalizer stack</a> to determine why/if the finalizer is blocked", 0, "{82c84b54-30b9-40ee-96b9-549bb8c1ab14}");
						Globals.Manager.WriteLine("<BR>  " + text + ".  This is an indication that the finalizer thread may be blocked.  Check the call stack of the finalizer thread below to see if it is blocked or active.");
						Globals.Manager.WriteLine("");
						Globals.Manager.WriteLine("<B>Finalizer Thread</B>");
						foreach (NetDbgStackFrame item in (List<NetDbgStackFrame>)(object)threadBySystemID.StackFrames)
						{
							Globals.Manager.WriteLine(item.GetFrameText(true, true));
						}
					}
				}
			}
			Globals.Manager.CurrentSection = currentSection;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "PrintFinalizequeue");
		}
	}

	private static int PrintFinalizableObjects(bool liveObjects)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		IEnumerable<ulong> source;
		ReportSection val;
		string text;
		if (liveObjects)
		{
			source = ((Globals.g_Debugger.ClrVersionInfo.Version.Major < 4) ? Globals.g_Debugger.ClrHeap.EnumerateFinalizableObjects() : Globals.g_Debugger.ClrRuntime.EnumerateFinalizerQueue());
			val = Globals.Manager.CurrentSection.AddChildSection("FINALIZABLE", (SectionType)0);
			val.Title = "Top Finalizable Objects on the heap (live objects)";
			text = "There are no live finalizable objects";
		}
		else
		{
			source = ((Globals.g_Debugger.ClrVersionInfo.Version.Major < 4) ? Globals.g_Debugger.ClrRuntime.EnumerateFinalizerQueue() : Globals.g_Debugger.ClrHeap.EnumerateFinalizableObjects());
			val = Globals.Manager.CurrentSection.AddChildSection("FINALIZEQUEUE", (SectionType)0);
			val.Title = "Top Objects in the Finalizer queue (ready for finalization)";
			text = "There are no objects in the finalizer queue";
		}
		if (!source.Any())
		{
			val.WriteLine(text);
			return 0;
		}
		Globals.g_haveObjectsReadyForFinalization = true;
		Globals.Manager.CurrentSection = val;
		var list = (from obj in source.Select((Func<ulong, ClrObject>)((ulong addr) => new ClrObject(Globals.g_Debugger.ClrHeap, ExtensionMethods.GetObjectTypeSafe(Globals.g_Debugger.ClrHeap, addr), addr)))
			let type = obj.GetHeapType()
			where type != null
			group obj by type into g
			select new
			{
				TypeName = g.Key.Name,
				NumObjects = g.Count(),
				TotalSize = g.Sum((ClrObject o) => (long)g.Key.GetSize(o.GetValue()))
			} into e
			orderby e.TotalSize descending
			select e).Take(40).ToList();
		BarGraph barGraph = new BarGraph();
		barGraph.SetRowCount(Math.Min(40, list.Count));
		int num = 0;
		foreach (var item in list)
		{
			string typeName = item.TypeName;
			typeName = HttpUtility.HtmlEncode(typeName);
			long totalSize = item.TotalSize;
			int numObjects = item.NumObjects;
			string caption = ((typeName.IndexOf("System.Web.UI") < 0) ? ((typeName.IndexOf("System.Data") < 0) ? ((typeName.IndexOf("System.Xml") < 0) ? ((typeName.IndexOf("System") >= 0 || typeName.IndexOf("Microsoft") >= 0 || typeName.IndexOf("Free") >= 0) ? typeName : ("<font color=purple>" + typeName + "</font>")) : ("<font color=green>" + typeName + "</font>")) : ("<font color=blue>" + typeName + "</font>")) : ("<font color=red>" + typeName + "</font>"));
			barGraph.Rows[num].Value = totalSize;
			barGraph.Rows[num].Caption = caption;
			barGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(totalSize) + "&nbsp;&nbsp;&nbsp;&nbsp;(" + numObjects + " objects )";
			num++;
		}
		barGraph.DrawGraph();
		return source.Count();
	}

	public void PrintLOHObjects()
	{
		try
		{
			if (!int.TryParse(ConfigHelper.GetSetting("MaxHeapObjectTypes"), out var result))
			{
				result = 100;
			}
			if (!Globals.g_Debugger.ClrHeap.CanWalkHeap)
			{
				return;
			}
			var source = from obj in Globals.g_Debugger.EnumerateHeapObjects()
				let type = obj.GetHeapType()
				where type != null
				where type.GetSize(obj.GetValue()) > 85000
				group obj by type into g
				select new
				{
					TypeName = g.Key.Name,
					NumObjects = g.Count(),
					TotalSize = g.Sum((ClrObject o) => (long)g.Key.GetSize(o.GetValue()))
				};
			var list = source.OrderByDescending(e => e.TotalSize).Take(result).ToList();
			BarGraph barGraph = new BarGraph();
			barGraph.SetRowCount(Math.Min(result, list.Count));
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("LOH", (SectionType)0);
			val.Title = $"Top {result} Objects on the Large Object Heap";
			if (!list.Any())
			{
				val.WriteLine("There are no objects on the Large Object Heap");
				return;
			}
			Globals.Manager.CurrentSection = val;
			int num = 0;
			foreach (var item in list)
			{
				string typeName = item.TypeName;
				typeName = HttpUtility.HtmlEncode(typeName);
				long totalSize = item.TotalSize;
				int numObjects = item.NumObjects;
				string caption = ((typeName.IndexOf("System.Web.UI") < 0) ? ((typeName.IndexOf("System.Data") < 0) ? ((typeName.IndexOf("System.Xml") < 0) ? ((typeName.IndexOf("System") >= 0 || typeName.IndexOf("Microsoft") >= 0 || typeName.IndexOf("Free") >= 0) ? typeName : ("<font color=purple>" + typeName + "</font>")) : ("<font color=green>" + typeName + "</font>")) : ("<font color=blue>" + typeName + "</font>")) : ("<font color=red>" + typeName + "</font>"));
				barGraph.Rows[num].Value = totalSize;
				barGraph.Rows[num].Caption = caption;
				barGraph.Rows[num].Caption2 = Globals.HelperFunctions.PrintMemory(totalSize) + "&nbsp;&nbsp;&nbsp;&nbsp;(" + numObjects + " objects )";
				num++;
			}
			barGraph.DrawGraph();
			long num2 = source.Sum(item => item.TotalSize);
			if (num2 > 0)
			{
				string arg = Globals.HelperFunctions.PrintMemory(num2);
				Globals.Manager.Write("<BR/>");
				Globals.Manager.Write("<BlockQuote><font color=Maroon size=4><B>Total LOH Size:</B>&nbsp;&nbsp;");
				Globals.Manager.Write($"{arg}");
				Globals.Manager.Write("</font></BlockQuote>");
			}
			Globals.Manager.Write("<BR/> <BR/>");
			Globals.Manager.Write("<BlockQuote><B>More information:</B>");
			Globals.Manager.WriteLine("A high amount of large objects (strings and arrays over 85000 bytes) can lead to GC Heap framgmentation and thus higher memory usage in your application.");
			Globals.Manager.WriteLine("Look through the large objects, to dig deeper you can run !do on the object address in windbg, to see if these objects are expected and if you can minimize their usage in any way, by caching etc.");
			Globals.Manager.WriteLine("Common reasons for high amounts of large objects are <a href=http://blogs.msdn.com/tess/archive/2006/11/24/asp-net-case-study-bad-perf-high-memory-usage-and-high-cpu-in-gc-death-by-viewstate.aspx>large viewstate</a> and <a href=http://blogs.msdn.com/tess/archive/2008/09/02/outofmemoryexceptions-while-remoting-very-large-datasets.aspx>Dataset serialization</a>");
			Globals.Manager.Write("</BlockQuote>");
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "PrintLOHObjects");
		}
	}

	public void PrintCacheSize()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = null;
			string[] array = Globals.AnalyzeManaged.DumpHeapForType("System.Web.HttpRuntime");
			if (array.Length == 0)
			{
				return;
			}
			Globals.g_webCache = true;
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("WEBCACHE", (SectionType)0);
			val.Title = "HttpRuntime Caches &amp; Sessions";
			Globals.Manager.CurrentSection = val;
			Globals.Manager.Write("<table border=1 ><tr class=myCustomText><td>Runtime</td><td>Size</tr></th>");
			string[] array2 = array;
			foreach (string objectAddress in array2)
			{
				if (1 == Globals.g_Debugger.ClrVersionInfo.Version.Major)
				{
					string text2 = Globals.AnalyzeManaged.DumpObject(objectAddress, "_cache");
					text = ((!(Globals.NOT_FOUND != text2)) ? Globals.NOT_FOUND : Globals.AnalyzeManaged.DumpObject(text2, "_cachePublic"));
				}
				else
				{
					text = Globals.AnalyzeManaged.DumpObject(objectAddress, "_cachePublic");
				}
				if (Globals.NOT_FOUND != text)
				{
					double num = BangObjSize(text);
					string text3 = Globals.AnalyzeManaged.DumpString(objectAddress, "_appDomainAppPath");
					if (num > 50.0 * Math.Pow(1024.0, 2.0))
					{
						Globals.Manager.ReportWarning("The size of the ASP.NET cache for the Application domain " + text3 + "is " + Globals.HelperFunctions.PrintMemory(num), "If this is high, try to determine the objects stored in the cache and try to mininmize the lifetime of the objects stored in the cache", 0, "{f8f8c11b-a0da-4c20-acb0-44c12839c784}");
					}
					Globals.Manager.Write("<tr class=myCustomText><td>" + text3 + "</td><td>" + Globals.HelperFunctions.PrintMemory(num) + "</td></tr>");
				}
			}
			Globals.Manager.Write("</table>");
			Globals.Manager.Write("<BR/> <BR/>");
			Globals.Manager.Write("<BlockQuote><B>More information:</B><BR/>");
			Globals.Manager.WriteLine("There is one System.Web.Caching.Cache object referencing all cached objects, per web application");
			Globals.Manager.WriteLine("In-Proc session state is stored in the cache, so the size of all session vars is also included in the size of the cache for the specific application");
			Globals.Manager.WriteLine("");
			Globals.Manager.WriteLine("<b>Related articles</b>");
			Globals.Manager.WriteLine("<a href=http://blogs.msdn.com/tess/archive/2006/01/26/517819.aspx>How much are you caching</a>");
			Globals.Manager.WriteLine("<a href=http://blogs.msdn.com/tess/archive/2008/05/28/asp-net-memory-thou-shalt-not-store-ui-objects-in-cache-or-session-scope.aspx>UI objects in session scope</a>");
			Globals.Manager.Write("</BlockQuote>");
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "PrintCacheSize");
		}
	}

	public void PrintCacheContents()
	{
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Invalid comparison between Unknown and I4
		string[] array = Globals.AnalyzeManaged.DumpHeapForType("System.Web.Caching.CacheEntry");
		if (array.Length == 0)
		{
			return;
		}
		ReportSection val = Globals.Manager.CurrentSection.AddChildSection("WEBCACHECONTENTS", (SectionType)0);
		val.Title = "Entries stored in the Web Cache in the process";
		Globals.Manager.CurrentSection = val;
		Globals.Manager.Write("<table border=1 ><tr class=myCustomText><th>Key</th><th>Value</th></tr>");
		string[] array2 = array;
		foreach (string theHexStr in array2)
		{
			ulong num = (ulong)Globals.HelperFunctions.FromHex(theHexStr);
			dynamic dynamicObject = ClrMemDiagExtensions.GetDynamicObject(Globals.g_Debugger.ClrHeap, num);
			Globals.Manager.Write("<tr><td>");
			Globals.Manager.Write((string)dynamicObject._key);
			Globals.Manager.Write("</td><td>");
			dynamic val2 = dynamicObject._value;
			if (val2 == null || val2 is ClrNullValue)
			{
				Globals.Manager.Write("");
			}
			else
			{
				ClrType val3 = (ClrType)val2.GetHeapType();
				if ((int)val3.ElementType == 14)
				{
					Globals.Manager.Write(Globals.HelperFunctions.GetAsHexString((double)val2.GetValue()));
					string text = (string)val2;
					int num2 = 150;
					if (text.Length > num2)
					{
						text = text.Substring(0, num2) + "...";
					}
					Globals.Manager.Write(Globals.AnalyzeManaged.HTMLEncode(text));
				}
				else if (val3.IsPrimitive)
				{
					object value = val3.GetValue(val2.GetValue());
					Globals.Manager.Write(value.ToString());
				}
				else
				{
					Globals.Manager.Write(Globals.HelperFunctions.GetAsHexString((double)val2.GetValue()));
					Globals.Manager.Write(" " + val3.Name);
				}
			}
			Globals.Manager.Write("</td></tr>");
		}
		Globals.Manager.Write("</table>");
		Globals.Manager.CurrentSection = val.Parent;
	}

	public void PrintSessionStatistics()
	{
		try
		{
			Globals.g_Debugger.Execute("!name2ee System.Web.dll System.Web.SessionState.InProcSessionState");
			string[] array = Globals.AnalyzeManaged.DumpHeapForType("System.Web.SessionState.InProcSessionState");
			if (array.Length != 0)
			{
				int num = 0;
				string[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					_ = array2[i];
					num++;
				}
				Globals.Manager.ReportInformation("<B>Number of active in-process sessions:</B> " + num, 0, "{afcdbe83-ac4f-4b67-8354-5e9735ff246f}");
			}
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "PrintSessionStatistics");
		}
	}

	public void DumpDomainStat()
	{
		try
		{
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("DUMPDOMAIN", (SectionType)0);
			val.Title = "Application Domain Statistics";
			Globals.Manager.CurrentSection = val;
			Globals.Manager.Write("<table border=1 cellpadding=0 cellspacing=0 class=myCustomText><tr ><td>Domain</td><td>Number of Assemblies</td><td>Size of Assemblies</td><td>Name</td></tr></th>");
			foreach (ClrAppDomain appDomain in Globals.g_Debugger.ClrRuntime.AppDomains)
			{
				int num = (from x in appDomain.Modules
					group x by x.AssemblyId).Distinct().Count();
				ulong num2 = 0uL;
				foreach (ClrModule module in appDomain.Modules)
				{
					num2 += module.Size;
				}
				string name = appDomain.Name;
				Globals.Manager.Write("<tr><td>" + appDomain.Address.ToString(Globals.g_Debugger.Is32Bit ? "X8" : "X16") + "</td><td>" + num + "</td><td>" + num2.ToString("n0") + "</td><td>" + name + "</td></tr>");
			}
			Globals.Manager.Write("</table>");
			if (Globals.g_Debugger.ClrRuntime.AppDomains.Count > 3)
			{
				Globals.Manager.ReportWarning("There are more than 3 AppDomains loaded in the process", "For more information look at the <a href='#DUMPDOMAIN" + Globals.g_UniqueReference + "'><b>Application Domain Statistics</b></a> report", 0, "{c6bf1732-3018-41cf-8067-f8d35eb2fc2c}");
			}
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "DumpDomainStat");
		}
	}

	public void DumpDynamicAssemblies()
	{
		try
		{
			int num = (from x in Globals.g_Debugger.ClrRuntime.EnumerateModules()
				where x.IsDynamic
				select x).Count();
			if (num > 0)
			{
				if (num > 300)
				{
					Globals.Manager.ReportWarning("There are " + num + " <b>dynamic assemblies</b> loaded in the dump file", "Incorrect use of XSLT transformations or XmlSerializers can cause a high number of dynamic assmeblies to be generated in a process. Please refer to the follwoing articles to get more details on this isssue. <br/> <ul><li><a href='http://blogs.msdn.com/b/tess/archive/2006/02/15/532804.aspx'>.NET Memory Leak: XmlSerializing your way to a Memory Leak</a></li><li><a href='http://blogs.msdn.com/b/tess/archive/2010/05/05/net-memory-leak-xslcompiledtransform-and-leaked-dynamic-assemblies.aspx'>.NET Memory Leak: XslCompiledTransform and leaked dynamic assemblies</a></li></ul>", 0, "{ba5248f1-8dea-4885-9ca3-07abe716abb8}");
				}
				else
				{
					Globals.Manager.ReportInformation("There are " + num + " <b>dynamic assemblies</b> loaded in the dump file", 0, "{ba5248f1-8dea-4885-9ca3-07abe716abb8}");
				}
			}
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "DumpDynamicAssemblies");
		}
	}

	public void CheckIfInGC()
	{
		try
		{
			foreach (ClrThread thread in Globals.g_Debugger.ClrRuntime.Threads)
			{
				if (thread.IsGC)
				{
					Globals.Manager.ReportOther("The process is currently in the middle of a garbage collection", "Any information about the GC heaps and the objects on the GC heaps may be invalid", "Notification", "notificationicon.png", 0, "{2f1373a7-d4dc-4405-a924-7fe85be24e2e}");
					break;
				}
			}
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "CheckIfInGC");
		}
	}

	public void DumpDataTables()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		string columnNames = null;
		string text = null;
		int rowCount = 0;
		int columnsCount = 0;
		try
		{
			string[] array = Globals.AnalyzeManaged.DumpHeapForType("System.Data.DataTable");
			ReportSection val = Globals.Manager.CurrentSection.AddChildSection("DATATABLES", (SectionType)0);
			val.Title = "Top 10 DataTables in the memory dump";
			Globals.Manager.CurrentSection = val;
			if (array.Length == 0)
			{
				Globals.Manager.Write("There are no DataTables objects in this memory dump");
				Globals.Manager.CurrentSection = val.Parent;
				return;
			}
			List<Tuple<int, string>> list = new List<Tuple<int, string>>();
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				int num = Globals.AnalyzeManaged.DumpShort(text2, "nextRowID");
				string objectAddress = Globals.AnalyzeManaged.DumpObject(text2, "columnCollection");
				string text3 = null;
				text3 = ((1 != Globals.g_Debugger.ClrVersionInfo.Version.Major) ? Globals.AnalyzeManaged.DumpObject(objectAddress, "_list") : Globals.AnalyzeManaged.DumpObject(objectAddress, "list"));
				columnsCount = Globals.AnalyzeManaged.DumpShort(text3, "_size");
				list.Add(new Tuple<int, string>(columnsCount * num, text2));
			}
			Globals.Manager.Write("<table border=1 cellpadding=0 cellspacing=0 class=myCustomText><tr><td>Rows</td><td>ColumnsCount</td><td>Columns</td><td>Size</td></tr>");
			int num2 = 1;
			foreach (Tuple<int, string> item2 in list.OrderByDescending((Tuple<int, string> x) => x.Item1))
			{
				string item = item2.Item2;
				string text4 = "&nbsp;";
				GetDataTableInformation(item, ref columnNames, ref rowCount, ref columnsCount);
				if (1 == num2 || 10 == num2)
				{
					text4 = Globals.HelperFunctions.PrintMemory(BangObjSize(item));
					if (1 == num2)
					{
						text = text4;
					}
				}
				Globals.Manager.Write("<tr><td>" + rowCount + " </td><td>" + columnsCount + "</td><td>" + columnNames + " </td><td>" + text4 + " </td></tr>");
				if (10 != num2)
				{
					num2++;
					continue;
				}
				break;
			}
			Globals.Manager.Write("</table>");
			Globals.Manager.Write("<br> Total " + list.Count + " DataTable objects in the memory");
			Globals.Manager.ReportInformation("Total <B>" + list.Count + "</B> DataTable objects in the memory and the size of the largest DataTable object in memory = " + text + "<BR>For more information check out <a href='#DATATABLES" + Globals.g_UniqueReference + "'>Top 10 DataTable Report</a>", 0, "{e2281056-fee0-4160-9d40-2e9eaae0c6ef}");
			Globals.Manager.CurrentSection = val.Parent;
		}
		catch (Win32Exception ex)
		{
			Globals.HelperFunctions.AddAnalysisError(ex.NativeErrorCode, ex.Source, ex.InnerException.ToString(), "DumpDataTables");
		}
	}

	public void GetDataTableInformation(string DataTableObject, ref string columnNames, ref int rowCount, ref int columnsCount)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		rowCount = Globals.AnalyzeManaged.DumpShort(DataTableObject, "nextRowID");
		string objectAddress = Globals.AnalyzeManaged.DumpObject(DataTableObject, "columnCollection");
		string text = null;
		text = ((1 != Globals.g_Debugger.ClrVersionInfo.Version.Major) ? Globals.AnalyzeManaged.DumpObject(objectAddress, "_list") : Globals.AnalyzeManaged.DumpObject(objectAddress, "list"));
		string theHexStr = Globals.AnalyzeManaged.DumpObject(text, "_items");
		columnsCount = Globals.AnalyzeManaged.DumpShort(text, "_size");
		dynamic dynamicObject = ClrMemDiagExtensions.GetDynamicObject(Globals.g_Debugger.ClrHeap, (ulong)Globals.HelperFunctions.FromHex(theHexStr));
		columnNames = "";
		for (int i = 0; i < columnsCount; i++)
		{
			columnNames = columnNames + (string)dynamicObject[i]._columnName + ", ";
		}
		columnNames = columnNames.Substring(0, columnNames.GetSafeLength() - 2) + "&nbsp;";
	}

	public bool ShouldRunAnalysis(NetScriptManager manager, AnalysisModes mode, ref string filterReason)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		_analysisMode = mode;
		foreach (string dumpFile in manager.GetDumpFiles())
		{
			NetDbgObj val = NetDbgObj.OpenDump(dumpFile, (string)null, (string)null, (object)null, false, true, true);
			try
			{
				string filterReason2 = null;
				if (ShouldRunAnalysis(val, mode, ref filterReason2))
				{
					return true;
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		filterReason = "None of the selected dumps are full usermode dumps with the .NET runtime loaded";
		filterReason += ".";
		return false;
	}

	private bool ShouldRunAnalysis(NetDbgObj debugger, AnalysisModes mode, ref string filterReason)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (debugger.DumpType == "MINIDUMP")
		{
			filterReason = debugger.DumpFileShortName + " is a <b>mini</b> dump.  DotNetMemoryAnalysis requires a <b>full</b> usermode dump with the .NET Runtime loaded.";
			return false;
		}
		if (debugger.IsKernelMode)
		{
			filterReason = debugger.DumpFileShortName + " is a <b>kernel</b> dump.  DotNetMemoryAnalysis requires a full <b>usermode</b> dump with the .NET Runtime loaded.";
			return false;
		}
		if (!debugger.IsManaged)
		{
			filterReason = debugger.DumpFileShortName + " does not have the .NET Runtime loaded. DotNetMemoryAnalysis requires a full usermode dump <b>with the .NET Runtime loaded.</b>";
			return false;
		}
		if ((int)mode == 0)
		{
			return true;
		}
		if (IsVirtBytesHigh(debugger))
		{
			return true;
		}
		filterReason = ".NET Runtime is loaded but there are less than 1GB Virtual Bytes in this process.";
		return false;
	}

	private bool IsVirtBytesHigh(NetDbgObj dbgObj)
	{
		return true;
	}
}
