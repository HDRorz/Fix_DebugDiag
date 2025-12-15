using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.Linux;

internal class LinuxDefaultSymbolLocator : DefaultSymbolLocator
{
	private IEnumerable<string> _modules;

	public LinuxDefaultSymbolLocator(IEnumerable<string> modules)
	{
		_modules = modules;
	}

	public override string FindBinary(string fileName, int buildTimeStamp, int imageSize, bool checkProperties = true)
	{
		string fileName2 = Path.GetFileName(fileName);
		foreach (string module in _modules)
		{
			if (fileName2 == Path.GetFileName(module))
			{
				return module;
			}
			string text = Path.Combine(Path.GetDirectoryName(module), fileName2);
			if (File.Exists(text))
			{
				return text;
			}
		}
		return base.FindBinary(fileName, buildTimeStamp, imageSize, checkProperties);
	}
}
