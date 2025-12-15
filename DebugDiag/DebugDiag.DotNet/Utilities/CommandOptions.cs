using System;
using System.Collections.Generic;
using System.IO;

namespace Utilities;

/// <summary>
/// CommandOptions is a helper class for the Command class.  It stores options
/// that affect the behavior of the execution of ETWCommands and is passes as a 
/// parapeter to the constuctor of a Command.  
///
/// It is useful for these options be be on a separate class (rather than 
/// on Command itself), because it is reasonably common to want to have a set
/// of options passed to several commands, which is not easily possible otherwise. 
/// </summary>
internal sealed class CommandOptions
{
	/// <summary>
	/// Can be assigned to the Timeout Property to indicate infinite timeout. 
	/// </summary>
	internal const int Infinite = -1;

	internal bool noThrow;

	internal bool useShellExecute;

	internal bool noWindow;

	internal bool noWait;

	internal bool elevate;

	internal int timeoutMSec;

	internal string input;

	internal string outputFile;

	internal TextWriter outputStream;

	internal string currentDirectory;

	internal Dictionary<string, string> environmentVariables;

	/// <summary>
	/// Normally commands will throw if the subprocess returns a non-zero 
	/// exit code.  NoThrow suppresses this. 
	/// </summary>
	internal bool NoThrow
	{
		get
		{
			return noThrow;
		}
		set
		{
			noThrow = value;
		}
	}

	/// <summary>
	/// ShortHand for UseShellExecute and NoWait
	/// </summary>
	internal bool Start
	{
		get
		{
			return useShellExecute;
		}
		set
		{
			useShellExecute = value;
			noWait = value;
		}
	}

	/// <summary>
	/// Normally commands are launched with CreateProcess.  However it is
	/// also possible use the Shell Start API.  This causes Command to look
	/// up the executable differently 
	/// </summary>
	internal bool UseShellExecute
	{
		get
		{
			return useShellExecute;
		}
		set
		{
			useShellExecute = value;
		}
	}

	/// <summary>
	/// Indicates that you want to hide any new window created.  
	/// </summary>
	internal bool NoWindow
	{
		get
		{
			return noWindow;
		}
		set
		{
			noWindow = value;
		}
	}

	/// <summary>
	/// Indicates that you want don't want to wait for the command to complete.
	/// </summary>
	internal bool NoWait
	{
		get
		{
			return noWait;
		}
		set
		{
			noWait = value;
		}
	}

	/// <summary>
	/// Indicates that the command must run at elevated Windows privledges (causes a new command window)
	/// </summary>
	internal bool Elevate
	{
		get
		{
			return elevate;
		}
		set
		{
			elevate = value;
		}
	}

	/// <summary>
	/// By default commands have a 10 minute timeout (600,000 msec), If this
	/// is inappropriate, the Timeout property can change this.  Like all
	/// timouts in .NET, it is in units of milliseconds, and you can use
	/// CommandOptions.Infinite to indicate no timeout. 
	/// </summary>
	internal int Timeout
	{
		get
		{
			return timeoutMSec;
		}
		set
		{
			timeoutMSec = value;
		}
	}

	/// <summary>
	/// Indicates the string will be sent to Console.In for the subprocess.  
	/// </summary>
	internal string Input
	{
		get
		{
			return input;
		}
		set
		{
			input = value;
		}
	}

	/// <summary>
	/// Indicates the current directory the subProcess will have. 
	/// </summary>
	internal string CurrentDirectory
	{
		get
		{
			return currentDirectory;
		}
		set
		{
			currentDirectory = value;
		}
	}

	/// <summary>
	/// Indicates the standard output and error of the command should be redirected
	/// to a archiveFile rather than being stored in Memory in the 'Output' property of the
	/// command.
	/// </summary>
	internal string OutputFile
	{
		get
		{
			return outputFile;
		}
		set
		{
			if (outputStream != null)
			{
				throw new Exception("OutputFile and OutputStream can not both be set");
			}
			outputFile = value;
		}
	}

	/// <summary>
	/// Indicates the standard output and error of the command should be redirected
	/// to a a TextWriter rather than being stored in Memory in the 'Output' property 
	/// of the command.
	/// </summary>
	internal TextWriter OutputStream
	{
		get
		{
			return outputStream;
		}
		set
		{
			if (outputFile != null)
			{
				throw new Exception("OutputFile and OutputStream can not both be set");
			}
			outputStream = value;
		}
	}

	/// <summary>
	/// Gets the Environment variables that will be set in the subprocess that
	/// differ from current process's environment variables.  Any time a string
	/// of the form %VAR% is found in a value of a environment variable it is
	/// replaced with the value of the environment variable at the time the
	/// command is launched.  This is useful for example to update the PATH
	/// environment variable eg. "%PATH%;someNewPath"
	/// </summary>
	internal Dictionary<string, string> EnvironmentVariables
	{
		get
		{
			if (environmentVariables == null)
			{
				environmentVariables = new Dictionary<string, string>();
			}
			return environmentVariables;
		}
	}

	/// <summary>
	/// CommanOptions holds a set of options that can be passed to the constructor
	/// to the Command Class as well as Command.Run*
	/// </summary>
	internal CommandOptions()
	{
		timeoutMSec = 600000;
	}

	/// <summary>
	/// Return a copy an existing set of command options
	/// </summary>
	/// <returns>The copy of the command options</returns>
	internal CommandOptions Clone()
	{
		return (CommandOptions)MemberwiseClone();
	}

	/// <summary>
	/// Updates the NoThrow propery and returns the updated commandOptions.
	/// <returns>Updated command options</returns>
	/// </summary>
	internal CommandOptions AddNoThrow()
	{
		noThrow = true;
		return this;
	}

	/// <summary>
	/// Updates the Start propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddStart()
	{
		Start = true;
		return this;
	}

	/// <summary>
	/// Updates the Start propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddUseShellExecute()
	{
		useShellExecute = true;
		return this;
	}

	/// <summary>
	/// Updates the NoWindow propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddNoWindow()
	{
		noWindow = true;
		return this;
	}

	/// <summary>
	/// Updates the NoWait propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddNoWait()
	{
		noWait = true;
		return this;
	}

	/// <summary>
	/// Updates the Elevate propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddElevate()
	{
		elevate = true;
		return this;
	}

	/// <summary>
	/// Updates the Timeout propery and returns the updated commandOptions.
	/// CommandOptions.Infinite can be used for infinite
	/// </summary>
	internal CommandOptions AddTimeout(int milliseconds)
	{
		timeoutMSec = milliseconds;
		return this;
	}

	/// <summary>
	/// Updates the Input propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddInput(string input)
	{
		this.input = input;
		return this;
	}

	/// <summary>
	/// Updates the CurrentDirectory propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddCurrentDirectory(string directoryPath)
	{
		currentDirectory = directoryPath;
		return this;
	}

	/// <summary>
	/// Updates the OutputFile propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddOutputFile(string outputFile)
	{
		OutputFile = outputFile;
		return this;
	}

	/// <summary>
	/// Updates the OutputStream propery and returns the updated commandOptions.
	/// </summary>
	internal CommandOptions AddOutputStream(TextWriter outputStream)
	{
		OutputStream = outputStream;
		return this;
	}

	/// <summary>
	/// Adds the environment variable with the give value to the set of 
	/// environmetn variables to be passed to the sub-process and returns the 
	/// updated commandOptions.   Any time a string
	/// of the form %VAR% is found in a value of a environment variable it is
	/// replaced with the value of the environment variable at the time the
	/// command is launched.  This is useful for example to update the PATH
	/// environment variable eg. "%PATH%;someNewPath"
	/// </summary>
	internal CommandOptions AddEnvironmentVariable(string variable, string value)
	{
		EnvironmentVariables[variable] = value;
		return this;
	}
}
