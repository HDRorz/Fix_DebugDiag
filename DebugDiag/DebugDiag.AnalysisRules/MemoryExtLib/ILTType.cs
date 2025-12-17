using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryExtLib;

[ComImport]
[CompilerGenerated]
[Guid("F1EC038E-EDBD-4105-BA97-DBACD45C18CC")]
[TypeIdentifier]
public interface ILTType
{
	[DispId(1)]
	double AllocationCount
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	[DispId(2)]
	double AllocationSize
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	[DispId(3)]
	int LeakProbability
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		get;
	}
}
