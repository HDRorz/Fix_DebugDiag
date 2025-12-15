using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

internal class DumpDataReader : IDataReader, IDisposable
{
	private string _fileName;

	private DumpReader _dumpReader;

	private List<ModuleInfo> _modules;

	private string _generatedPath;

	private byte[] _ptrBuffer = new byte[IntPtr.Size];

	public bool IsMinidump => _dumpReader.IsMinidump;

	public bool CanReadAsync => true;

	public DumpDataReader(string file)
	{
		if (!File.Exists(file))
		{
			throw new FileNotFoundException(file);
		}
		if (Path.GetExtension(file).ToLower() == ".cab")
		{
			file = ExtractCab(file);
		}
		_fileName = file;
		_dumpReader = new DumpReader(file);
	}

	~DumpDataReader()
	{
		Dispose();
	}

	private string ExtractCab(string file)
	{
		_generatedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		while (Directory.Exists(_generatedPath))
		{
			_generatedPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		}
		Directory.CreateDirectory(_generatedPath);
		CommandOptions commandOptions = new CommandOptions();
		commandOptions.NoThrow = true;
		commandOptions.NoWindow = true;
		Command command = Command.Run($"expand -F:*dmp {file} {_generatedPath}", commandOptions);
		bool flag = false;
		if (command.ExitCode != 0)
		{
			flag = true;
		}
		else
		{
			file = null;
			string[] files = Directory.GetFiles(_generatedPath);
			foreach (string text in files)
			{
				switch (Path.GetExtension(text).ToLower())
				{
				case ".dll":
				case ".pdb":
				case ".exe":
					continue;
				}
				file = text;
				break;
			}
			flag = flag || file == null;
		}
		if (flag)
		{
			Dispose();
			throw new IOException("Failed to extract a crash dump from " + file);
		}
		return file;
	}

	public override string ToString()
	{
		return _fileName;
	}

	public void Close()
	{
		_dumpReader.Dispose();
		Dispose();
	}

	public void Dispose()
	{
		if (_generatedPath == null)
		{
			return;
		}
		try
		{
			string[] files = Directory.GetFiles(_generatedPath);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
			Directory.Delete(_generatedPath, recursive: false);
		}
		catch
		{
		}
		_generatedPath = null;
	}

	public void Flush()
	{
		_modules = null;
	}

	public Architecture GetArchitecture()
	{
		return _dumpReader.ProcessorArchitecture switch
		{
			ProcessorArchitecture.PROCESSOR_ARCHITECTURE_ARM => Architecture.Arm, 
			ProcessorArchitecture.PROCESSOR_ARCHITECTURE_AMD64 => Architecture.Amd64, 
			ProcessorArchitecture.PROCESSOR_ARCHITECTURE_INTEL => Architecture.X86, 
			_ => Architecture.Unknown, 
		};
	}

	public uint GetPointerSize()
	{
		Architecture architecture = GetArchitecture();
		if (architecture == Architecture.Amd64)
		{
			return 8u;
		}
		return 4u;
	}

	public IList<ModuleInfo> EnumerateModules()
	{
		if (_modules != null)
		{
			return _modules;
		}
		List<ModuleInfo> list = new List<ModuleInfo>();
		foreach (DumpModule item in _dumpReader.EnumerateModules())
		{
			DumpReader.DumpNative.MINIDUMP_MODULE raw = item.Raw;
			ModuleInfo moduleInfo = new ModuleInfo(this);
			moduleInfo.FileName = item.FullName;
			moduleInfo.ImageBase = raw.BaseOfImage;
			moduleInfo.FileSize = raw.SizeOfImage;
			moduleInfo.TimeStamp = raw.TimeDateStamp;
			moduleInfo.Version = GetVersionInfo(item);
			list.Add(moduleInfo);
		}
		_modules = list;
		return list;
	}

	public void GetVersionInfo(ulong baseAddress, out VersionInfo version)
	{
		DumpModule dumpModule = _dumpReader.TryLookupModuleByAddress(baseAddress);
		version = ((dumpModule != null) ? GetVersionInfo(dumpModule) : default(VersionInfo));
	}

	private static VersionInfo GetVersionInfo(DumpModule module)
	{
		DumpReader.DumpNative.VS_FIXEDFILEINFO versionInfo = module.Raw.VersionInfo;
		int minor = (ushort)versionInfo.dwFileVersionMS;
		int major = (ushort)(versionInfo.dwFileVersionMS >> 16);
		int patch = (ushort)versionInfo.dwFileVersionLS;
		int revision = (ushort)(versionInfo.dwFileVersionLS >> 16);
		return new VersionInfo(major, minor, revision, patch);
	}

	public ulong ReadPointerUnsafe(ulong addr)
	{
		return _dumpReader.ReadPointerUnsafe(addr);
	}

	public uint ReadDwordUnsafe(ulong addr)
	{
		return _dumpReader.ReadDwordUnsafe(addr);
	}

	public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		bytesRead = _dumpReader.ReadPartialMemory(address, buffer, bytesRequested);
		return bytesRead > 0;
	}

	public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		bytesRead = (int)_dumpReader.ReadPartialMemory(address, buffer, (uint)bytesRequested);
		return bytesRead > 0;
	}

	public ulong GetThreadTeb(uint id)
	{
		return _dumpReader.GetThread((int)id)?.Teb ?? 0;
	}

	public IEnumerable<uint> EnumerateAllThreads()
	{
		foreach (DumpThread item in _dumpReader.EnumerateThreads())
		{
			yield return (uint)item.ThreadId;
		}
	}

	public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
	{
		return _dumpReader.VirtualQuery(addr, out vq);
	}

	public bool GetThreadContext(uint id, uint contextFlags, uint contextSize, IntPtr context)
	{
		DumpThread thread = _dumpReader.GetThread((int)id);
		if (thread == null)
		{
			return false;
		}
		thread.GetThreadContext(context, (int)contextSize);
		return true;
	}

	public AsyncMemoryReadResult ReadMemoryAsync(ulong address, int bytesRequested)
	{
		AsyncMemoryReadResult asyncMemoryReadResult = new AsyncMemoryReadResult(address, bytesRequested);
		ThreadPool.QueueUserWorkItem(QueueMemoryRead, asyncMemoryReadResult);
		return asyncMemoryReadResult;
	}

	private void QueueMemoryRead(object state)
	{
		_dumpReader.ReadMemory((AsyncMemoryReadResult)state);
	}

	public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
	{
		throw new NotImplementedException();
	}
}
