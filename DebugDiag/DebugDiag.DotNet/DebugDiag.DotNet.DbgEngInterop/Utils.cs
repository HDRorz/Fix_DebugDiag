namespace DebugDiag.DotNet.DbgEngInterop;

public class Utils
{
	public static bool SUCCEEDED(int hr)
	{
		return SUCCEEDED((HRESULTS)hr);
	}

	public static bool FAILED(int hr)
	{
		return FAILED((HRESULTS)hr);
	}

	public static bool SUCCEEDED(HRESULTS hr)
	{
		return hr >= HRESULTS.S_OK;
	}

	public static bool FAILED(HRESULTS hr)
	{
		return !SUCCEEDED(hr);
	}
}
