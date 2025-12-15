using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class DumpModule
{
	private DumpReader.DumpNative.MINIDUMP_MODULE _raw;

	private DumpReader _owner;

	internal DumpReader.DumpNative.MINIDUMP_MODULE Raw => _raw;

	public string FullName
	{
		get
		{
			DumpReader.DumpNative.RVA moduleNameRva = _raw.ModuleNameRva;
			DumpPointer ptr = _owner.TranslateRVA(moduleNameRva);
			return _owner.GetString(ptr);
		}
	}

	public ulong BaseAddress => _raw.BaseOfImage;

	public uint Size => _raw.SizeOfImage;

	public DateTime Timestamp => _raw.Timestamp;

	public uint RawTimestamp => _raw.TimeDateStamp;

	internal DumpModule(DumpReader owner, DumpReader.DumpNative.MINIDUMP_MODULE raw)
	{
		_raw = raw;
		_owner = owner;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is DumpModule dumpModule))
		{
			return false;
		}
		if (dumpModule._owner == _owner)
		{
			return dumpModule._raw == _raw;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)_raw.TimeDateStamp;
	}
}
