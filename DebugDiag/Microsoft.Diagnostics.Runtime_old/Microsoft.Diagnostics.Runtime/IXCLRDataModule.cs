using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Runtime;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("88E32849-0A0A-4cb0-9022-7CD2E9E139E2")]
internal interface IXCLRDataModule
{
	void StartEnumAssemblies_do_not_use();

	void EnumAssembly_do_not_use();

	void EndEnumAssemblies_do_not_use();

	void StartEnumTypeDefinitions_do_not_use();

	void EnumTypeDefinition_do_not_use();

	void EndEnumTypeDefinitions_do_not_use();

	void StartEnumTypeInstances_do_not_use();

	void EnumTypeInstance_do_not_use();

	void EndEnumTypeInstances_do_not_use();

	void StartEnumTypeDefinitionsByName_do_not_use();

	void EnumTypeDefinitionByName_do_not_use();

	void EndEnumTypeDefinitionsByName_do_not_use();

	void StartEnumTypeInstancesByName_do_not_use();

	void EnumTypeInstanceByName_do_not_use();

	void EndEnumTypeInstancesByName_do_not_use();

	void GetTypeDefinitionByToken_do_not_use();

	void StartEnumMethodDefinitionsByName_do_not_use();

	void EnumMethodDefinitionByName_do_not_use();

	void EndEnumMethodDefinitionsByName_do_not_use();

	void StartEnumMethodInstancesByName_do_not_use();

	void EnumMethodInstanceByName_do_not_use();

	void EndEnumMethodInstancesByName_do_not_use();

	void GetMethodDefinitionByToken_do_not_use();

	void StartEnumDataByName_do_not_use();

	void EnumDataByName_do_not_use();

	void EndEnumDataByName_do_not_use();

	void GetName_do_not_use();

	void GetFileName_do_not_use();

	void GetFlags_do_not_use();

	void IsSameObject_do_not_use();

	void StartEnumExtents(out ulong handle);

	void EnumExtent(ref ulong handle, out CLRDATA_MODULE_EXTENT extent);

	void EndEnumExtents(ulong handle);

	void Request_do_not_use();

	void StartEnumAppDomains_do_not_use();

	void EnumAppDomain_do_not_use();

	void EndEnumAppDomains_do_not_use();

	void GetVersionId_do_not_use();
}
