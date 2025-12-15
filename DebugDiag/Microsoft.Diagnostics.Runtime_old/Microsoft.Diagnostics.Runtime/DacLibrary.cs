using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.Desktop;

namespace Microsoft.Diagnostics.Runtime;

internal class DacLibrary
{
	private IntPtr _library;

	private IDacDataTarget _dacDataTarget;

	private IXCLRDataProcess _dac;

	private ISOSDac _sos;

	public IXCLRDataProcess DacInterface => _dac;

	public ISOSDac SOSInterface
	{
		get
		{
			if (_sos == null)
			{
				_sos = (ISOSDac)_dac;
			}
			return _sos;
		}
	}

	public DacLibrary(DataTargetImpl dataTarget, object ix)
	{
		_dac = ix as IXCLRDataProcess;
		if (_dac == null)
		{
			throw new ArgumentException("clrDataProcess not an instance of IXCLRDataProcess");
		}
	}

	public DacLibrary(DataTargetImpl dataTarget, string dacDll)
	{
		if (dataTarget.ClrVersions.Count == 0)
		{
			throw new ClrDiagnosticsException($"Process is not a CLR process!");
		}
		_library = NativeMethods.LoadLibrary(dacDll);
		if (_library == IntPtr.Zero)
		{
			throw new ClrDiagnosticsException("Failed to load dac: " + dacDll);
		}
		IntPtr procAddress = NativeMethods.GetProcAddress(_library, "CLRDataCreateInstance");
		_dacDataTarget = new DacDataTarget(dataTarget);
		NativeMethods.CreateDacInstance obj = (NativeMethods.CreateDacInstance)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(NativeMethods.CreateDacInstance));
		Guid riid = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");
		object ppObj;
		int num = obj(ref riid, _dacDataTarget, out ppObj);
		if (num == 0)
		{
			_dac = ppObj as IXCLRDataProcess;
		}
		if (_dac == null)
		{
			throw new ClrDiagnosticsException("Failure loading DAC: CreateDacInstance failed 0x" + num.ToString("x"), ClrDiagnosticsException.HR.DacError);
		}
	}

	~DacLibrary()
	{
		if (_library != IntPtr.Zero)
		{
			NativeMethods.FreeLibrary(_library);
		}
	}
}
