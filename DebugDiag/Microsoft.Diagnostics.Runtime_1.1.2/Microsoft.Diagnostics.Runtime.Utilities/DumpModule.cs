using System;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal class DumpModule
{
	private readonly DumpReader _owner;

	internal MINIDUMP_MODULE Raw { get; }

	public string FullName
	{
		get
		{
			RVA moduleNameRva = Raw.ModuleNameRva;
			DumpPointer ptr = _owner.TranslateRVA(moduleNameRva);
			return _owner.GetString(ptr);
		}
	}

	public ulong BaseAddress => Raw.BaseOfImage;

	public uint Size => Raw.SizeOfImage;

	public DateTime Timestamp => Raw.Timestamp;

	public uint RawTimestamp => Raw.TimeDateStamp;

	internal DumpModule(DumpReader owner, MINIDUMP_MODULE raw)
	{
		Raw = raw;
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
			return dumpModule.Raw == Raw;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)Raw.TimeDateStamp;
	}
}
