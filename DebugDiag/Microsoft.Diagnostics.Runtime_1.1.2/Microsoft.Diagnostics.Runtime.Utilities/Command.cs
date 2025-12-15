using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal sealed class Command
{
	private static string[] s_pathExts;

	private static string[] s_paths;

	private readonly string _commandLine;

	private readonly StringBuilder _output;

	private TextWriter _outputStream;

	public DateTime StartTime => Process.StartTime;

	public bool HasExited => Process.HasExited;

	public DateTime ExitTime => Process.ExitTime;

	public TimeSpan Duration => ExitTime - StartTime;

	public int Id => Process.Id;

	public int ExitCode => Process.ExitCode;

	public string Output
	{
		get
		{
			if (_outputStream != null)
			{
				throw new Exception("Output not available if redirected to file or stream");
			}
			return _output.ToString();
		}
	}

	public CommandOptions Options { get; }

	public Process Process { get; }

	private static string[] PathExts
	{
		get
		{
			if (s_pathExts == null)
			{
				s_pathExts = Environment.GetEnvironmentVariable("PATHEXT").Split(';');
			}
			return s_pathExts;
		}
	}

	private static string[] Paths
	{
		get
		{
			if (s_paths == null)
			{
				s_paths = Environment.GetEnvironmentVariable("PATH").Split(';');
			}
			return s_paths;
		}
	}

	public static Command Run(string commandLine, CommandOptions options)
	{
		Command command = new Command(commandLine, options);
		command.Wait();
		return command;
	}

	public static Command Run(string commandLine)
	{
		return Run(commandLine, new CommandOptions());
	}

	public Command(string commandLine, CommandOptions options)
	{
		Options = options;
		_commandLine = commandLine;
		Match match = Regex.Match(commandLine, "^\\s*\"(.*?)\"\\s*(.*)");
		if (!match.Success)
		{
			match = Regex.Match(commandLine, "\\s*(\\S*)\\s*(.*)");
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(match.Groups[1].Value, match.Groups[2].Value)
		{
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			ErrorDialog = false,
			CreateNoWindow = true,
			RedirectStandardInput = (options.Input != null)
		};
		Process = new Process
		{
			StartInfo = processStartInfo
		};
		Process.StartInfo = processStartInfo;
		_output = new StringBuilder();
		if (options.elevate)
		{
			options.useShellExecute = true;
			processStartInfo.Verb = "runas";
			if (options.currentDirectory == null)
			{
				options.currentDirectory = Environment.CurrentDirectory;
			}
		}
		Process.OutputDataReceived += OnProcessOutput;
		Process.ErrorDataReceived += OnProcessOutput;
		if (options.environmentVariables != null)
		{
			foreach (string key in options.environmentVariables.Keys)
			{
				string text = options.environmentVariables[key];
				if (text != null)
				{
					int startat = 0;
					while (true)
					{
						match = new Regex("%(\\w+)%").Match(text, startat);
						if (!match.Success)
						{
							break;
						}
						string value = match.Groups[1].Value;
						string text2;
						if (processStartInfo.EnvironmentVariables.ContainsKey(value))
						{
							text2 = processStartInfo.EnvironmentVariables[value];
						}
						else
						{
							text2 = Environment.GetEnvironmentVariable(value);
							if (text2 == null)
							{
								text2 = "";
							}
						}
						int num = match.Groups[1].Index - 1;
						int num2 = num + match.Groups[1].Length + 2;
						text = text.Substring(0, num) + text2 + text.Substring(num2, text.Length - num2);
						startat = num + text2.Length;
					}
				}
				processStartInfo.EnvironmentVariables[key] = text;
			}
		}
		processStartInfo.WorkingDirectory = options.currentDirectory;
		_outputStream = options.outputStream;
		if (options.outputFile != null)
		{
			_outputStream = File.CreateText(options.outputFile);
		}
		try
		{
			Process.Start();
		}
		catch (Exception ex)
		{
			string text3 = "Failure starting Process\r\n    Exception: " + ex.Message + "\r\n    Cmd: " + commandLine + "\r\n";
			if (Regex.IsMatch(processStartInfo.FileName, "^(copy|dir|del|color|set|cd|cdir|md|mkdir|prompt|pushd|popd|start|assoc|ftype)", RegexOptions.IgnoreCase))
			{
				text3 = text3 + "    Cmd " + processStartInfo.FileName + " implemented by Cmd.exe, fix by prefixing with 'cmd /c'.";
			}
			throw new Exception(text3, ex);
		}
		if (!processStartInfo.UseShellExecute)
		{
			Process.BeginOutputReadLine();
			Process.BeginErrorReadLine();
		}
		if (options.input != null)
		{
			Process.StandardInput.Write(options.input);
			Process.StandardInput.Dispose();
		}
	}

	public Command(string commandLine)
		: this(commandLine, new CommandOptions())
	{
	}

	public Command Wait()
	{
		if (Options.noWait)
		{
			return this;
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			Process.WaitForExit(Options.timeoutMSec);
			flag = true;
			for (int i = 0; i < 10; i++)
			{
				Thread.Sleep(1);
			}
		}
		finally
		{
			if (!Process.HasExited)
			{
				flag2 = true;
				Kill();
			}
		}
		if (_outputStream != null && Options.outputFile != null)
		{
			_outputStream.Dispose();
		}
		_outputStream = null;
		if (flag && flag2)
		{
			throw new Exception("Timeout of " + Options.timeoutMSec / 1000 + " sec exceeded\r\n    Cmd: " + _commandLine);
		}
		if (Process.ExitCode != 0 && !Options.noThrow)
		{
			ThrowCommandFailure(null);
		}
		return this;
	}

	public void ThrowCommandFailure(string message)
	{
		if (Process.ExitCode != 0)
		{
			string text = "";
			if (_outputStream == null)
			{
				string text2 = _output.ToString();
				Match match = Regex.Match(text2, "^(\\s*\\n)?(.+\\n)(.|\\n)*?(.+\\n.*\\S)\\s*$");
				text2 = ((!match.Success) ? text2.Trim() : (match.Groups[2].Value + "    <<< Omitted output ... >>>\r\n" + match.Groups[4].Value));
				text2 = text2.Replace("\n", "\n    ");
				text = "\r\n  Output: {\r\n    " + text2 + "\r\n  }";
			}
			if (message == null)
			{
				message = "";
			}
			else if (message.Length > 0)
			{
				message += "\r\n";
			}
			throw new Exception(message + "Process returned exit code 0x" + Process.ExitCode.ToString("x") + "\r\n  Cmd: " + _commandLine + text);
		}
	}

	public void Kill()
	{
		try
		{
			Run("taskkill /f /t /pid " + Process.Id);
		}
		catch (Exception)
		{
		}
		int num = 0;
		do
		{
			Thread.Sleep(10);
			num++;
			_ = 100;
		}
		while (!Process.HasExited);
		if (_outputStream != null && Options.outputFile != null)
		{
			_outputStream.Dispose();
		}
		_outputStream = null;
	}

	public static string Quote(string str)
	{
		if (str.IndexOf('"') < 0)
		{
			str = Regex.Replace(str, "\\*\"", "\\$1");
		}
		return "\"" + str + "\"";
	}

	public static string FindOnPath(string commandExe)
	{
		string text = ProbeForExe(commandExe);
		if (text != null)
		{
			return text;
		}
		if (!commandExe.Contains("\\"))
		{
			string[] paths = Paths;
			for (int i = 0; i < paths.Length; i++)
			{
				text = ProbeForExe(Path.Combine(paths[i], commandExe));
				if (text != null)
				{
					return text;
				}
			}
		}
		return null;
	}

	private static string ProbeForExe(string path)
	{
		if (File.Exists(path))
		{
			return path;
		}
		string[] pathExts = PathExts;
		foreach (string text in pathExts)
		{
			string text2 = path + text;
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		return null;
	}

	private void OnProcessOutput(object sender, DataReceivedEventArgs e)
	{
		if (_outputStream != null)
		{
			_outputStream.WriteLine(e.Data);
		}
		else
		{
			_output.AppendLine(e.Data);
		}
	}
}
