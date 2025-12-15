using System;
using System.Runtime.Serialization;
using System.Xml;

namespace DebugDiag.DotNet.x86Analysis;

public class SharedTypeResolver : DataContractResolver
{
	public override bool TryResolveType(Type dataContractType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
	{
		if (!knownTypeResolver.TryResolveType(dataContractType, declaredType, null, out typeName, out typeNamespace))
		{
			XmlDictionary xmlDictionary = new XmlDictionary();
			typeName = xmlDictionary.Add(dataContractType.FullName);
			typeNamespace = xmlDictionary.Add(dataContractType.Assembly.FullName);
		}
		return true;
	}

	public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
	{
		return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null) ?? Type.GetType(typeName + ", " + typeNamespace);
	}
}
