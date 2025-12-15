using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Runtime;

internal class TraceDataReader : IDataReader
{
	private readonly IDataReader _reader;

	private readonly StreamWriter _file;

	public bool IsMinidump { get; }

	public TraceDataReader(IDataReader reader)
	{
		_reader = reader;
		_file = File.CreateText("datareader.txt");
		_file.AutoFlush = true;
		_file.WriteLine(reader.GetType().ToString());
	}

	public void Close()
	{
		_file.WriteLine("Close");
		_reader.Close();
	}

	public void Flush()
	{
		_file.WriteLine("Flush");
		_reader.Flush();
	}

	public Architecture GetArchitecture()
	{
		Architecture architecture = _reader.GetArchitecture();
		_file.WriteLine("GetArchitecture - {0}", architecture);
		return architecture;
	}

	public uint GetPointerSize()
	{
		uint pointerSize = _reader.GetPointerSize();
		_file.WriteLine("GetPointerSize - {0}", pointerSize);
		return pointerSize;
	}

	public IList<ModuleInfo> EnumerateModules()
	{
		IList<ModuleInfo> list = _reader.EnumerateModules();
		int num = 0;
		foreach (ModuleInfo item in list)
		{
			num ^= item.FileName.ToLower().GetHashCode();
		}
		_file.WriteLine("EnumerateModules - {0} {1:x}", list.Count, num);
		return list;
	}

	public void GetVersionInfo(ulong baseAddress, out VersionInfo version)
	{
		_reader.GetVersionInfo(baseAddress, out version);
		_file.WriteLine("GetVersionInfo - {0:x} {1}", baseAddress, version.ToString());
	}

	public bool ReadMemory(ulong address, byte[] buffer, int bytesRequested, out int bytesRead)
	{
		bool flag = _reader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
		StringBuilder stringBuilder = new StringBuilder();
		int num = ((bytesRead > 8) ? 8 : bytesRead);
		for (int i = 0; i < num; i++)
		{
			stringBuilder.Append(buffer[i].ToString("x"));
		}
		_file.WriteLine("ReadMemory {0}- {1:x} {2} {3}", flag ? "" : "failed ", address, bytesRead, stringBuilder);
		return flag;
	}

	public bool ReadMemory(ulong address, IntPtr buffer, int bytesRequested, out int bytesRead)
	{
		bool flag = _reader.ReadMemory(address, buffer, bytesRequested, out bytesRead);
		_file.WriteLine("ReadMemory {0}- {1:x} {2}", flag ? "" : "failed ", address, bytesRead);
		return flag;
	}

	public ulong GetThreadTeb(uint thread)
	{
		ulong threadTeb = _reader.GetThreadTeb(thread);
		_file.WriteLine("GetThreadTeb - {0:x} {1:x}", thread, threadTeb);
		return threadTeb;
	}

	public IEnumerable<uint> EnumerateAllThreads()
	{
		List<uint> list = new List<uint>(_reader.EnumerateAllThreads());
		bool flag = true;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (uint item in list)
		{
			if (!flag)
			{
				stringBuilder.Append(", ");
			}
			flag = false;
			stringBuilder.Append(item.ToString("x"));
		}
		_file.WriteLine("Threads: {0} {1}", list.Count, stringBuilder);
		return list;
	}

	public bool VirtualQuery(ulong addr, out VirtualQueryData vq)
	{
		bool flag = _reader.VirtualQuery(addr, out vq);
		_file.WriteLine("VirtualQuery {0}: {1:x} {2:x} {3}", flag ? "" : "failed ", addr, vq.BaseAddress, vq.Size);
		return flag;
	}

	public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, IntPtr context)
	{
		bool threadContext = _reader.GetThreadContext(threadID, contextFlags, contextSize, context);
		_file.WriteLine("GetThreadContext - {0}", threadContext);
		return threadContext;
	}

	public bool GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte[] context)
	{
		bool threadContext = _reader.GetThreadContext(threadID, contextFlags, contextSize, context);
		_file.WriteLine("GetThreadContext - {0}", threadContext);
		return threadContext;
	}

	public ulong ReadPointerUnsafe(ulong addr)
	{
		ulong num = _reader.ReadPointerUnsafe(addr);
		_file.WriteLine("ReadPointerUnsafe - {0}: {1}", addr, num);
		return num;
	}

	public uint ReadDwordUnsafe(ulong addr)
	{
		uint num = _reader.ReadDwordUnsafe(addr);
		_file.WriteLine("ReadDwordUnsafe - {0}: {1}", addr, num);
		return num;
	}
}
