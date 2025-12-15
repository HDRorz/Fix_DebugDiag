using System.IO;

namespace Microsoft.Diagnostics.Runtime;

public abstract class ClrStackFrame
{
	public static string UnknownModuleName = "UNKNOWN";

	public abstract ulong InstructionPointer { get; }

	public abstract ulong StackPointer { get; }

	public abstract ClrStackFrameType Kind { get; }

	public abstract string DisplayString { get; }

	public abstract ClrMethod Method { get; }

	public virtual string ModuleName
	{
		get
		{
			if (Method == null || Method.Type == null || Method.Type.Module == null)
			{
				return UnknownModuleName;
			}
			string name = Method.Type.Module.Name;
			try
			{
				return Path.GetFileNameWithoutExtension(name);
			}
			catch
			{
				return name;
			}
		}
	}

	public abstract SourceLocation GetFileAndLineNumber();
}
