using System;
using DebugDiag.DbgLib;

namespace DebugDiag.DotNet;

/// <summary>
/// This object represents a progress bar object. An instance of this object is obtained from the <see cref="P:DebugDiag.DotNet.NetScriptManager.Progress" /> property of the <c>NetScriptManager</c> object.
/// </summary>
/// <remarks>
/// <example>
/// <code language="cs">
/// public void RunAnalysisRule(NetScriptManager manager, NetDbgObj debugger, NetProgress progress)
/// {
///     progress.CurrentStatus = "Enumerating Modules in dump file";
///     IModuleInfo Modules = debugger.Modules;
///     progress.SetCurrentRange(0, Modules.Count);
///
///     int count = 0;
///
///     foreach (IDbgModule module in Modules)
///     {
///         if (module.IsISAPIFilter)
///         { 
///             manager.WriteLine(module.ModuleName + " is an ISAPI filter");
///         }
///         count++;
///         progress.CurrentPosition = count;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public class NetProgress : IProgress
{
	private const string END_DEBUGGER_STATUS = "END";

	public virtual int OverallPosition
	{
		set
		{
			if (this.OnSetOverallPositionChanged != null)
			{
				this.OnSetOverallPositionChanged(this, new SetOverallPositionEventArgs(value));
			}
		}
	}

	public virtual int CurrentPosition
	{
		set
		{
			if (this.OnSetCurrentPositionChanged != null)
			{
				this.OnSetCurrentPositionChanged(this, new SetCurrentPositionEventArgs(value));
			}
		}
	}

	public virtual string OverallStatus
	{
		set
		{
			if (this.OnSetOverallStatusChanged != null)
			{
				this.OnSetOverallStatusChanged(this, new SetOverallStatusEventArgs(value));
			}
		}
	}

	public virtual string CurrentStatus
	{
		set
		{
			if (this.OnSetCurrentStatusChanged != null)
			{
				this.OnSetCurrentStatusChanged(this, new SetCurrentStatusEventArgs(value));
			}
		}
	}

	public virtual string DebuggerStatus
	{
		set
		{
			if (value == "END")
			{
				End();
			}
			else if (this.OnSetDebuggerStatusChanged != null)
			{
				this.OnSetDebuggerStatusChanged(this, new SetDebuggerStatusEventArgs(value));
			}
		}
	}

	public event EventHandler<SetOverallRangeEventArgs> OnSetOverallRangeChanged;

	public event EventHandler<SetCurrentRangeEventArgs> OnSetCurrentRangeChanged;

	public event EventHandler<SetOverallPositionEventArgs> OnSetOverallPositionChanged;

	public event EventHandler<SetCurrentPositionEventArgs> OnSetCurrentPositionChanged;

	public event EventHandler<SetOverallStatusEventArgs> OnSetOverallStatusChanged;

	public event EventHandler<SetCurrentStatusEventArgs> OnSetCurrentStatusChanged;

	public event EventHandler<SetDebuggerStatusEventArgs> OnSetDebuggerStatusChanged;

	public event EventHandler OnEnd;

	public virtual void SetOverallRange(int Low, int High)
	{
		if (this.OnSetOverallRangeChanged != null)
		{
			this.OnSetOverallRangeChanged(this, new SetOverallRangeEventArgs(Low, High));
		}
	}

	/// <summary>
	/// This method takes two integer arguments and sets the progress range for the current script (child) execution.
	/// <event cref="E:DebugDiag.DotNet.NetProgress.OnSetCurrentRangeChanged">This event is raised when calling this method.</event>
	/// </summary>
	/// <param name="Low"></param>
	/// <param name="High"></param>
	public virtual void SetCurrentRange(int Low, int High)
	{
		if (this.OnSetCurrentRangeChanged != null)
		{
			this.OnSetCurrentRangeChanged(this, new SetCurrentRangeEventArgs(Low, High));
		}
	}

	public virtual void End()
	{
		if (this.OnEnd != null)
		{
			this.OnEnd(this, null);
		}
	}
}
