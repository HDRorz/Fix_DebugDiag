using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopArrayType : BaseDesktopHeapType
{
	private ClrElementType _arrayElement;

	private ClrType _arrayElementType;

	private int _ranks;

	private string _name;

	public override ClrModule Module => base.DesktopModule;

	public override int Index => -1;

	public override string Name
	{
		get
		{
			if (_name == null)
			{
				BuildName(null);
			}
			return _name;
		}
	}

	public override ClrType ArrayComponentType
	{
		get
		{
			if (_arrayElementType == null)
			{
				_arrayElementType = base.DesktopHeap.GetBasicType(_arrayElement);
			}
			return _arrayElementType;
		}
		internal set
		{
			if (value != null)
			{
				_arrayElementType = value;
			}
		}
	}

	public override bool IsArray => true;

	public override IList<ClrInstanceField> Fields => new ClrInstanceField[0];

	public override IList<ClrStaticField> StaticFields => new ClrStaticField[0];

	public override IList<ClrThreadStaticField> ThreadStaticFields => new ClrThreadStaticField[0];

	public override IList<ClrMethod> Methods => new ClrMethod[0];

	public override ClrHeap Heap => base.DesktopHeap;

	public override IList<ClrInterface> Interfaces => new ClrInterface[0];

	public override bool IsFinalizable => false;

	public override bool IsPublic => true;

	public override bool IsPrivate => false;

	public override bool IsInternal => false;

	public override bool IsProtected => false;

	public override bool IsAbstract => false;

	public override bool IsSealed => false;

	public override bool IsInterface => false;

	public override ClrType BaseType => base.DesktopHeap.ArrayType;

	public override int ElementSize => DesktopInstanceField.GetSize(null, _arrayElement);

	public override int BaseSize => IntPtr.Size * 8;

	public DesktopArrayType(DesktopGCHeap heap, DesktopBaseModule module, ClrElementType eltype, int ranks, uint token, string nameHint)
		: base(heap, module, token)
	{
		ElementType = ClrElementType.Array;
		_arrayElement = eltype;
		_ranks = ranks;
		if (nameHint != null)
		{
			BuildName(nameHint);
		}
	}

	internal override ulong GetModuleAddress(ClrAppDomain domain)
	{
		return 0uL;
	}

	private void BuildName(string hint)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ClrType arrayComponentType = ArrayComponentType;
		stringBuilder.Append((arrayComponentType != null) ? arrayComponentType.Name : GetElementTypeName(hint));
		stringBuilder.Append("[");
		for (int i = 0; i < _ranks - 1; i++)
		{
			stringBuilder.Append(",");
		}
		stringBuilder.Append("]");
		_name = stringBuilder.ToString();
	}

	private string GetElementTypeName(string hint)
	{
		switch (_arrayElement)
		{
		case ClrElementType.Boolean:
			return "System.Boolean";
		case ClrElementType.Char:
			return "System.Char";
		case ClrElementType.Int8:
			return "System.SByte";
		case ClrElementType.UInt8:
			return "System.Byte";
		case ClrElementType.Int16:
			return "System.Int16";
		case ClrElementType.UInt16:
			return "ClrElementType.UInt16";
		case ClrElementType.Int32:
			return "System.Int32";
		case ClrElementType.UInt32:
			return "System.UInt32";
		case ClrElementType.Int64:
			return "System.Int64";
		case ClrElementType.UInt64:
			return "System.UInt64";
		case ClrElementType.Float:
			return "System.Single";
		case ClrElementType.Double:
			return "System.Double";
		case ClrElementType.NativeInt:
			return "System.IntPtr";
		case ClrElementType.NativeUInt:
			return "System.UIntPtr";
		case ClrElementType.Struct:
			return "Sytem.ValueType";
		default:
			if (hint != null)
			{
				return hint;
			}
			return "ARRAY";
		}
	}

	public override bool IsFinalizeSuppressed(ulong obj)
	{
		return false;
	}

	public override ulong GetSize(ulong objRef)
	{
		return base.DesktopHeap.GetObjectType(objRef).GetSize(objRef);
	}

	public override void EnumerateRefsOfObject(ulong objRef, Action<ulong, int> action)
	{
		base.DesktopHeap.GetObjectType(objRef).EnumerateRefsOfObject(objRef, action);
	}

	public override bool GetFieldForOffset(int fieldOffset, bool inner, out ClrInstanceField childField, out int childFieldOffset)
	{
		childField = null;
		childFieldOffset = 0;
		return false;
	}

	public override ClrInstanceField GetFieldByName(string name)
	{
		return null;
	}

	public override ClrStaticField GetStaticFieldByName(string name)
	{
		return null;
	}

	public override int GetArrayLength(ulong objRef)
	{
		throw new NotImplementedException();
	}

	public override ulong GetArrayElementAddress(ulong objRef, int index)
	{
		throw new NotImplementedException();
	}

	public override object GetArrayElementValue(ulong objRef, int index)
	{
		throw new NotImplementedException();
	}

	public override void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action)
	{
		base.DesktopHeap.GetObjectType(objRef).EnumerateRefsOfObjectCarefully(objRef, action);
	}
}
