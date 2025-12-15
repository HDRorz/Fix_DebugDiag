using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

internal sealed class OSXFunctions : PlatformFunctions
{
	private const string LibDl = "libdl.dylib";

	private const int RTLD_NOW = 2;

	internal override bool GetFileVersion(string dll, out int major, out int minor, out int revision, out int patch)
	{
		major = (minor = (revision = (patch = 0)));
		return false;
	}

	public override bool TryGetWow64(IntPtr proc, out bool result)
	{
		result = false;
		return true;
	}

	public override IntPtr LoadLibrary(string filename)
	{
		return dlopen(filename, 2);
	}

	public override bool FreeLibrary(IntPtr module)
	{
		return dlclose(module) == 0;
	}

	public override IntPtr GetProcAddress(IntPtr module, string method)
	{
		return dlsym(module, method);
	}

	[DllImport("libdl.dylib")]
	private static extern IntPtr dlopen(string filename, int flags);

	[DllImport("libdl.dylib")]
	private static extern int dlclose(IntPtr module);

	[DllImport("libdl.dylib")]
	private static extern IntPtr dlsym(IntPtr handle, string symbol);
}
