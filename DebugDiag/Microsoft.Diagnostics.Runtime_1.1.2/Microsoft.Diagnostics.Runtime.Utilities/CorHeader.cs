using System;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime.Utilities;

public class CorHeader
{
	private IMAGE_COR20_HEADER _header;

	public COMIMAGE_FLAGS Flags => (COMIMAGE_FLAGS)_header.Flags;

	public ushort MajorRuntimeVersion => _header.MajorRuntimeVersion;

	public ushort MinorRuntimeVersion => _header.MinorRuntimeVersion;

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY Metadata => _header.MetaData;

	public uint NativeEntryPoint
	{
		get
		{
			if ((Flags & COMIMAGE_FLAGS.NATIVE_ENTRYPOINT) != COMIMAGE_FLAGS.NATIVE_ENTRYPOINT)
			{
				throw new InvalidOperationException();
			}
			return _header.EntryPoint.RVA;
		}
	}

	public uint ManagedEntryPoint
	{
		get
		{
			if ((Flags & COMIMAGE_FLAGS.NATIVE_ENTRYPOINT) == COMIMAGE_FLAGS.NATIVE_ENTRYPOINT)
			{
				throw new InvalidOperationException();
			}
			return _header.EntryPoint.Token;
		}
	}

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY Resources => _header.Resources;

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY StrongNameSignature => _header.StrongNameSignature;

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY VTableFixups => _header.VTableFixups;

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ExportAddressTableJumps => _header.ExportAddressTableJumps;

	public Microsoft.Diagnostics.Runtime.Interop.IMAGE_DATA_DIRECTORY ManagedNativeHeader => _header.ManagedNativeHeader;

	internal CorHeader(IMAGE_COR20_HEADER header)
	{
		_header = header;
	}
}
