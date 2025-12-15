using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime.DacInterface;
using Microsoft.Diagnostics.Runtime.Utilities;

namespace Microsoft.Diagnostics.Runtime;

public sealed class DacLibrary : IDisposable
{
	private delegate int DllMain(IntPtr instance, int reason, IntPtr reserved);

	private delegate int PAL_Initialize();

	private delegate int CreateDacInstance(ref Guid riid, IntPtr dacDataInterface, out IntPtr ppObj);

	private bool _disposed;

	private SOSDac _sos;

	private SOSDac6 _sos6;

	internal DacDataTargetWrapper DacDataTarget { get; }

	internal RefCountedFreeLibrary OwningLibrary { get; }

	internal ClrDataProcess InternalDacPrivateInterface { get; }

	public ClrDataProcess DacPrivateInterface => new ClrDataProcess(InternalDacPrivateInterface);

	public SOSDac SOSDacInterface
	{
		get
		{
			SOSDac sOSInterfaceNoAddRef = GetSOSInterfaceNoAddRef();
			if (sOSInterfaceNoAddRef == null)
			{
				return null;
			}
			return new SOSDac(sOSInterfaceNoAddRef);
		}
	}

	internal SOSDac GetSOSInterfaceNoAddRef()
	{
		if (_sos == null)
		{
			_sos = InternalDacPrivateInterface.GetSOSDacInterface();
		}
		return _sos;
	}

	internal SOSDac6 GetSOSInterface6NoAddRef()
	{
		if (_sos6 == null)
		{
			_sos6 = InternalDacPrivateInterface.GetSOSDacInterface6();
		}
		return _sos6;
	}

	public T GetInterface<T>(ref Guid riid) where T : CallableCOMWrapper
	{
		IntPtr intPtr = InternalDacPrivateInterface.QueryInterface(ref riid);
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		return (T)Activator.CreateInstance(typeof(T), this, intPtr);
	}

	internal static IntPtr TryGetDacPtr(object ix)
	{
		IntPtr intPtr = ((ix is IntPtr) ? ((IntPtr)ix) : ((!Marshal.IsComObject(ix)) ? IntPtr.Zero : Marshal.GetIUnknownForObject(ix)));
		if (intPtr == IntPtr.Zero)
		{
			throw new ArgumentException("clrDataProcess not an instance of IXCLRDataProcess");
		}
		return intPtr;
	}

	internal DacLibrary(DataTarget dataTarget, IntPtr pUnk)
	{
		InternalDacPrivateInterface = new ClrDataProcess(this, pUnk);
	}

	public DacLibrary(DataTarget dataTarget, string dacDll)
	{
		if (dataTarget.ClrVersions.Count == 0)
		{
			throw new ClrDiagnosticsException("Process is not a CLR process!");
		}
		IntPtr intPtr = DataTarget.PlatformFunctions.LoadLibrary(dacDll);
		if (intPtr == IntPtr.Zero)
		{
			throw new ClrDiagnosticsException("Failed to load dac: " + intPtr);
		}
		OwningLibrary = new RefCountedFreeLibrary(intPtr);
		dataTarget.AddDacLibrary(this);
		IntPtr procAddress = DataTarget.PlatformFunctions.GetProcAddress(intPtr, "DAC_PAL_InitializeDLL");
		if (procAddress == IntPtr.Zero)
		{
			procAddress = DataTarget.PlatformFunctions.GetProcAddress(intPtr, "PAL_InitializeDLL");
		}
		if (procAddress != IntPtr.Zero)
		{
			IntPtr procAddress2 = DataTarget.PlatformFunctions.GetProcAddress(intPtr, "DllMain");
			if (procAddress2 == IntPtr.Zero)
			{
				throw new ClrDiagnosticsException("Failed to obtain Dac DllMain");
			}
			((DllMain)Marshal.GetDelegateForFunctionPointer(procAddress2, typeof(DllMain)))(intPtr, 1, IntPtr.Zero);
		}
		IntPtr procAddress3 = DataTarget.PlatformFunctions.GetProcAddress(intPtr, "CLRDataCreateInstance");
		if (procAddress3 == IntPtr.Zero)
		{
			throw new ClrDiagnosticsException("Failed to obtain Dac CLRDataCreateInstance");
		}
		DacDataTarget = new DacDataTargetWrapper(dataTarget);
		CreateDacInstance obj = (CreateDacInstance)Marshal.GetDelegateForFunctionPointer(procAddress3, typeof(CreateDacInstance));
		Guid riid = new Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7");
		IntPtr ppObj;
		int num = obj(ref riid, DacDataTarget.IDacDataTarget, out ppObj);
		if (num != 0)
		{
			throw new ClrDiagnosticsException("Failure loading DAC: CreateDacInstance failed 0x" + num.ToString("x"), ClrDiagnosticsExceptionKind.DacError, num);
		}
		InternalDacPrivateInterface = new ClrDataProcess(this, ppObj);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~DacLibrary()
	{
		Dispose(disposing: false);
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			InternalDacPrivateInterface?.Dispose();
			_sos?.Dispose();
			OwningLibrary?.Release();
			_disposed = true;
		}
	}
}
