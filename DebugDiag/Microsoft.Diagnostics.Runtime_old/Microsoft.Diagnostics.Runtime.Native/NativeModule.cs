using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Diagnostics.Runtime.Native;

internal class NativeModule : ClrModule
{
	private NativeRuntime _runtime;

	private string _name;

	private string _filename;

	private ulong _imageBase;

	private ulong _size;

	public override string AssemblyName => _name;

	public override string Name => _name;

	public override bool IsDynamic => false;

	public override bool IsFile => true;

	public override string FileName => _filename;

	public override ulong ImageBase => _imageBase;

	public override ulong Size => _size;

	public override ulong MetadataAddress
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override ulong MetadataLength
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override object MetadataImport
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override DebuggableAttribute.DebuggingModes DebuggingMode
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override ulong AssemblyId
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool IsPdbLoaded
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override object PdbInterface
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public NativeModule(NativeRuntime runtime, ModuleInfo module)
	{
		_runtime = runtime;
		_name = (string.IsNullOrEmpty(module.FileName) ? "" : Path.GetFileNameWithoutExtension(module.FileName));
		_filename = module.FileName;
		_imageBase = module.ImageBase;
		_size = module.FileSize;
	}

	public override IEnumerable<ClrType> EnumerateTypes()
	{
		foreach (ClrType item in _runtime.GetHeap().EnumerateTypes())
		{
			if (item.Module == this)
			{
				yield return item;
			}
		}
	}

	internal int ComparePointer(ulong eetype)
	{
		if (eetype < ImageBase)
		{
			return -1;
		}
		if (eetype >= ImageBase + Size)
		{
			return 1;
		}
		return 0;
	}

	public override ClrType GetTypeByName(string name)
	{
		throw new NotImplementedException();
	}

	public override bool IsMatchingPdb(string pdbPath)
	{
		throw new NotImplementedException();
	}

	public override void LoadPdb(string path)
	{
		throw new NotImplementedException();
	}

	public override string TryDownloadPdb()
	{
		throw new NotImplementedException();
	}

	public override SourceLocation GetSourceInformation(uint token, int ilOffset)
	{
		throw new NotImplementedException();
	}

	public override SourceLocation GetSourceInformation(ClrMethod method, int ilOffset)
	{
		throw new NotImplementedException();
	}
}
