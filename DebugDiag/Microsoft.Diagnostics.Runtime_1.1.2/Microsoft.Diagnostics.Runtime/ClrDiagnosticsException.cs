using System;
using System.Runtime.Serialization;

namespace Microsoft.Diagnostics.Runtime;

[Serializable]
public class ClrDiagnosticsException : Exception
{
	public ClrDiagnosticsExceptionKind Kind { get; }

	internal ClrDiagnosticsException(string message, ClrDiagnosticsExceptionKind kind = ClrDiagnosticsExceptionKind.Unknown, int hr = -2146233088)
		: base(message)
	{
		Kind = kind;
		base.HResult = hr;
	}

	protected ClrDiagnosticsException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		Kind = (ClrDiagnosticsExceptionKind)info.GetValue("Kind", typeof(ClrDiagnosticsExceptionKind));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("Kind", Kind, typeof(ClrDiagnosticsExceptionKind));
		base.GetObjectData(info, context);
	}
}
