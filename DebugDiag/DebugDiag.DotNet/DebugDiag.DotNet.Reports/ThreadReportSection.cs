using System;
using System.IO;
using System.Text;

namespace DebugDiag.DotNet.Reports;

internal sealed class ThreadReportSection : ReportSection
{
	private MemoryStream _contentHeader;

	private bool _isDisposed;

	private Guid _threadHash = Guid.Empty;

	public Guid ThreadHash
	{
		get
		{
			return _threadHash;
		}
		set
		{
			_threadHash = value;
		}
	}

	public ThreadReportSection(string SectionID, bool IncludeInTOC)
		: base(SectionID, IncludeInTOC)
	{
		_type = SectionType.Thread;
	}

	/// <summary>
	/// This method is used to write the header information of the thread.   
	/// </summary>
	/// <param name="value">Content to write on the header</param>
	public void WriteHeader(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			if (_contentHeader == null)
			{
				_contentHeader = new MemoryStream();
			}
			byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(value);
			_contentHeader.Write(bytes, 0, bytes.Length);
		}
	}

	protected internal override MemoryStream RenderHTML()
	{
		MemoryStream memoryStream = new MemoryStream();
		MemoryStream content = _content;
		new MemoryStream();
		if (_contentHeader != null)
		{
			_contentHeader.Position = 0L;
			_contentHeader.CopyTo(memoryStream);
		}
		if (_content != null)
		{
			_content.Position = 0L;
			_content.CopyTo(memoryStream);
		}
		_content = memoryStream;
		MemoryStream result = base.RenderHTML();
		_content = content;
		memoryStream.Close();
		return result;
	}

	public MemoryStream RenderThreadHeaderHTML()
	{
		MemoryStream content = _content;
		_content = _contentHeader;
		new MemoryStream();
		MemoryStream result = base.RenderHTML();
		_content = content;
		return result;
	}

	public MemoryStream RenderThreadStackStringOnlyHTML()
	{
		string title = base.Title;
		base.Title = null;
		return base.RenderHTML();
	}

	/// <summary>
	/// Dispose Method that will clean up unmanaged resources used.
	/// </summary>
	/// <param name="disposing"></param>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!_isDisposed && disposing)
		{
			if (_contentHeader != null)
			{
				_contentHeader.Dispose();
			}
			_isDisposed = true;
		}
	}
}
