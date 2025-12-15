using Dia2Lib;

namespace Microsoft.Diagnostics.Runtime;

public class SourceLocation
{
	private IDiaLineNumber _source;

	public string FilePath => _source.sourceFile.fileName;

	public int LineNumber => (int)_source.lineNumber;

	public int LineNumberEnd => (int)_source.lineNumberEnd;

	public int ColStart => (int)_source.columnNumber;

	public int ColEnd => (int)_source.columnNumberEnd;

	public override string ToString()
	{
		int lineNumber = LineNumber;
		int lineNumberEnd = LineNumberEnd;
		if (lineNumber == lineNumberEnd)
		{
			return $"{FilePath}:{lineNumber}";
		}
		return $"{FilePath}:{lineNumber}-{lineNumberEnd}";
	}

	internal SourceLocation(IDiaLineNumber source)
	{
		_source = source;
	}
}
