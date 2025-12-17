using System;
using System.IO;
using System.Runtime.InteropServices;
using DebugDiag.DbgEng;
using DebugDiag.DbgLib;
using DebugDiag.DotNet;
using IASPInfo = IISInfoLib.IASPInfo;
using IISInfoLib;

namespace DebugDiag.AnalysisRules;

public class CDump
{
	private struct MemLocation
	{
		public ulong VAAddr;

		public ulong VASize;

		public ulong FileAddr;

		public ulong FileSize;
	}

	public struct IMAGE_SECTION_HEADER
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public char[] Name;

		public uint VirtualSize;

		public uint VirtualAddress;

		public uint SizeOfRawData;

		public uint PointerToRawData;

		public uint PointerToRelocations;

		public uint PointerToLinenumbers;

		public ushort NumberOfRelocations;

		public ushort NumberOfLinenumbers;

		public uint Characteristics;
	}

	private string m_longFileName;

	private string m_shortFileName = "";

	private NetDbgObj m_debugger;

	private double m_processUpTime;

	private int m_dumpNumber;

	private IASPInfo m_aspInfo;

	private bool m_isMiniDump;

	private bool m_extendedThreadInfoAvailable;

	private COperations m_operationsInDump;

	private bool m_SymSrvDropped;

	private static NetDbgObj _previouslyOpenedDebugger;

	public COperations OperationsInDump => m_operationsInDump;

	public int DumpNumber
	{
		get
		{
			return m_dumpNumber;
		}
		set
		{
			m_dumpNumber = value;
		}
	}

	public IASPInfo ASPInfo => m_aspInfo;

	public bool IsMiniDump => m_isMiniDump;

	public bool ExtendedThreadInfoAvailable => m_extendedThreadInfoAvailable;

	public string ShortFileName => m_shortFileName;

	public string LongFileName => m_longFileName;

	public NetDbgObj Debugger
	{
		get
		{
			if (!IsDebuggerOpen())
			{
				OpenDebugger();
			}
			return m_debugger;
		}
	}

	public double ProcessUpTime => m_processUpTime;

	public DateTime SystemTime => Debugger.DumpCreationTime;

	public CDump()
	{
		m_operationsInDump = new COperations();
	}

	public void Init(string longFileName)
	{
		int num = 0;
		m_longFileName = longFileName;
		m_shortFileName = longFileName;
		num = longFileName.LastIndexOf("\\", 0, StringComparison.CurrentCultureIgnoreCase);
		if (num > 0)
		{
			m_shortFileName = longFileName.Substring(longFileName.GetSafeLength() - num);
		}
	}

	public void CloseDebugger()
	{
		m_debugger.Dispose();
		m_debugger = null;
		Globals.g_Debugger = null;
		_previouslyOpenedDebugger = null;
		m_aspInfo = null;
		Globals.g_ASPInfo = null;
	}

	public void MakeCurrent()
	{
		CacheFunctions.ResetCache();
		Globals.g_Debugger = Debugger;
		Globals.g_ASPInfo = m_aspInfo;
		Globals.g_ExtendedThreadInfoAvailable = m_debugger.ExtendedThreadInfoAvailable;
		Globals.g_ShortDumpFileName = m_shortFileName;
		Globals.HelperFunctions.SetOSVersion();
	}

	public void DropSymSrv()
	{
		m_SymSrvDropped = true;
		if (IsDebuggerOpen())
		{
			DropSymSrvInternal();
		}
	}

	private void DropSymSrvInternal()
	{
		string text = "";
		string[] array = null;
		string[] array2 = null;
		bool flag = false;
		int num = 0;
		text = Convert.ToString(Debugger.Execute(".sympath")).ToUpper();
		if (text.Substring(0, 23) == "SYMBOL SEARCH PATH IS: ")
		{
			text = text.Substring(23);
		}
		if (text.GetSafeLength() > 0)
		{
			text = Globals.HelperFunctions.Split(text, "\n", -1)[0];
		}
		array = Globals.HelperFunctions.Split(text, ";", -1);
		for (num = 0; num <= Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array, 1); num++)
		{
			if (array[num].Substring(0, 4) == "SRV*")
			{
				array2 = Globals.HelperFunctions.Split(array[num], "*", -1);
				if (Globals.HelperFunctions.UBound_HACK_DO_NOT_USE(array2, 1) == 2 && array2[1] != "" && array2[2] != "")
				{
					array2[2] = "";
					array[num] = string.Join("*", array2);
					flag = true;
				}
			}
		}
		if (flag)
		{
			Debugger.Execute(".sympath " + string.Join(";", array));
			Debugger.Execute(".reload");
		}
		Debugger.Execute(".reload /if msvbvm60.dll");
	}

	private void OpenDebugger()
	{
		if (_previouslyOpenedDebugger != null)
		{
			_previouslyOpenedDebugger.Dispose();
		}
		m_debugger = Globals.Manager.GetDebugger(LongFileName);
		_previouslyOpenedDebugger = m_debugger;
		if (m_SymSrvDropped)
		{
			DropSymSrvInternal();
		}
		if (m_processUpTime.Equals(0.0))
		{
			m_processUpTime = Debugger.ProcessUpTime;
			m_isMiniDump = Convert.ToString(Debugger.DumpType) == "MINIDUMP";
			m_extendedThreadInfoAvailable = Debugger.ExtendedThreadInfoAvailable;
		}
		MakeCurrent();
	}

	private bool IsDebuggerOpen()
	{
		bool result = false;
		if (m_debugger != null && _previouslyOpenedDebugger == m_debugger)
		{
			result = true;
		}
		return result;
	}

	public void AppendExePath(object exePath)
	{
		Debugger.Execute(".exepath+ " + Convert.ToString(exePath));
		Debugger.Execute(".reload");
	}

	public void SaveAllModules(object exePath)
	{
		Globals.HelperFunctions.ResetStatusNoIncrement("Saving all modules from full dump, to support mini dump analysis (this may take a while)");
		for (int i = 0; i < Debugger.Modules.Count; i++)
		{
			IDbgModule val = Debugger.Modules[i];
			ulong num = (ulong)val.Base;
			string imageName = val.ImageName;
			if (!string.IsNullOrEmpty(imageName))
			{
				imageName = Path.GetFileName(imageName);
				string text = Path.Combine(Convert.ToString(exePath), imageName);
				if (File.Exists(text))
				{
					text = Path.Combine(Convert.ToString(exePath), Path.GetFileNameWithoutExtension(imageName) + "_" + num + Path.GetExtension(imageName));
				}
				WriteModuleToDisk(text, val);
			}
		}
		Globals.HelperFunctions.ClearSubStatus();
	}

	private unsafe bool ReadStruct<T>(ulong address, out T s)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		IntPtr intPtr = IntPtr.Zero;
		IDebugDataSpaces4 val = (IDebugDataSpaces4)Debugger.RawDebugger;
		s = default(T);
		try
		{
			int num = Marshal.SizeOf(typeof(T));
			intPtr = Marshal.AllocHGlobal(num);
			uint num2 = 0u;
			if (val.ReadVirtual(address, intPtr, (uint)num, &num2) == 0)
			{
				s = (T)Marshal.PtrToStructure(intPtr, typeof(T));
				return true;
			}
			return false;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}

	private bool QueryVirtual(ulong address, out MEMORY_BASIC_INFORMATION64 mbi)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		IntPtr intPtr = IntPtr.Zero;
		IDebugDataSpaces4 val = (IDebugDataSpaces4)Debugger.RawDebugger;
		mbi = default(MEMORY_BASIC_INFORMATION64);
		try
		{
			intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(mbi));
			if (val.QueryVirtual(address, intPtr) == 0)
			{
				mbi = (MEMORY_BASIC_INFORMATION64)Marshal.PtrToStructure(intPtr, typeof(MEMORY_BASIC_INFORMATION64));
				return true;
			}
			return false;
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
	}

	private unsafe bool ReadVirtual(ulong address, byte[] buffer, ref uint read)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		IDebugDataSpaces4 val = (IDebugDataSpaces4)Debugger.RawDebugger;
		fixed (byte* value = buffer)
		{
			uint num = 0u;
			if (val.ReadVirtual(address, new IntPtr(value), read, &num) == 0)
			{
				read = num;
				return true;
			}
		}
		return false;
	}

	private bool ReadImageNtHeaders(ulong address, out IMAGE_NT_HEADERS64 headers)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return ((IDebugDataSpaces4)Debugger.RawDebugger).ReadImageNtHeaders(address, out headers) == 0;
	}

	private uint OSPageSize()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		IDebugControl val = (IDebugControl)Debugger.RawDebugger;
		uint result = 0u;
		val.GetPageSize(out result);
		return result;
	}

	private bool WriteModuleToDisk(string fullname, IDbgModule module)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Invalid comparison between Unknown and I4
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		ulong num = (ulong)module.Base;
		if (!QueryVirtual(num, out var mbi))
		{
			throw new InvalidOperationException("Failed to retrieve information about segment " + num.ToString("x16"));
		}
		bool flag = (int)mbi.Type == 16777216;
		if (!ReadStruct<IMAGE_DOS_HEADER>(num, out var s))
		{
			return false;
		}
		if (s.e_magic != 23117)
		{
			return false;
		}
		if (!ReadImageNtHeaders(num, out var headers))
		{
			return false;
		}
		ulong num2 = (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader").ToInt64();
		ulong num3 = num + s.e_lfanew + num2 + headers.FileHeader.SizeOfOptionalHeader;
		int numberOfSections = headers.FileHeader.NumberOfSections;
		MemLocation[] array = new MemLocation[numberOfSections];
		int num4 = -1;
		for (int i = 0; i < numberOfSections; i++)
		{
			if (ReadStruct<IMAGE_SECTION_HEADER>(num3, out var s2))
			{
				int j;
				for (j = 0; j <= num4 && s2.PointerToRawData >= array[j].FileAddr; j++)
				{
				}
				for (int num5 = num4; num5 >= j; num5--)
				{
					array[num5 + 1] = array[num5];
				}
				array[j].VAAddr = s2.VirtualAddress;
				array[j].VASize = s2.VirtualSize;
				array[j].FileAddr = s2.PointerToRawData;
				array[j].FileSize = s2.SizeOfRawData;
				num4++;
				num3 += (uint)Marshal.SizeOf((object)s2);
				continue;
			}
			throw new InvalidOperationException("Failed to read PE section info");
		}
		using (FileStream fileStream = File.Create(fullname))
		{
			uint num6 = OSPageSize();
			byte[] buffer = new byte[num6];
			ulong num7 = num;
			uint read;
			for (ulong num8 = num + headers.OptionalHeader.SizeOfHeaders; num7 < num8; num7 += read)
			{
				read = num6;
				if (num8 - num7 < read)
				{
					read = (uint)(num8 - num7);
				}
				if (!ReadVirtual(num7, buffer, ref read))
				{
					throw new InvalidOperationException("Failed to read memory at: " + num7.ToString("x16"));
				}
				fileStream.Write(buffer, 0, (int)read);
			}
			for (int j = 0; j <= num4; j++)
			{
				num7 = ((!Debugger.Is32Bit) ? (num + array[j].VAAddr) : ((!flag) ? (num + array[j].FileAddr) : (num + array[j].VAAddr)));
				for (ulong num8 = array[j].FileSize + num7 - 1; num7 <= num8; num7 += num6)
				{
					read = num6;
					if (num8 - num7 + 1 < num6)
					{
						read = (uint)(num8 - num7 + 1);
					}
					if (ReadVirtual(num7, buffer, ref read))
					{
						fileStream.Write(buffer, 0, (int)read);
					}
				}
			}
		}
		return true;
	}
}
