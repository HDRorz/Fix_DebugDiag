using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.ICorDebug;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime.DacInterface;

internal class DacDataTargetWrapper : COMCallableIUnknown, ICorDebugDataTarget
{
	private unsafe delegate int GetMetadataDelegate(IntPtr self, [In][MarshalAs(UnmanagedType.LPWStr)] string filename, uint imageTimestamp, uint imageSize, IntPtr mvid, uint mdRva, uint flags, uint bufferSize, IntPtr buffer, int* dataSize);

	private delegate int GetMachineTypeDelegate(IntPtr self, out IMAGE_FILE_MACHINE machineType);

	private delegate int GetPointerSizeDelegate(IntPtr self, out uint pointerSize);

	private delegate int GetImageBaseDelegate(IntPtr self, [In][MarshalAs(UnmanagedType.LPWStr)] string imagePath, out ulong baseAddress);

	private delegate int ReadVirtualDelegate(IntPtr self, ulong address, IntPtr buffer, int bytesRequested, out int bytesRead);

	private delegate int WriteVirtualDelegate(IntPtr self, ulong address, IntPtr buffer, uint bytesRequested, out uint bytesWritten);

	private delegate int GetTLSValueDelegate(IntPtr self, uint threadID, uint index, out ulong value);

	private delegate int SetTLSValueDelegate(IntPtr self, uint threadID, uint index, ulong value);

	private delegate int GetCurrentThreadIDDelegate(IntPtr self, out uint threadID);

	private delegate int GetThreadContextDelegate(IntPtr self, uint threadID, uint contextFlags, uint contextSize, IntPtr context);

	private delegate int SetThreadContextDelegate(IntPtr self, uint threadID, uint contextSize, IntPtr context);

	private delegate int RequestDelegate(IntPtr self, uint reqCode, uint inBufferSize, IntPtr inBuffer, IntPtr outBufferSize, out IntPtr outBuffer);

	private static readonly Guid IID_IDacDataTarget = new Guid("3E11CCEE-D08B-43e5-AF01-32717A64DA03");

	private static readonly Guid IID_IMetadataLocator = new Guid("aa8fa804-bc05-4642-b2c5-c353ed22fc63");

	private readonly DataTarget _dataTarget;

	private readonly IDataReader _dataReader;

	private readonly ModuleInfo[] _modules;

	private uint? _nextThreadId;

	private ulong? _nextTLSValue;

	public IntPtr IDacDataTarget { get; }

	public DacDataTargetWrapper(DataTarget dataTarget)
	{
		_dataTarget = dataTarget;
		_dataReader = _dataTarget.DataReader;
		_modules = dataTarget.EnumerateModules().ToArray();
		Array.Sort(_modules, (ModuleInfo a, ModuleInfo b) => a.ImageBase.CompareTo(b.ImageBase));
		VTableBuilder vTableBuilder = AddInterface(IID_IDacDataTarget, validate: false);
		vTableBuilder.AddMethod(new GetMachineTypeDelegate(GetMachineType));
		vTableBuilder.AddMethod(new GetPointerSizeDelegate(GetPointerSize));
		vTableBuilder.AddMethod(new GetImageBaseDelegate(GetImageBase));
		vTableBuilder.AddMethod(new ReadVirtualDelegate(ReadVirtual));
		vTableBuilder.AddMethod(new WriteVirtualDelegate(WriteVirtual));
		vTableBuilder.AddMethod(new GetTLSValueDelegate(GetTLSValue));
		vTableBuilder.AddMethod(new SetTLSValueDelegate(SetTLSValue));
		vTableBuilder.AddMethod(new GetCurrentThreadIDDelegate(GetCurrentThreadID));
		vTableBuilder.AddMethod(new GetThreadContextDelegate(GetThreadContext));
		vTableBuilder.AddMethod(new RequestDelegate(Request));
		IDacDataTarget = vTableBuilder.Complete();
		vTableBuilder = AddInterface(IID_IMetadataLocator, validate: false);
		vTableBuilder.AddMethod(new GetMetadataDelegate(GetMetadata));
		vTableBuilder.Complete();
	}

	public int ReadVirtual(IntPtr self, ulong address, IntPtr buffer, uint bytesRequested, out uint bytesRead)
	{
		if (ReadVirtual(self, address, buffer, (int)bytesRequested, out var bytesRead2) >= 0)
		{
			bytesRead = (uint)bytesRead2;
			return 0;
		}
		bytesRead = 0u;
		return -2147467259;
	}

	public int GetMachineType(IntPtr self, out IMAGE_FILE_MACHINE machineType)
	{
		switch (_dataReader.GetArchitecture())
		{
		case Architecture.Amd64:
			machineType = IMAGE_FILE_MACHINE.AMD64;
			break;
		case Architecture.X86:
			machineType = IMAGE_FILE_MACHINE.I386;
			break;
		case Architecture.Arm:
			machineType = IMAGE_FILE_MACHINE.THUMB2;
			break;
		case Architecture.Arm64:
			machineType = IMAGE_FILE_MACHINE.ARM64;
			break;
		default:
			machineType = IMAGE_FILE_MACHINE.UNKNOWN;
			break;
		}
		return 0;
	}

	private ModuleInfo GetModule(ulong address)
	{
		int num = 0;
		int num2 = _modules.Length - 1;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			ModuleInfo moduleInfo = _modules[num3];
			if (moduleInfo.ImageBase <= address && address < moduleInfo.ImageBase + moduleInfo.FileSize)
			{
				return moduleInfo;
			}
			if (moduleInfo.ImageBase < address)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return null;
	}

	public int GetPointerSize(IntPtr self, out uint pointerSize)
	{
		pointerSize = _dataReader.GetPointerSize();
		return 0;
	}

	public int GetImageBase(IntPtr self, string imagePath, out ulong baseAddress)
	{
		imagePath = Path.GetFileNameWithoutExtension(imagePath);
		ModuleInfo[] modules = _modules;
		foreach (ModuleInfo moduleInfo in modules)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleInfo.FileName);
			if (imagePath.Equals(fileNameWithoutExtension, StringComparison.CurrentCultureIgnoreCase))
			{
				baseAddress = moduleInfo.ImageBase;
				return 0;
			}
		}
		baseAddress = 0uL;
		return -2147467259;
	}

	public int ReadVirtual(IntPtr self, ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		if (_dataReader.ReadMemory(address, buffer, bytesRequested, out var bytesRead2))
		{
			bytesRead = bytesRead2;
			return 0;
		}
		bytesRead = 0;
		ModuleInfo module = GetModule(address);
		if (module != null)
		{
			if (Path.GetExtension(module.FileName).ToLower() == ".so")
			{
				return -2147467263;
			}
			string text = _dataTarget.SymbolLocator.FindBinary(module.FileName, module.TimeStamp, module.FileSize);
			if (text == null)
			{
				bytesRead = 0;
				return -2147467259;
			}
			PEImage pEImage = _dataTarget.FileLoader.LoadPEImage(text);
			if (pEImage != null)
			{
				int virtualAddress = checked((int)(address - module.ImageBase));
				bytesRead = pEImage.Read(buffer, virtualAddress, bytesRequested);
				return 0;
			}
		}
		return -2147467259;
	}

	public int ReadMemory(ulong address, byte[] buffer, uint bytesRequested, out uint bytesRead)
	{
		if (_dataReader.ReadMemory(address, buffer, (int)bytesRequested, out var bytesRead2))
		{
			bytesRead = (uint)bytesRead2;
			return 0;
		}
		bytesRead = 0u;
		return -2147467259;
	}

	public int ReadVirtual(ulong address, byte[] buffer, uint bytesRequested, out uint bytesRead)
	{
		return ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	public int WriteVirtual(IntPtr self, ulong address, IntPtr buffer, uint bytesRequested, out uint bytesWritten)
	{
		bytesWritten = bytesRequested;
		return 0;
	}

	public void SetNextCurrentThreadId(uint? threadId)
	{
		_nextThreadId = threadId;
	}

	internal void SetNextTLSValue(ulong? value)
	{
		_nextTLSValue = value;
	}

	public int GetTLSValue(IntPtr self, uint threadID, uint index, out ulong value)
	{
		if (_nextTLSValue.HasValue)
		{
			value = _nextTLSValue.Value;
			return 0;
		}
		value = 0uL;
		return -2147467259;
	}

	public int SetTLSValue(IntPtr self, uint threadID, uint index, ulong value)
	{
		return -2147467259;
	}

	public int GetCurrentThreadID(IntPtr self, out uint threadID)
	{
		if (_nextThreadId.HasValue)
		{
			threadID = _nextThreadId.Value;
			return 0;
		}
		threadID = 0u;
		return -2147467259;
	}

	public int GetThreadContext(IntPtr self, uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		if (_dataReader.GetThreadContext(threadID, contextFlags, contextSize, context))
		{
			return 0;
		}
		return -2147467259;
	}

	public int SetThreadContext(IntPtr self, uint threadID, uint contextSize, IntPtr context)
	{
		return -2147467263;
	}

	public int Request(IntPtr self, uint reqCode, uint inBufferSize, IntPtr inBuffer, IntPtr outBufferSize, out IntPtr outBuffer)
	{
		outBuffer = IntPtr.Zero;
		return -2147467263;
	}

	public unsafe int GetMetadata(IntPtr self, string filename, uint imageTimestamp, uint imageSize, IntPtr mvid, uint mdRva, uint flags, uint bufferSize, IntPtr buffer, int* pDataSize)
	{
		if (buffer == IntPtr.Zero)
		{
			return -2147024809;
		}
		string text = _dataTarget.SymbolLocator.FindBinary(filename, imageTimestamp, imageSize);
		if (text == null)
		{
			return -2147467259;
		}
		PEImage pEImage = _dataTarget.FileLoader.LoadPEImage(text);
		if (pEImage == null)
		{
			return -2147467259;
		}
		uint num = mdRva;
		uint num2 = bufferSize;
		if (num == 0)
		{
			CorHeader corHeader = pEImage.CorHeader;
			if (corHeader == null)
			{
				return -2147467259;
			}
			Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY metadata = corHeader.Metadata;
			if (metadata.VirtualAddress == 0)
			{
				return -2147467259;
			}
			num = metadata.VirtualAddress;
			num2 = Math.Min(bufferSize, metadata.Size);
		}
		int num3 = checked(pEImage.Read(buffer, (int)num, (int)num2));
		if (pDataSize != null)
		{
			*pDataSize = num3;
		}
		return 0;
	}

	CorDebugPlatform ICorDebugDataTarget.GetPlatform()
	{
		return _dataReader.GetArchitecture() switch
		{
			Architecture.Amd64 => CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64, 
			Architecture.X86 => CorDebugPlatform.CORDB_PLATFORM_WINDOWS_X86, 
			Architecture.Arm => CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM, 
			Architecture.Arm64 => CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64, 
			_ => throw new Exception(), 
		};
	}

	uint ICorDebugDataTarget.ReadVirtual(ulong address, IntPtr buffer, uint bytesRequested)
	{
		if (ReadVirtual(IntPtr.Zero, address, buffer, (int)bytesRequested, out var bytesRead) >= 0)
		{
			return (uint)bytesRead;
		}
		throw new Exception();
	}

	void ICorDebugDataTarget.GetThreadContext(uint threadId, uint contextFlags, uint contextSize, IntPtr context)
	{
		if (!_dataReader.GetThreadContext(threadId, contextFlags, contextSize, context))
		{
			throw new Exception();
		}
	}
}
