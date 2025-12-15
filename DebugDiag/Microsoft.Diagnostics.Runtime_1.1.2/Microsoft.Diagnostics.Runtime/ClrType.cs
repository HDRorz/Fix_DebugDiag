using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrType
{
	protected internal abstract GCDesc GCDesc { get; }

	public abstract ulong MethodTable { get; }

	public abstract uint MetadataToken { get; }

	public abstract string Name { get; }

	public virtual bool ContainsPointers => true;

	public virtual bool IsCollectible => false;

	public virtual ulong LoaderAllocatorObject => 0uL;

	public abstract ClrHeap Heap { get; }

	public virtual bool IsRuntimeType => false;

	public virtual ClrModule Module => null;

	public virtual ClrElementType ElementType
	{
		get
		{
			return ClrElementType.Unknown;
		}
		internal set
		{
			throw new NotImplementedException();
		}
	}

	public virtual bool IsPrimitive => ElementType.IsPrimitive();

	public virtual bool IsValueClass => ElementType.IsValueClass();

	public virtual bool IsObjectReference => ElementType.IsObjectReference();

	public abstract IList<ClrInterface> Interfaces { get; }

	public abstract bool IsFinalizable { get; }

	public abstract bool IsPublic { get; }

	public abstract bool IsPrivate { get; }

	public abstract bool IsInternal { get; }

	public abstract bool IsProtected { get; }

	public abstract bool IsAbstract { get; }

	public abstract bool IsSealed { get; }

	public abstract bool IsInterface { get; }

	public virtual IList<ClrInstanceField> Fields => null;

	public virtual IList<ClrStaticField> StaticFields => null;

	public virtual IList<ClrThreadStaticField> ThreadStaticFields => null;

	public virtual IList<ClrMethod> Methods => null;

	public abstract ClrType BaseType { get; }

	public virtual bool IsPointer => false;

	public virtual ClrType ComponentType { get; internal set; }

	public virtual bool IsArray => false;

	public abstract int ElementSize { get; }

	public abstract int BaseSize { get; }

	public virtual bool IsString => false;

	public virtual bool IsFree => false;

	public virtual bool IsException => false;

	public virtual bool IsEnum => false;

	public virtual bool HasSimpleValue => false;

	public abstract IEnumerable<ulong> EnumerateMethodTables();

	public abstract ulong GetSize(ulong objRef);

	public abstract void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> action);

	public abstract void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action);

	public virtual IEnumerable<ClrObject> EnumerateObjectReferences(ulong obj, bool carefully = false)
	{
		return Heap.EnumerateObjectReferences(obj, this, carefully);
	}

	public virtual IEnumerable<ClrObjectReference> EnumerateObjectReferencesWithFields(ulong obj, bool carefully = false)
	{
		return Heap.EnumerateObjectReferencesWithFields(obj, this, carefully);
	}

	public virtual ClrType GetRuntimeType(ulong obj)
	{
		throw new NotImplementedException();
	}

	internal virtual ClrMethod GetMethod(uint token)
	{
		return null;
	}

	public virtual bool IsFinalizeSuppressed(ulong obj)
	{
		throw new NotImplementedException();
	}

	public abstract bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset);

	public abstract ClrInstanceField GetFieldByName(string name);

	public abstract ClrStaticField GetStaticFieldByName(string name);

	public virtual bool IsCCW(ulong obj)
	{
		return false;
	}

	public virtual CcwData GetCCWData(ulong obj)
	{
		return null;
	}

	public virtual bool IsRCW(ulong obj)
	{
		return false;
	}

	public virtual RcwData GetRCWData(ulong obj)
	{
		return null;
	}

	public abstract int GetArrayLength(ulong objRef);

	public abstract ulong GetArrayElementAddress(ulong objRef, int index);

	public abstract object GetArrayElementValue(ulong objRef, int index);

	public virtual ClrElementType GetEnumElementType()
	{
		throw new NotImplementedException();
	}

	public virtual IEnumerable<string> GetEnumNames()
	{
		throw new NotImplementedException();
	}

	public virtual string GetEnumName(object value)
	{
		throw new NotImplementedException();
	}

	public virtual string GetEnumName(int value)
	{
		throw new NotImplementedException();
	}

	public virtual bool TryGetEnumValue(string name, out int value)
	{
		throw new NotImplementedException();
	}

	public virtual bool TryGetEnumValue(string name, out object value)
	{
		throw new NotImplementedException();
	}

	public virtual object GetValue(ulong address)
	{
		return null;
	}

	public override string ToString()
	{
		return Name;
	}
}
