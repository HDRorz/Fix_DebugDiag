using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.RuntimeExt;

[ComVisible(true)]
[Guid("7505BB76-73B1-11E1-BAD9-E6174924019B")]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(IMDActivator))]
public class CLRMDActivator : IMDActivator
{
	public void CreateFromCrashDump(string crashdump, out IMDTarget ppTarget)
	{
		ppTarget = new MDTarget(crashdump);
	}

	public void CreateFromIDebugClient(object iDebugClient, out IMDTarget ppTarget)
	{
		ppTarget = new MDTarget(iDebugClient);
	}
}
