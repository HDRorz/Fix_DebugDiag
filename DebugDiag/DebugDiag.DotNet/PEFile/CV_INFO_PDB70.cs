using System;
using System.Runtime.InteropServices;

namespace PEFile;

internal struct CV_INFO_PDB70
{
	internal const int PDB70CvSignature = 1396986706;

	internal int CvSignature;

	internal Guid Signature;

	internal int Age;

	internal unsafe fixed byte bytePdbFileName[1];

	internal unsafe string PdbFileName
	{
		get
		{
			fixed (byte* ptr = bytePdbFileName)
			{
				return Marshal.PtrToStringAnsi((IntPtr)ptr);
			}
		}
	}
}
