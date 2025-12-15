using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.ICorDebug;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class FileLoader : ICLRDebuggingLibraryProvider
{
	private readonly Dictionary<string, PEImage> _pefileCache = new Dictionary<string, PEImage>(StringComparer.OrdinalIgnoreCase);

	private readonly DataTarget _dataTarget;

	public FileLoader(DataTarget dt)
	{
		_dataTarget = dt;
	}

	public PEImage LoadPEImage(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return null;
		}
		if (_pefileCache.TryGetValue(fileName, out var value))
		{
			return value;
		}
		value = new PEImage(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
		if (!value.IsValid)
		{
			value = null;
		}
		_pefileCache[fileName] = value;
		return value;
	}

	public int ProvideLibrary([In][MarshalAs(UnmanagedType.LPWStr)] string fileName, int timestamp, int sizeOfImage, out IntPtr hModule)
	{
		string text = _dataTarget.SymbolLocator.FindBinary(fileName, timestamp, sizeOfImage, checkProperties: false);
		if (text == null)
		{
			hModule = IntPtr.Zero;
			return -1;
		}
		hModule = WindowsFunctions.NativeMethods.LoadLibrary(text);
		return 0;
	}
}
