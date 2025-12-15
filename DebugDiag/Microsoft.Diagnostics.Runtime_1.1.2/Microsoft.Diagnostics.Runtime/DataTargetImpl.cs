using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime;

internal class DataTargetImpl : DataTarget
{
	private readonly IDataReader _dataReader;

	private uint? _pid;

	private ClrInfo[] _versions;

	private readonly Lazy<ModuleInfo[]> _modules;

	private readonly List<DacLibrary> _dacLibraries = new List<DacLibrary>(2);

	private static readonly Regex s_invalidChars = new Regex("[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]");

	public override uint ProcessId
	{
		get
		{
			if (!_pid.HasValue)
			{
				if (_dataReader is IDataReader2 dataReader)
				{
					_pid = dataReader.ProcessId;
				}
				else
				{
					_pid = uint.MaxValue;
				}
			}
			return _pid.Value;
		}
	}

	public override IDataReader DataReader => _dataReader;

	public override bool IsMinidump => _dataReader.IsMinidump;

	public override Architecture Architecture { get; }

	public override uint PointerSize => _dataReader.GetPointerSize();

	public override IList<ClrInfo> ClrVersions
	{
		get
		{
			if (_versions == null)
			{
				_versions = InitVersions();
			}
			return _versions;
		}
	}

	public override IDebugClient DebuggerInterface { get; }

	public DataTargetImpl(IDataReader dataReader, IDebugClient client)
	{
		_dataReader = dataReader ?? throw new ArgumentNullException("dataReader");
		DebuggerInterface = client;
		Architecture = _dataReader.GetArchitecture();
		_modules = new Lazy<ModuleInfo[]>(InitModules);
	}

	public override bool ReadProcessMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		return _dataReader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	public override IEnumerable<ModuleInfo> EnumerateModules()
	{
		return _modules.Value;
	}

	private ModuleInfo[] InitModules()
	{
		List<ModuleInfo> list = new List<ModuleInfo>(from m in _dataReader.EnumerateModules()
			where !s_invalidChars.IsMatch(m.FileName)
			select m);
		list.Sort((ModuleInfo a, ModuleInfo b) => a.ImageBase.CompareTo(b.ImageBase));
		return list.ToArray();
	}

	private ClrInfo[] InitVersions()
	{
		List<ClrInfo> list = new List<ClrInfo>();
		foreach (ModuleInfo item in EnumerateModules())
		{
			if (!ClrInfoProvider.IsSupportedRuntime(item, out var flavor, out var platform))
			{
				continue;
			}
			string dacFileName = ClrInfoProvider.GetDacFileName(flavor, platform);
			string text = Path.Combine(Path.GetDirectoryName(item.FileName), dacFileName);
			if (platform == Platform.Linux)
			{
				if (File.Exists(text))
				{
					int id = Process.GetCurrentProcess().Id;
					string text2 = Path.Combine(Path.GetTempPath(), "clrmd" + id);
					Directory.CreateDirectory(text2);
					string text3 = Path.Combine(text2, dacFileName);
					if (LinuxFunctions.symlink(text, text3) == 0)
					{
						text = text3;
					}
				}
				else
				{
					text = dacFileName;
				}
			}
			else if (!File.Exists(text) || !DataTarget.PlatformFunctions.IsEqualFileVersion(text, item.Version))
			{
				text = null;
			}
			VersionInfo version = item.Version;
			string dacRequestFileName = ClrInfoProvider.GetDacRequestFileName(flavor, Architecture, Architecture, version, platform);
			string dacRequestFileName2 = ClrInfoProvider.GetDacRequestFileName(flavor, (IntPtr.Size == 4) ? Architecture.X86 : Architecture.Amd64, Architecture, version, platform);
			DacInfo dacInfo = new DacInfo(_dataReader, dacRequestFileName, Architecture)
			{
				FileSize = item.FileSize,
				TimeStamp = item.TimeStamp,
				FileName = dacRequestFileName2,
				Version = item.Version
			};
			list.Add(new ClrInfo(this, flavor, item, dacInfo, text));
		}
		ClrInfo[] array = list.ToArray();
		Array.Sort(array);
		return array;
	}

	public override void Dispose()
	{
		_dataReader.Close();
		foreach (DacLibrary dacLibrary in _dacLibraries)
		{
			dacLibrary.Dispose();
		}
	}

	protected internal override void AddDacLibrary(DacLibrary dacLibrary)
	{
		_dacLibraries.Add(dacLibrary);
	}
}
