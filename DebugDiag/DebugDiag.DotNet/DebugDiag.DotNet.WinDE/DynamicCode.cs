using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DebugDiag.DotNet.WinDE;

internal class DynamicCode
{
	public static object EvalCSharp(string cSharpSnippet, Type returnType, NetDbgObj debugger)
	{
		DumpInfo dumpInfo = new DumpInfo
		{
			PropertyName = "",
			QWord = 0uL,
			StringValue = ""
		};
		cSharpSnippet = TransformAnnotationSnippet(cSharpSnippet);
		return CompileDynamicCode(WrapAnnotationSnippet(cSharpSnippet, "DynamicClass", "Eval", returnType), "c#", "DynamicClass", "Eval").Invoke(null, new object[2] { debugger, dumpInfo });
	}

	private static MethodInfo CompileDynamicCode(string dynamicCode, string language, string className, string methodName)
	{
		return ThrowIfCompilationError(CodeDomProvider.CreateProvider(language).CompileAssemblyFromSource(new CompilerParameters
		{
			ReferencedAssemblies = 
			{
				typeof(NetDbgObj).Assembly.Location,
				typeof(Queryable).Assembly.Location
			}
		}, dynamicCode)).CompiledAssembly.GetType(className).GetMethod(methodName);
	}

	private static string TransformAnnotationSnippet(string cSharpSnippet)
	{
		return cSharpSnippet.Replace("DumpInfo.Any(", "debugger.MexDumpInfo.Any(di => ").Replace("PropertyName ", "di.PropertyName ").Replace("QWord ", "di.QWord ")
			.Replace("Threads.Any(", "debugger.MexGetThreads().Any(ti => ")
			.Replace("CallStack.Any(", "ti.Stack.StackFramesList.Any(sf => ")
			.Replace("ModuleName ==", "sf.Module ==")
			.Replace("FunctionName ==", "sf.Module ==");
	}

	private static string WrapAnnotationSnippet(string cSharpSnippet, string className, string methodName, Type returnType)
	{
		return string.Format("\r\n                using DebugDiag.DotNet;\r\n                using DebugDiag.DotNet.WinDE;\r\n                using Microsoft.Mex.Framework;\r\n                using System.Linq;\r\n\r\n                public class {0}\r\n                {{\r\n                    public static {1} {2}(NetDbgObj debugger, DumpInfo DumpInfo)\r\n                    {{\r\n                        return {3};\r\n                    }}\r\n                }}", className, (returnType == null) ? "void" : returnType.Name, methodName, cSharpSnippet);
	}

	private static CompilerResults ThrowIfCompilationError(CompilerResults results)
	{
		if (results.Errors.Count > 0)
		{
			string arg = ((results.Errors.Count == 1) ? "" : "x");
			StringBuilder stringBuilder = new StringBuilder($"Compilation Error{arg}:\r\n");
			foreach (object error in results.Errors)
			{
				stringBuilder.AppendLine(error.ToString());
			}
			throw new Exception(stringBuilder.ToString());
		}
		return results;
	}
}
