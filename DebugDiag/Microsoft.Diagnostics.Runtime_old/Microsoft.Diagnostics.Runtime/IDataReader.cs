using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Runtime;

public interface IDataReader
{
	bool CanReadAsync { get; }

	bool IsMinidump { get; }

	void Close();

	void Flush();

	Architecture GetArchitecture();

	uint GetPointerSize();

	IList<ModuleInfo> EnumerateModules();

	void GetVersionInfo(ulong baseAddress, out VersionInfo version);

	bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead);

	bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead);

	AsyncMemoryReadResult ReadMemoryAsync(ulong address, int bytesRequested);

	ulong GetThreadTeb(uint thread);

	IEnumerable<uint> EnumerateAllThreads();

	bool VirtualQuery(ulong addr, out VirtualQueryData vq);

	bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context);

	bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context);

	ulong ReadPointerUnsafe(ulong addr);

	uint ReadDwordUnsafe(ulong addr);
}
