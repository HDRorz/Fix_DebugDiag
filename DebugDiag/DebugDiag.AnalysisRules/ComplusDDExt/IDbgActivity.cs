using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ComplusDDExt;

[ComImport]
[CompilerGenerated]
[Guid("72D7D726-213A-4079-AF8E-696CDCABA0C9")]
[TypeIdentifier]
public interface IDbgActivity
{
	[DispId(1)]
	string State
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	[DispId(2)]
	double Address
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	[DispId(3)]
	int OwnerThreadNum
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}

	[DispId(4)]
	int WaitingThreadCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(5)]
	int GetWaitingThreadByIndex([In] int Index);
}
