using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Runtime.Desktop;

internal class DesktopPointerType : BaseDesktopHeapType
{
	private readonly ClrElementType _pointerElement;

	private ClrType _pointerElementType;

	private string _name;

	public override ClrModule Module => base.DesktopModule;

	public override ulong MethodTable => 0uL;

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

	public override ClrType ComponentType
	{
		get
		{
			if (_pointerElementType == null)
			{
				_pointerElementType = base.DesktopHeap.GetBasicType(_pointerElement);
			}
			return _pointerElementType;
		}
		internal set
		{
			if (value != null)
			{
				_pointerElementType = value;
			}
		}
	}

	public override bool IsPointer => true;

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

	public override ClrType BaseType => null;

	public override int ElementSize => DesktopInstanceField.GetSize(null, _pointerElement);

	public override int BaseSize => IntPtr.Size * 8;

	public DesktopPointerType(DesktopGCHeap heap, DesktopBaseModule module, ClrElementType eltype, uint token, string nameHint)
		: base(0uL, heap, module, token)
	{
		ElementType = ClrElementType.Pointer;
		_pointerElement = eltype;
		if (nameHint != null)
		{
			BuildName(nameHint);
		}
	}

	public override IEnumerable<ulong> EnumerateMethodTables()
	{
		return new ulong[1] { MethodTable };
	}

	internal override ulong GetModuleAddress(ClrAppDomain domain)
	{
		return 0uL;
	}

	private void BuildName(string hint)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ClrType componentType = ComponentType;
		stringBuilder.Append((componentType != null) ? componentType.Name : GetElementTypeName(hint));
		stringBuilder.Append("*");
		_name = stringBuilder.ToString();
	}

	private string GetElementTypeName(string hint)
	{
		switch (_pointerElement)
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
			return "POINTER";
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
		return 0;
	}

	public override ulong GetArrayElementAddress(ulong objRef, int index)
	{
		return 0uL;
	}

	public override object GetArrayElementValue(ulong objRef, int index)
	{
		return null;
	}

	public override void EnumerateRefsOfObjectCarefully(ulong objRef, Action<ulong, int> action)
	{
		base.DesktopHeap.GetObjectType(objRef).EnumerateRefsOfObjectCarefully(objRef, action);
	}
}
