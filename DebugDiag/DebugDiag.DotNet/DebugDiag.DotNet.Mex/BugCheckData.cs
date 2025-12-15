namespace DebugDiag.DotNet.Mex;

internal class BugCheckData : MexWrapperBase
{
	public uint Code => RealMexObject.Code;

	public BugCheckData(object realMexObject)
		: base(realMexObject)
	{
	}
}
