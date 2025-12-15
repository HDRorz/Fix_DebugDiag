using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrRuntime
{
	public delegate void RuntimeFlushedCallback(ClrRuntime runtime);

	public abstract DataTarget DataTarget { get; }

	public bool ServerGC { get; protected set; }

	public int HeapCount { get; protected set; }

	public abstract int PointerSize { get; }

	public abstract IList<ClrAppDomain> AppDomains { get; }

	public abstract IList<ClrThread> Threads { get; }

	public event RuntimeFlushedCallback RuntimeFlushed;

	public abstract IEnumerable<int> EnumerateGCThreads();

	public abstract IEnumerable<ulong> EnumerateFinalizerQueue();

	public abstract CcwData GetCcwDataFromAddress(ulong addr);

	[Obsolete("Use ReadMemory instead.")]
	public abstract bool ReadVirtual(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	public abstract bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	public abstract bool ReadPointer(ulong address, out ulong value);

	public abstract IEnumerable<ClrHandle> EnumerateHandles();

	public abstract ClrHeap GetHeap();

	public abstract ClrHeap GetHeap(TextWriter diagnosticLog);

	public virtual ClrThreadPool GetThreadPool()
	{
		throw new NotImplementedException();
	}

	public abstract IEnumerable<ClrMemoryRegion> EnumerateMemoryRegions();

	public abstract ClrMethod GetMethodByAddress(ulong ip);

	public abstract IEnumerable<ClrModule> EnumerateModules();

	public abstract void Flush();

	protected void OnRuntimeFlushed()
	{
		this.RuntimeFlushed?.Invoke(this);
	}

	internal static bool IsPrimitive(ClrElementType cet)
	{
		if ((cet < ClrElementType.Boolean || cet > ClrElementType.Double) && cet != ClrElementType.NativeInt && cet != ClrElementType.NativeUInt && cet != ClrElementType.Pointer)
		{
			return cet == ClrElementType.FunctionPointer;
		}
		return true;
	}

	internal static bool IsValueClass(ClrElementType cet)
	{
		return cet == ClrElementType.Struct;
	}

	internal static bool IsObjectReference(ClrElementType cet)
	{
		if (cet != ClrElementType.String && cet != ClrElementType.Class && cet != ClrElementType.Array && cet != ClrElementType.SZArray)
		{
			return cet == ClrElementType.Object;
		}
		return true;
	}

	internal static Type GetTypeForElementType(ClrElementType type)
	{
		switch (type)
		{
		case ClrElementType.Boolean:
			return typeof(bool);
		case ClrElementType.Char:
			return typeof(char);
		case ClrElementType.Double:
			return typeof(double);
		case ClrElementType.Float:
			return typeof(float);
		case ClrElementType.Pointer:
		case ClrElementType.NativeInt:
		case ClrElementType.FunctionPointer:
			return typeof(IntPtr);
		case ClrElementType.NativeUInt:
			return typeof(UIntPtr);
		case ClrElementType.Int16:
			return typeof(short);
		case ClrElementType.Int32:
			return typeof(int);
		case ClrElementType.Int64:
			return typeof(long);
		case ClrElementType.Int8:
			return typeof(sbyte);
		case ClrElementType.UInt16:
			return typeof(ushort);
		case ClrElementType.UInt32:
			return typeof(uint);
		case ClrElementType.UInt64:
			return typeof(ulong);
		case ClrElementType.UInt8:
			return typeof(byte);
		default:
			return null;
		}
	}
}
