using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime.Utilities;

internal struct CV_INFO_PDB70
{
	public const int PDB70CvSignature = 1396986706;

	public int CvSignature;

	public Guid Signature;

	public int Age;

	public unsafe fixed byte bytePdbFileName[1];

	public unsafe string PdbFileName
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
