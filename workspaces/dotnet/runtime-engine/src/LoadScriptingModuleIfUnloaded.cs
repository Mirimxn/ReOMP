using System;
using System.IO;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void LoadScriptingModuleIfUnloaded(ScriptingModuleContext scriptingModuleContext)
    {
        if (
            scriptingModuleContext.ScriptingModule != null
            ||
            scriptingModuleContext.ScriptingModuleAssemblyLoadContext != null
        )
        {
            return;
        }

        scriptingModuleContext.ScriptingModuleAssemblyLoadContext = new ScriptingModuleAssemblyLoadContext(
            scriptingModuleContext.ScriptingModuleInfo.AssemblyPath
        );

        var scriptingModuleAssembly = scriptingModuleContext.ScriptingModuleAssemblyLoadContext.LoadFromStream(
            new FileStream(scriptingModuleContext.ScriptingModuleInfo.AssemblyPath, FileMode.Open, FileAccess.Read)
        );

        var scriptingModuleType = scriptingModuleAssembly.GetType(
            scriptingModuleContext.ScriptingModuleInfo.TypeName
        ) ?? throw new InvalidOperationException();

        scriptingModuleContext.ScriptingModule = Activator.CreateInstance(scriptingModuleType) ?? throw new InvalidOperationException();
    }
}