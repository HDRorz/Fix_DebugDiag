using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Utilities;

/// <summary>
/// Command represents a running of a command lineNumber process.  It is basically
/// a wrapper over System.Diagnostics.Process, which hides the complexitity
/// of System.Diagnostics.Process, and knows how to capture output and otherwise
/// makes calling commands very easy.
/// </summary>
internal sealed class Command
{
	private static string[] pathExts;

	private static string[] paths;

	private string commandLine;

	private Process process;

	private StringBuilder output;

	private CommandOptions options;

	private TextWriter outputStream;

	/// <summary>
	/// The time the process started.  
	/// </summary>
	internal DateTime StartTime => process.StartTime;

	/// <summary>
	/// returns true if the process has exited. 
	/// </summary>
	internal bool HasExited => process.HasExited;

	/// <summary>
	/// The time the processed Exited.  (HasExited should be true before calling)
	/// </summary>
	internal DateTime ExitTime => process.ExitTime;

	/// <summary>
	/// The duration of the command (HasExited should be true before calling)
	/// </summary>
	internal TimeSpan Duration => ExitTime - StartTime;

	/// <summary>
	/// The operating system ID for the subprocess.  
	/// </summary>
	internal int Id => process.Id;

	/// <summary>
	/// The process exit code for the subprocess.  (HasExited should be true before calling)
	/// Often this does not need to be checked because Command.Run will throw an exception 
	/// if it is not zero.   However it is useful if the CommandOptions.NoThrow property 
	/// was set.  
	/// </summary>
	internal int ExitCode => process.ExitCode;

	/// <summary>
	/// The standard output and standard error output from the command.  This
	/// is accumulated in real time so it can vary if the process is still running.
	///
	/// This property is NOT available if the CommandOptions.OutputFile or CommandOptions.OutputStream
	/// is specified since the output is being redirected there.   If a large amount of output is 
	/// expected (&gt; 1Meg), the Run.AddOutputStream(Stream) is recommended for retrieving it since
	/// the large string is never materialized at one time. 
	/// </summary>
	internal string Output
	{
		get
		{
			if (outputStream != null)
			{
				throw new Exception("Output not available if redirected to file or stream");
			}
			return output.ToString();
		}
	}

	/// <summary>
	/// Returns that CommandOptions structure that holds all the options that affect
	/// the running of the command (like Timeout, Input ...)
	/// </summary>
	internal CommandOptions Options => options;

	/// <summary>
	/// Get the underlying process object.  Generally not used. 
	/// </summary>
	internal Process Process => process;

	private static string[] PathExts
	{
		get
		{
			if (pathExts == null)
			{
				pathExts = Environment.GetEnvironmentVariable("PATHEXT").Split(';');
			}
			return pathExts;
		}
	}

	private static string[] Paths
	{
		get
		{
			if (paths == null)
			{
				paths = Environment.GetEnvironmentVariable("PATH").Split(';');
			}
			return paths;
		}
	}

	/// <summary>
	/// Run 'commandLine', sending the output to the console, and wait for the command to complete.
	/// This simulates what batch filedo when executing their commands.  It is a bit more verbose
	/// by default, however 
	/// </summary>
	/// <param variable="commandLine">The command lineNumber to run as a subprocess</param>
	/// <param variable="options">Additional qualifiers that control how the process is run</param>
	/// <returns>A Command structure that can be queried to determine ExitCode, Output, etc.</returns>
	internal static Command RunToConsole(string commandLine, CommandOptions options)
	{
		return Run(commandLine, options.Clone().AddOutputStream(Console.Out));
	}

	internal static Command RunToConsole(string commandLine)
	{
		return RunToConsole(commandLine, new CommandOptions());
	}

	/// <summary>
	/// Run 'commandLine' as a subprocess and waits for the command to complete.
	/// Output is captured and placed in the 'Output' property of the returned Command 
	/// structure. 
	/// </summary>
	/// <param variable="commandLine">The command lineNumber to run as a subprocess</param>
	/// <param variable="options">Additional qualifiers that control how the process is run</param>
	/// <returns>A Command structure that can be queried to determine ExitCode, Output, etc.</returns>
	internal static Command Run(string commandLine, CommandOptions options)
	{
		Command command = new Command(commandLine, options);
		command.Wait();
		return command;
	}

	internal static Command Run(string commandLine)
	{
		return Run(commandLine, new CommandOptions());
	}

	/// <summary>
	/// Launch a new command and returns the Command object that can be used to monitor
	/// the restult.  It does not wait for the command to complete, however you 
	/// can call 'Wait' to do that, or use the 'Run' or 'RunToConsole' methods. */
	/// </summary>
	/// <param variable="commandLine">The command lineNumber to run as a subprocess</param>
	/// <param variable="options">Additional qualifiers that control how the process is run</param>
	/// <returns>A Command structure that can be queried to determine ExitCode, Output, etc.</returns>
	internal Command(string commandLine, CommandOptions options)
	{
		this.options = options;
		this.commandLine = commandLine;
		Match match = Regex.Match(commandLine, "^\\s*\"(.*?)\"\\s*(.*)");
		if (!match.Success)
		{
			match = Regex.Match(commandLine, "\\s*(\\S*)\\s*(.*)");
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(match.Groups[1].Value, match.Groups[2].Value);
		process = new Process();
		process.StartInfo = processStartInfo;
		output = new StringBuilder();
		if (options.elevate)
		{
			options.useShellExecute = true;
			processStartInfo.Verb = "runas";
			if (options.currentDirectory == null)
			{
				options.currentDirectory = Environment.CurrentDirectory;
			}
		}
		processStartInfo.CreateNoWindow = options.noWindow;
		if (options.useShellExecute)
		{
			processStartInfo.UseShellExecute = true;
			if (options.noWindow)
			{
				processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
		}
		else
		{
			if (options.input != null)
			{
				processStartInfo.RedirectStandardInput = true;
			}
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardError = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.ErrorDialog = false;
			processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			processStartInfo.CreateNoWindow = true;
			process.OutputDataReceived += OnProcessOutput;
			process.ErrorDataReceived += OnProcessOutput;
		}
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
		outputStream = options.outputStream;
		if (options.outputFile != null)
		{
			outputStream = File.CreateText(options.outputFile);
		}
		try
		{
			process.Start();
		}
		catch (Exception ex)
		{
			string text3 = "Failure starting Process\r\n    Exception: " + ex.Message + "\r\n    Cmd: " + commandLine + "\r\n";
			if (Regex.IsMatch(processStartInfo.FileName, "^(copy|dir|del|color|set|cd|cdir|md|mkdir|prompt|pushd|popd|start|assoc|ftype)", RegexOptions.IgnoreCase))
			{
				text3 = text3 + "    Cmd " + processStartInfo.FileName + " implemented by Cmd.exe, fix by prefixing with 'cmd /c'.";
			}
			throw new ApplicationException(text3, ex);
		}
		if (!processStartInfo.UseShellExecute)
		{
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		}
		if (options.input != null)
		{
			process.StandardInput.Write(options.input);
			process.StandardInput.Close();
		}
	}

	/// <summary>
	/// Create a subprocess to run 'commandLine' with no special options. 
	/// <param variable="commandLine">The command lineNumber to run as a subprocess</param>
	/// </summary>
	internal Command(string commandLine)
		: this(commandLine, new CommandOptions())
	{
	}

	/// <summary>
	/// Wait for a started process to complete (HasExited will be true on return)
	/// </summary>
	/// <returns>Wait returns that 'this' pointer.</returns>
	internal Command Wait()
	{
		if (options.noWait)
		{
			return this;
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			process.WaitForExit(options.timeoutMSec);
			flag = true;
			for (int i = 0; i < 10; i++)
			{
				Thread.Sleep(1);
			}
		}
		finally
		{
			if (!process.HasExited)
			{
				flag2 = true;
				Kill();
			}
		}
		if (outputStream != null && options.outputFile != null)
		{
			outputStream.Close();
		}
		outputStream = null;
		if (flag && flag2)
		{
			throw new Exception("Timeout of " + options.timeoutMSec / 1000 + " sec exceeded\r\n    Cmd: " + commandLine);
		}
		if (process.ExitCode != 0 && !options.noThrow)
		{
			ThrowCommandFailure(null);
		}
		return this;
	}

	/// <summary>
	/// Throw a error if the command exited with a non-zero exit code
	/// printing useful diagnostic information along with the thrown message.
	/// This is useful when NoThrow is specified, and after post-processing
	/// you determine that the command really did fail, and an normal 
	/// Command.Run failure was the appropriate action.  
	/// </summary>
	/// <param name="message">An additional message to print in the throw (can be null)</param>
	internal void ThrowCommandFailure(string message)
	{
		if (process.ExitCode != 0)
		{
			string text = "";
			if (outputStream == null)
			{
				string text2 = output.ToString();
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
			throw new Exception(message + "Process returned exit code 0x" + process.ExitCode.ToString("x") + "\r\n  Cmd: " + commandLine + text);
		}
	}

	/// <summary>
	/// Kill the process (and any child processses (recursively) associated with the 
	/// running command).   Note that it may not be able to kill everything it should
	/// if the child-parent' chain is broken by a child that creates a subprocess and
	/// then dies itself.   This is reasonably uncommon, however. 
	/// </summary>
	internal void Kill()
	{
		Console.WriteLine("Killing process tree " + Id + " Cmd: " + commandLine);
		try
		{
			Run("taskkill /f /t /pid " + process.Id);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		int num = 0;
		do
		{
			Thread.Sleep(10);
			num++;
			if (num > 100)
			{
				Console.WriteLine("ERROR: process is not dead 1 sec after killing " + process.Id);
				Console.WriteLine("Cmd: " + commandLine);
			}
		}
		while (!process.HasExited);
		if (outputStream != null && options.outputFile != null)
		{
			outputStream.Close();
		}
		outputStream = null;
	}

	/// <summary>
	/// Put double quotes around 'str' if necessary (handles quotes quotes.  
	/// </summary>
	internal static string Quote(string str)
	{
		if (str.IndexOf('"') < 0)
		{
			str = Regex.Replace(str, "\\*\"", "\\$1");
		}
		return "\"" + str + "\"";
	}

	/// <summary>
	/// Given a string 'commandExe' look for it on the path the way cmd.exe would.   
	/// Returns null if it was not found.   
	/// </summary>
	internal static string FindOnPath(string commandExe)
	{
		string text = ProbeForExe(commandExe);
		if (text != null)
		{
			return text;
		}
		if (!commandExe.Contains("\\"))
		{
			string[] array = Paths;
			for (int i = 0; i < array.Length; i++)
			{
				text = ProbeForExe(Path.Combine(array[i], commandExe));
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
		string[] array = PathExts;
		foreach (string text in array)
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
		if (outputStream != null)
		{
			outputStream.WriteLine(e.Data);
		}
		else
		{
			output.AppendLine(e.Data);
		}
	}
}
