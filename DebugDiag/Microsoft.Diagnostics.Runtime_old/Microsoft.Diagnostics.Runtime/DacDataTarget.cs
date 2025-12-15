using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

internal class DacDataTarget : IDacDataTarget, IMetadataLocator
{
	private DataTargetImpl _dataTarget;

	private IDataReader _dataReader;

	private ModuleInfo[] _modules;

	public DacDataTarget(DataTargetImpl dataTarget)
	{
		_dataTarget = dataTarget;
		_dataReader = _dataTarget.DataReader;
		_modules = dataTarget.EnumerateModules().ToArray();
		Array.Sort(_modules, (ModuleInfo a, ModuleInfo b) => a.ImageBase.CompareTo(b.ImageBase));
	}

	public void GetMachineType(out IMAGE_FILE_MACHINE machineType)
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
		default:
			machineType = IMAGE_FILE_MACHINE.UNKNOWN;
			break;
		}
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

	public void GetPointerSize(out uint pointerSize)
	{
		pointerSize = _dataReader.GetPointerSize();
	}

	public void GetImageBase(string imagePath, out ulong baseAddress)
	{
		imagePath = Path.GetFileNameWithoutExtension(imagePath);
		ModuleInfo[] modules = _modules;
		foreach (ModuleInfo moduleInfo in modules)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleInfo.FileName);
			if (imagePath.Equals(fileNameWithoutExtension, StringComparison.CurrentCultureIgnoreCase))
			{
				baseAddress = moduleInfo.ImageBase;
				return;
			}
		}
		throw new Exception();
	}

	public unsafe int ReadVirtual(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		int bytesRead2 = 0;
		if (_dataReader.ReadMemory(address, buffer, bytesRequested, out bytesRead2))
		{
			bytesRead = bytesRead2;
			return 0;
		}
		ModuleInfo module = GetModule(address);
		if (module != null)
		{
			PEFile pEFile = _dataTarget.SymbolLocator.LoadBinary(module.FileName, module.TimeStamp, module.FileSize);
			if (pEFile != null)
			{
				PEBuffer pEBuffer = pEFile.AllocBuff();
				int result = checked((int)(address - module.ImageBase));
				if (pEFile.Header.TryGetFileOffsetFromRva(result, out result))
				{
					byte* ptr = (byte*)buffer.ToPointer();
					byte* ptr2 = pEBuffer.Fetch(result, bytesRequested);
					for (int i = 0; i < bytesRequested; i++)
					{
						ptr[i] = ptr2[i];
					}
					bytesRead = bytesRequested;
					return 0;
				}
				pEFile.FreeBuff(pEBuffer);
			}
		}
		bytesRead = 0;
		return -1;
	}

	public int ReadMemory(ulong address, byte[] buffer, uint bytesRequested, out uint bytesRead)
	{
		int bytesRead2 = 0;
		if (_dataReader.ReadMemory(address, buffer, (int)bytesRequested, out bytesRead2))
		{
			bytesRead = (uint)bytesRead2;
			return 0;
		}
		bytesRead = 0u;
		return -1;
	}

	public int ReadVirtual(ulong address, byte[] buffer, uint bytesRequested, out uint bytesRead)
	{
		return ReadMemory(address, buffer, bytesRequested, out bytesRead);
	}

	public void WriteVirtual(ulong address, byte[] buffer, uint bytesRequested, out uint bytesWritten)
	{
		bytesWritten = bytesRequested;
	}

	public void GetTLSValue(uint threadID, uint index, out ulong value)
	{
		value = 0uL;
	}

	public void SetTLSValue(uint threadID, uint index, ulong value)
	{
		throw new NotImplementedException();
	}

	public void GetCurrentThreadID(out uint threadID)
	{
		threadID = 0u;
	}

	public void GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		_dataReader.GetThreadContext(threadID, contextFlags, contextSize, context);
	}

	public void SetThreadContext(uint threadID, uint contextSize, IntPtr context)
	{
		throw new NotImplementedException();
	}

	public void Request(uint reqCode, uint inBufferSize, IntPtr inBuffer, IntPtr outBufferSize, out IntPtr outBuffer)
	{
		throw new NotImplementedException();
	}

	public int GetMetadata(string filename, uint imageTimestamp, uint imageSize, IntPtr mvid, uint mdRva, uint flags, uint bufferSize, byte[] buffer, IntPtr dataSize)
	{
		PEFile pEFile = _dataTarget.SymbolLocator.LoadBinary(filename, imageTimestamp, imageSize);
		if (pEFile == null)
		{
			return -1;
		}
		Microsoft.Diagnostics.Runtime.Utilities.IMAGE_DATA_DIRECTORY comDescriptorDirectory = pEFile.Header.ComDescriptorDirectory;
		if (comDescriptorDirectory.VirtualAddress == 0)
		{
			return -1;
		}
		PEBuffer buffer2 = pEFile.AllocBuff();
		if (mdRva == 0)
		{
			IMAGE_COR20_HEADER iMAGE_COR20_HEADER = (IMAGE_COR20_HEADER)Marshal.PtrToStructure(pEFile.SafeFetchRVA(comDescriptorDirectory.VirtualAddress, comDescriptorDirectory.Size, buffer2), typeof(IMAGE_COR20_HEADER));
			if (bufferSize < iMAGE_COR20_HEADER.MetaData.Size)
			{
				pEFile.FreeBuff(buffer2);
				return -1;
			}
			mdRva = iMAGE_COR20_HEADER.MetaData.VirtualAddress;
			bufferSize = iMAGE_COR20_HEADER.MetaData.Size;
		}
		Marshal.Copy(pEFile.SafeFetchRVA((int)mdRva, (int)bufferSize, buffer2), buffer, 0, (int)bufferSize);
		pEFile.FreeBuff(buffer2);
		return 0;
	}
}
