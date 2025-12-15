namespace Microsoft.Diagnostics.Runtime.Interop;

public struct DEBUG_HANDLE_DATA_BASIC
{
	public uint TypeNameSize;

	public uint ObjectNameSize;

	public uint Attributes;

	public uint GrantedAccess;

	public uint HandleCount;

	public uint PointerCount;
}
