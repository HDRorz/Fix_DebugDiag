#define TRACE
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace DebugDiag.DotNet;

internal class DDTrace
{
	private static TraceSwitch _traceSwitch;

	static DDTrace()
	{
		_traceSwitch = new TraceSwitch("DDLoggingLevel", "", "Error");
		if (_traceSwitch.Level != 0)
		{
			if (GetConfigBool("LogToLocalFile", defaultValue: true))
			{
				string fileName = Path.Combine(Environment.CurrentDirectory, Process.GetCurrentProcess().MainModule.ModuleName + ".log");
				Trace.Listeners.Add(new TextWriterTraceListener(fileName));
				Trace.AutoFlush = true;
			}
			if (GetConfigBool("LogToConsole", defaultValue: true))
			{
				Trace.Listeners.Add(new ConsoleTraceListener());
				Trace.AutoFlush = true;
			}
		}
	}

	private static bool GetConfigBool(string key, bool defaultValue)
	{
		if (bool.TryParse(ConfigurationManager.AppSettings[key], out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public static void TraceError(string msg)
	{
		TraceError(msg, null);
	}

	public static void TraceError(string msg, params object[] args)
	{
		if (_traceSwitch.TraceError)
		{
			Trace.TraceError(msg, args);
		}
	}

	public static void TraceWarning(string msg)
	{
		TraceWarning(msg, null);
	}

	public static void TraceWarning(string msg, params object[] args)
	{
		if (_traceSwitch.TraceWarning)
		{
			Trace.TraceWarning(msg, args);
		}
	}

	public static void TraceInformation(string msg)
	{
		TraceInformation(msg, null);
	}

	public static void TraceInformation(string msg, params object[] args)
	{
		if (_traceSwitch.TraceInfo)
		{
			Trace.TraceInformation(msg, args);
		}
	}

	public static void WriteLine(string message)
	{
		WriteLine(message, null, TraceLevel.Verbose);
	}

	public static void WriteLine(string message, string category)
	{
		WriteLine(message, category, TraceLevel.Verbose);
	}

	public static void WriteLine(string message, TraceLevel traceLevel)
	{
		WriteLine(message, null, traceLevel);
	}

	public static void WriteLine(string line, string category, TraceLevel traceLevel)
	{
		switch (traceLevel)
		{
		case TraceLevel.Verbose:
			Trace.WriteLineIf(_traceSwitch.TraceVerbose, line);
			break;
		case TraceLevel.Info:
			Trace.WriteLineIf(_traceSwitch.TraceInfo, line);
			break;
		case TraceLevel.Warning:
			Trace.WriteLineIf(_traceSwitch.TraceWarning, line);
			break;
		case TraceLevel.Error:
			Trace.WriteLineIf(_traceSwitch.TraceError, line);
			break;
		}
	}
}
