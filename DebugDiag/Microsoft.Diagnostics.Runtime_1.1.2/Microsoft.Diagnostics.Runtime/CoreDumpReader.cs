using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Linux;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

internal class CoreDumpReader : IDataReader2, IDataReader
{
	private class CoreModuleInfo : ModuleInfo
	{
		internal readonly ElfFile _elfFile;

		private readonly PEImage _pe;

		public CoreModuleInfo(IDataReader reader, ElfLoadedImage image)
			: base(reader, null)
		{
			_elfFile = image.Open();
			uint fileSize = (uint)image.Size;
			uint timeStamp = 0u;
			if (_elfFile == null)
			{
				PEImage pEImage = image.OpenAsPEImage();
				if (pEImage.IsValid)
				{
					_pe = pEImage;
					fileSize = (uint)pEImage.IndexFileSize;
					timeStamp = (uint)pEImage.IndexTimeStamp;
				}
			}
			else
			{
				base.BuildId = _elfFile.BuildId;
				_initialized = true;
			}
			FileName = image.Path;
			ImageBase = (ulong)image.BaseAddress;
			FileSize = fileSize;
			TimeStamp = timeStamp;
		}

		protected override void InitVersion(out VersionInfo version)
		{
			if (_elfFile != null)
			{
				LinuxFunctions.GetVersionInfo(_dataReader, ImageBase, _elfFile, out version);
				return;
			}
			version = default(VersionInfo);
			if (_pe == null)
			{
				return;
			}
			FileVersionInfo fileVersionInfo = _pe.GetFileVersionInfo();
			if (fileVersionInfo != null)
			{
				version = fileVersionInfo.VersionInfo;
				return;
			}
			PEImage pEImage = GetPEImage();
			if (pEImage != null && pEImage.IsValid)
			{
				fileVersionInfo = pEImage.GetFileVersionInfo();
				if (fileVersionInfo != null)
				{
					version = fileVersionInfo.VersionInfo;
				}
			}
		}
	}

	private readonly string _source;

	private readonly Stream _stream;

	private readonly ElfCoreFile _core;

	private readonly int _pointerSize;

	private readonly Architecture _architecture;

	private Dictionary<uint, IElfPRStatus> _threads;

	private List<ModuleInfo> _modules;

	private readonly byte[] _buffer = new byte[512];

	public bool IsMinidump => false;

	public uint ProcessId
	{
		get
		{
			using (IEnumerator<IElfPRStatus> enumerator = _core.EnumeratePRStatus().GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.ProcessId;
				}
			}
			return uint.MaxValue;
		}
	}

	public CoreDumpReader(string filename)
	{
		_source = filename;
		_stream = File.OpenRead(filename);
		_core = new ElfCoreFile(_stream);
		ElfMachine architecture = _core.ElfFile.Header.Architecture;
		switch (architecture)
		{
		case ElfMachine.EM_X86_64:
			_pointerSize = 8;
			_architecture = Architecture.Amd64;
			break;
		case ElfMachine.EM_386:
			_pointerSize = 4;
			_architecture = Architecture.X86;
			break;
		case ElfMachine.EM_AARCH64:
			_pointerSize = 8;
			_architecture = Architecture.Arm64;
			break;
		case ElfMachine.EM_ARM:
			_pointerSize = 4;
			_architecture = Architecture.Arm;
			break;
		default:
			throw new NotImplementedException($"Support for {architecture} not yet implemented.");
		}
	}

	public void Close()
	{
	}

	public IEnumerable<uint> EnumerateAllThreads()
	{
		InitThreads();
		return _threads.Keys;
	}

	public IList<ModuleInfo> EnumerateModules()
	{
		if (_modules == null)
		{
			ulong auxvValue = _core.GetAuxvValue(ElfAuxvType.Base);
			_modules = new List<ModuleInfo>(_core.LoadedImages.Count);
			foreach (ElfLoadedImage loadedImage in _core.LoadedImages)
			{
				if (loadedImage.BaseAddress != (long)auxvValue && !loadedImage.Path.StartsWith("/dev/"))
				{
					_modules.Add(new CoreModuleInfo(this, loadedImage));
				}
			}
		}
		return _modules;
	}

	public void Flush()
	{
		_threads = null;
		_modules = null;
	}

	public Architecture GetArchitecture()
	{
		return _architecture;
	}

	public uint GetPointerSize()
	{
		return (uint)_pointerSize;
	}

	public unsafe bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		InitThreads();
		if (_threads.TryGetValue(threadID, out var value))
		{
			return value.CopyContext(contextFlags, contextSize, context.ToPointer());
		}
		return false;
	}

	public unsafe bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
	{
		InitThreads();
		if (_threads.TryGetValue(threadID, out var value))
		{
			fixed (byte* context2 = context)
			{
				return value.CopyContext(contextFlags, contextSize, context2);
			}
		}
		return false;
	}

	public ulong GetThreadTeb(uint thread)
	{
		throw new NotImplementedException();
	}

	public void GetVersionInfo(ulong baseAddress, out VersionInfo version)
	{
		version = default(VersionInfo);
		ModuleInfo moduleInfo = EnumerateModules().FirstOrDefault((ModuleInfo module) => module.ImageBase == baseAddress);
		if (moduleInfo != null)
		{
			version = moduleInfo.Version;
		}
	}

	private long GetElfAddress(ulong addr)
	{
		if (_pointerSize == 4)
		{
			return (long)(addr & 0xFFFFFFFFu);
		}
		return (long)addr;
	}

	public uint ReadDwordUnsafe(ulong addr)
	{
		if (_core.ReadMemory(GetElfAddress(addr), _buffer, 4) == 4)
		{
			return BitConverter.ToUInt32(_buffer, 0);
		}
		return 0u;
	}

	public bool ReadMemory(ulong addr, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		bytesRead = _core.ReadMemory(GetElfAddress(addr), buffer, bytesRequested);
		return bytesRead > 0;
	}

	public bool ReadMemory(ulong address, IntPtr ptr, int bytesRequested, out int bytesRead)
	{
		byte[] array = _buffer;
		if (bytesRequested > array.Length)
		{
			array = new byte[bytesRequested];
		}
		bool num = ReadMemory(address, array, bytesRequested, out bytesRead);
		if (num)
		{
			Marshal.Copy(array, 0, ptr, bytesRead);
		}
		return num;
	}

	public ulong ReadPointerUnsafe(ulong addr)
	{
		if (_core.ReadMemory(GetElfAddress(addr), _buffer, _pointerSize) == _pointerSize)
		{
			if (_pointerSize == 8)
			{
				return BitConverter.ToUInt64(_buffer, 0);
			}
			if (_pointerSize == 4)
			{
				return BitConverter.ToUInt32(_buffer, 0);
			}
		}
		return 0uL;
	}

	public bool VirtualQuery(ulong address, out VirtualQueryData vq)
	{
		long elfAddress = GetElfAddress(address);
		foreach (ElfProgramHeader programHeader in _core.ElfFile.ProgramHeaders)
		{
			long virtualAddress = programHeader.VirtualAddress;
			long num = virtualAddress + programHeader.VirtualSize;
			if (virtualAddress <= elfAddress && elfAddress < num)
			{
				vq = new VirtualQueryData((ulong)virtualAddress, (ulong)programHeader.VirtualSize);
				return true;
			}
		}
		vq = default(VirtualQueryData);
		return false;
	}

	private void InitThreads()
	{
		if (_threads != null)
		{
			return;
		}
		_threads = new Dictionary<uint, IElfPRStatus>();
		foreach (IElfPRStatus item in _core.EnumeratePRStatus())
		{
			_threads.Add(item.ThreadId, item);
		}
	}
}
