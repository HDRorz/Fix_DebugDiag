# Fix_DebugDiag

修复微软官方[DebugDiag工具](https://www.microsoft.com/en-in/download/details.aspx?id=103453)无法分析.net4.8的dump文件的问题
主要将[Microsoft.Diagnostics.Runtime.dll](https://www.nuget.org/packages/Microsoft.Diagnostics.Runtime/1.1.142101)从0.9.2升级到1.1.2

使用时需要升级替换<br>
/AnalysisRules/DebugDiag.AnalysisRules.dll<br>
/ClrMemDiagExt.dll<br>
/DebugDiag.DotNet.dll<br>
/Microsoft.Diagnostics.Runtime.dll<br>


