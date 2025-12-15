using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DebugDiag.DbgLib;

/// <summary>
/// This is the interface implemented by a COM object that represents an instance of a DLL or EXE module.  
/// It is used to obtain information about the module it represents.  To obtain an instance of this object call the <c>NetDbgObj.GetModuleByAddress</c>
/// or <c>NetDbgObj.GetModuleByName</c> method of the DbgObj object. Alternatively you can use the ModuleInfo COM object to iterate through a collection of loaded modules, for more
/// information about the ModuleInfo see <see cref="T:DebugDiag.DbgLib.IModuleInfo" /> Interface help.
/// </summary>
/// <example>
/// <code language="cs">
///             public class ImplementedRule : IMultiDumpRule, IAnalysisRuleMetadata
///             {
/// public void RunAnalysisRule(NetScriptManager manager, NetProgress progress)
/// {
///    //Create a list of dumps that will be analyzed
///    List&lt;string&gt; dumpFiles = new List&lt;string&gt;();
///
///    dumpFiles = manager.GetDumpFiles();
///
///    foreach (string dump in dumpFiles)
///    {
///        using (NetDbgObj debugger = manager.GetDebugger(dump))
///        {
///            DebugDiag.DbgLib.IDbgModule Module = debugger.GetModuleByModuleName("clr");
///
///            if (Module == null)
///            {
///                manager.WriteLine("Unable to obtain Module object for clr.dll");
///            }
///            else
///            {
///                object Major, Minor, Build, Version;
///                manager.WriteLine("Image name: " + Module.ImageName);
///                Module.GetFileVersion(out Major, out Minor, out Build, out Version);
///                manager.WriteLine("File version: " + Major.ToString() + "." + Minor.ToString() + "." + Build.ToString() + "." + Version.ToString());
///                Module.GetProductVersion(out Major, out Minor, out Build, out Version);
///                manager.WriteLine("Product version: " + Major.ToString() + "." + Minor.ToString() + "." + Build.ToString() + "." + Version.ToString());
///                manager.WriteLine("Base address: " + Module.Base.ToString());
///                manager.WriteLine("Checksum: " + Module.Checksum.ToString());
///                manager.WriteLine("COM DLL: " + Module.IsCOMDLL.ToString());
///                manager.WriteLine("ISAPIExtension: " + Module.IsISAPIExtension.ToString());
///                manager.WriteLine("ISAPIFilter: " + Module.IsISAPIFilter.ToString());
///                manager.WriteLine("Managed DLL: " + Module.IsManaged.ToString());
///                manager.WriteLine("VB DLL: " + Module.IsVBModule.ToString());
///                manager.WriteLine("Loaded Image Name: " + Module.LoadedImageName);
///                manager.WriteLine("Mapped Image Name: " + Module.MappedImageName);
///                manager.WriteLine("Module name: " + Module.ModuleName);
///                manager.WriteLine("Single Threaded: " + Module.SingleThreaded.ToString());
///                manager.WriteLine("Size: " + Module.Size.ToString());
///                manager.WriteLine("Symbol File Name: " + Module.SymbolFileName);
///                manager.WriteLine("Symbol Type: " + Module.SymbolType);
///                manager.WriteLine("Time Stamp: " + Module.TimeStamp);
///                manager.WriteLine("Comments: " + Module.VSComments);
///                manager.WriteLine("Company Name: " + Module.VSCompanyName);
///                manager.WriteLine("File Description: " + Module.VSFileDescription);
///                manager.WriteLine("File Version: " + Module.VSFileVersion);
///                manager.WriteLine("Internal Name: " + Module.VSInternalName);
///                manager.WriteLine("Legal Copyright: " + Module.VSLegalCopyright);
///                manager.WriteLine("Legal Trademarks: " + Module.VSLegalTrademarks);
///                manager.WriteLine("Original filename: " + Module.VSOriginalFilename);
///                manager.WriteLine("Private Build: " + Module.VSPrivateBuild);
///                manager.WriteLine("Product Name: " + Module.VSProductName);
///                manager.WriteLine("Product Version: " + Module.VSProductVersion);
///                manager.WriteLine("Special Build: " + Module.VSSpecialBuild);
///            }
///        }
///    }
/// }
///             } 
/// </code>
/// </example>
[ComImport]
[TypeLibType(TypeLibTypeFlags.FDual | TypeLibTypeFlags.FNonExtensible | TypeLibTypeFlags.FDispatchable)]
[Guid("E590793C-DAFD-4F43-8089-4B6E260842F5")]
public interface IDbgModule
{
	/// <summary>
	/// This property returns the base address for this module.
	/// </summary>
	[DispId(1)]
	double Base
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	/// <summary>
	/// This property returns the size of the module. 
	/// </summary>
	[DispId(2)]
	double Size
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		get;
	}

	/// <summary>
	/// This property returns the date and time stamp of the module.
	/// </summary>
	[DispId(3)]
	string TimeStamp
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(3)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the Checksum value for this module.
	/// </summary>
	[DispId(4)]
	double Checksum
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(4)]
		get;
	}

	/// <summary>
	/// This property returns a string indicating the type of symbol loaded.
	///
	/// The string will be one of the following values.
	///
	/// None
	/// COFF
	/// CodeView
	/// PDB
	/// Export
	/// Deferred
	/// SYM
	/// DIA
	/// Unknown 
	/// </summary>
	[DispId(5)]
	string SymbolType
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(5)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns True if the module is a .NET managed module, otherwise it returns False.
	/// </summary>
	[DispId(6)]
	bool IsManaged
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(6)]
		get;
	}

	/// <summary>
	/// This property returns the name of the module.
	/// </summary>
	[DispId(7)]
	string ImageName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(7)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the module name associated with this DbgModule object.
	/// </summary>
	[DispId(8)]
	string ModuleName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(8)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the name of the loaded image if one is loaded.   This value will be empty if the debugger has not loaded an image for this module. 
	/// </summary>
	[DispId(9)]
	string LoadedImageName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(9)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the path and filename of the symbols loaded for this module.
	/// </summary>
	[DispId(10)]
	string SymbolFileName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(10)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the name of the mapped image if one is loaded.   This value will be empty if the debugger has not mapped an image for this module.
	/// </summary>
	[DispId(11)]
	string MappedImageName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(11)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the index of this module in the ModuleInfo object.
	/// </summary>
	[DispId(12)]
	int Index
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(12)]
		get;
	}

	/// <summary>
	/// This property returns the version specific comments for this module. 
	/// </summary>
	[DispId(13)]
	string VSComments
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(13)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific internal name for this module.
	/// </summary>
	[DispId(14)]
	string VSInternalName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(14)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific product name for this module.
	/// </summary>
	[DispId(15)]
	string VSProductName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(15)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific company name for this module. 
	/// </summary>
	[DispId(16)]
	string VSCompanyName
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(16)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific legal copyright information for this module.
	/// </summary>
	[DispId(17)]
	string VSLegalCopyright
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(17)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific product version for this module.
	/// </summary>
	[DispId(18)]
	string VSProductVersion
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(18)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific file description for this module.
	/// </summary>
	[DispId(19)]
	string VSFileDescription
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(19)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific legal trademarks information for this module.
	/// </summary>
	[DispId(20)]
	string VSLegalTrademarks
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(20)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific private build information for this module. 
	/// </summary>
	[DispId(21)]
	string VSPrivateBuild
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(21)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific file version for this module.
	/// </summary>
	[DispId(22)]
	string VSFileVersion
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(22)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific original filename for this module.
	/// </summary>
	[DispId(23)]
	string VSOriginalFilename
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(23)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This property returns the version specific special build information for this module.
	/// </summary>
	[DispId(24)]
	string VSSpecialBuild
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(24)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	/// <summary>
	/// This method is used to retrieve the Major, Minor, Build, and Version number of the module. 
	/// </summary>
	/// <param name="Major">Output parameter with the Major version number</param>
	/// <param name="Minor">Output parameter with the Minor version number</param>
	/// <param name="Build">Output parameter with the Build number</param>
	/// <param name="Private">Output parameter with the Version version number</param>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(25)]
	void GetFileVersion([MarshalAs(UnmanagedType.Struct)] out object Major, [MarshalAs(UnmanagedType.Struct)] out object Minor, [MarshalAs(UnmanagedType.Struct)] out object Build, [MarshalAs(UnmanagedType.Struct)] out object Private);

	/// <summary>
	/// This method is used to retrieve the Major, Minor, Build, and Version number of the product. 
	/// </summary>
	/// <param name="Major">Output parameter with the Major version number</param>
	/// <param name="Minor">Output parameter with the Minor version number</param>
	/// <param name="Build">Output parameter with the Build number</param>
	/// <param name="Private">Output parameter with the Version version number</param>
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(26)]
	void GetProductVersion([MarshalAs(UnmanagedType.Struct)] out object Major, [MarshalAs(UnmanagedType.Struct)] out object Minor, [MarshalAs(UnmanagedType.Struct)] out object Build, [MarshalAs(UnmanagedType.Struct)] out object Private);

	/// <summary>
	/// This property returns a ExportsInfo object. The returned object can be used to determine what functions this module exports.For more information about the 
	/// ExportsInfo object see <see cref="T:DebugDiag.DbgLib.IExportsInfo" /> interface help.
	/// </summary>
	[DispId(27)]
	IExportsInfo ExportsInfo
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(27)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This property returns a ImportsInfo object. The returned object can be used to determine what functions this module imports. For more information about the 
	/// ImportsInfo object see <see cref="T:DebugDiag.DbgLib.IImportsInfo" /> interface help.
	/// </summary>
	[DispId(28)]
	IImportsInfo ImportsInfo
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(28)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	/// <summary>
	/// This property returns True if the module is an ISAPI Extension, otherwise it returns False.
	/// </summary>
	[DispId(29)]
	bool IsISAPIExtension
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(29)]
		get;
	}

	/// <summary>
	/// This property returns True if the module is an ISAPI Filter, otherwise it returns False.
	/// </summary>
	[DispId(30)]
	bool IsISAPIFilter
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(30)]
		get;
	}

	/// <summary>
	/// This property returns True if this module is a Visual Basic DLL, otherwise it returns False.
	/// </summary>
	[DispId(31)]
	bool IsVBModule
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(31)]
		get;
	}

	/// <summary>
	/// This property returns True if the module is a COM DLL, otherwise it returns False.
	/// </summary>
	[DispId(32)]
	bool IsCOMDLL
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(32)]
		get;
	}

	/// <summary>
	/// This property returns True if the module has the Retain in Memory options set, otherwise it returns False.  
	/// This property should only be examined if the <c>IsVBModule</c> property returns True.
	/// </summary>
	[DispId(33)]
	bool RetainedInMemory
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(33)]
		get;
	}

	/// <summary>
	/// This property returns True if the module has the unattended execution options set, otherwise it returns False.  
	/// This property should only be examined if the <c>IsVBModule</c> property returns True.
	/// </summary>
	[DispId(34)]
	bool UnattendedExecution
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(34)]
		get;
	}

	/// <summary>
	/// This property returns True if the module is single threaded, otherwise it returns False.  This property should only be examined if the IsVBModule property returns True.
	/// </summary>
	[DispId(35)]
	bool SingleThreaded
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(35)]
		get;
	}
}
