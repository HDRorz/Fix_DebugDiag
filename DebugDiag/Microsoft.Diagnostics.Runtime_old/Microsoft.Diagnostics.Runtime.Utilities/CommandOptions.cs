using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public sealed class CommandOptions
{
	public const int Infinite = -1;

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

	public bool NoThrow
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

	public bool Start
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

	public bool UseShellExecute
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

	public bool NoWindow
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

	public bool NoWait
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

	public bool Elevate
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

	public int Timeout
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

	public string Input
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

	public string CurrentDirectory
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

	public string OutputFile
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

	public TextWriter OutputStream
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

	public Dictionary<string, string> EnvironmentVariables
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

	public CommandOptions()
	{
		timeoutMSec = 600000;
	}

	public CommandOptions Clone()
	{
		return (CommandOptions)MemberwiseClone();
	}

	public CommandOptions AddNoThrow()
	{
		noThrow = true;
		return this;
	}

	public CommandOptions AddStart()
	{
		Start = true;
		return this;
	}

	public CommandOptions AddUseShellExecute()
	{
		useShellExecute = true;
		return this;
	}

	public CommandOptions AddNoWindow()
	{
		noWindow = true;
		return this;
	}

	public CommandOptions AddNoWait()
	{
		noWait = true;
		return this;
	}

	public CommandOptions AddElevate()
	{
		elevate = true;
		return this;
	}

	public CommandOptions AddTimeout(int milliseconds)
	{
		timeoutMSec = milliseconds;
		return this;
	}

	public CommandOptions AddInput(string input)
	{
		this.input = input;
		return this;
	}

	public CommandOptions AddCurrentDirectory(string directoryPath)
	{
		currentDirectory = directoryPath;
		return this;
	}

	public CommandOptions AddOutputFile(string outputFile)
	{
		OutputFile = outputFile;
		return this;
	}

	public CommandOptions AddOutputStream(TextWriter outputStream)
	{
		OutputStream = outputStream;
		return this;
	}

	public CommandOptions AddEnvironmentVariable(string variable, string value)
	{
		EnvironmentVariables[variable] = value;
		return this;
	}
}
