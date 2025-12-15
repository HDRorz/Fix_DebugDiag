namespace DebugDiag.DotNet.Mex;

internal class MexWrapperBase
{
	public readonly dynamic RealMexObject;

	protected MexWrapperBase(dynamic realMexObject)
	{
		RealMexObject = realMexObject;
	}
}
