using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Desktop;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Native;

namespace Microsoft.Diagnostics.Runtime;

internal class DataTargetImpl : DataTarget
{
	private IDataReader _dataReader;

	private IDebugClient _client;

	private ClrInfo[] _versions;

	private Architecture _architecture;

	private ModuleInfo[] _modules;

	public override IDataReader DataReader => _dataReader;

	public override bool IsMinidump => _dataReader.IsMinidump;

	public override Architecture Architecture => _architecture;

	public override uint PointerSize => _dataReader.GetPointerSize();

	public override IList<ClrInfo> ClrVersions
	{
		get
		{
			if (_versions != null)
			{
				return _versions;
			}
			List<ClrInfo> list = new List<ClrInfo>();
			foreach (ModuleInfo item in EnumerateModules())
			{
				string text = Path.GetFileNameWithoutExtension(item.FileName).ToLower();
				if (!(text != "clr") || !(text != "mscorwks") || !(text != "coreclr") || !(text != "mrt100_app"))
				{
					string text2 = Path.Combine(Path.GetDirectoryName(item.FileName), "mscordacwks.dll");
					if (!File.Exists(text2) || !NativeMethods.IsEqualFileVersion(item.FileName, item.Version))
					{
						text2 = null;
					}
					ClrFlavor flavor = ((text == "mrt100_app") ? ClrFlavor.Redhawk : ((text == "coreclr") ? ClrFlavor.CoreCLR : ClrFlavor.Desktop));
					VersionInfo version = item.Version;
					string dacRequestFileName = DacInfo.GetDacRequestFileName(flavor, Architecture, Architecture, version);
					string dacRequestFileName2 = DacInfo.GetDacRequestFileName(flavor, (IntPtr.Size == 4) ? Architecture.X86 : Architecture.Amd64, Architecture, version);
					DacInfo dacInfo = new DacInfo(_dataReader, dacRequestFileName, Architecture);
					dacInfo.FileSize = item.FileSize;
					dacInfo.TimeStamp = item.TimeStamp;
					dacInfo.FileName = dacRequestFileName2;
					list.Add(new ClrInfo(this, flavor, item, dacInfo, text2));
				}
			}
			_versions = list.ToArray();
			Array.Sort(_versions);
			return _versions;
		}
	}

	public override IDebugClient DebuggerInterface => _client;

	public DataTargetImpl(IDataReader dataReader, IDebugClient client)
	{
		if (dataReader == null)
		{
			throw new ArgumentNullException("dataReader");
		}
		_dataReader = dataReader;
		_client = client;
		_architecture = _dataReader.GetArchitecture();
	}

	public override bool ReadProcessMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		return _dataReader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	public override ClrRuntime CreateRuntime(string dacFilename)
	{
		if (IntPtr.Size != (int)_dataReader.GetPointerSize())
		{
			throw new InvalidOperationException("Mismatched architecture between this process and the dac.");
		}
		if (string.IsNullOrEmpty(dacFilename))
		{
			throw new ArgumentNullException("dacFilename");
		}
		if (!File.Exists(dacFilename))
		{
			throw new FileNotFoundException(dacFilename);
		}
		DacLibrary lib = new DacLibrary(this, dacFilename);
		string text = Path.GetFileNameWithoutExtension(dacFilename).ToLower();
		bool flag = text.Contains("mscordaccore");
		bool flag2 = text.Contains("mrt100dac");
		NativeMethods.GetFileVersion(dacFilename, out var major, out var minor, out var _, out var patch);
		if (flag)
		{
			return new V45Runtime(this, lib);
		}
		if (flag2)
		{
			return new NativeRuntime(this, lib);
		}
		DesktopVersion version;
		if (major == 2)
		{
			version = DesktopVersion.v2;
		}
		else
		{
			if (major != 4 || minor != 0 || patch >= 10000)
			{
				return new V45Runtime(this, lib);
			}
			version = DesktopVersion.v4;
		}
		return new LegacyRuntime(this, lib, version, patch);
	}

	public override ClrRuntime CreateRuntime(object clrDataProcess)
	{
		DacLibrary dacLibrary = new DacLibrary(this, (IXCLRDataProcess)clrDataProcess);
		if (clrDataProcess is ISOSDac)
		{
			return new V45Runtime(this, dacLibrary);
		}
		byte[] array = new byte[Marshal.SizeOf(typeof(V2HeapDetails))];
		if (dacLibrary.DacInterface.Request(4026531885u, 0u, null, (uint)array.Length, array) == -2147024809)
		{
			return new LegacyRuntime(this, dacLibrary, DesktopVersion.v4, 10000);
		}
		return new LegacyRuntime(this, dacLibrary, DesktopVersion.v2, 3054);
	}

	public override IEnumerable<ModuleInfo> EnumerateModules()
	{
		if (_modules == null)
		{
			InitModules();
		}
		return _modules;
	}

	internal override string ResolveSymbol(ulong addr)
	{
		ModuleInfo moduleInfo = FindModule(addr);
		if (moduleInfo == null)
		{
			return null;
		}
		return base.SymbolLocator.LoadPdb(moduleInfo)?.FindNameForRva((uint)(addr - moduleInfo.ImageBase));
	}

	private ModuleInfo FindModule(ulong addr)
	{
		if (_modules == null)
		{
			InitModules();
		}
		ModuleInfo[] modules = _modules;
		foreach (ModuleInfo moduleInfo in modules)
		{
			if (moduleInfo.ImageBase <= addr && addr < moduleInfo.ImageBase + moduleInfo.FileSize)
			{
				return moduleInfo;
			}
		}
		return null;
	}

	private void InitModules()
	{
		if (_modules == null)
		{
			List<ModuleInfo> list = new List<ModuleInfo>(_dataReader.EnumerateModules());
			list.Sort((ModuleInfo a, ModuleInfo b) => a.ImageBase.CompareTo(b.ImageBase));
			_modules = list.ToArray();
		}
	}

	public override void Dispose()
	{
		_dataReader.Close();
	}
}
