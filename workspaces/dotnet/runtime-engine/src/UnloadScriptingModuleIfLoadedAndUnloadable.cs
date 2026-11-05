using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void UnloadScriptingModuleIfLoadedAndUnloadable(ScriptingModuleContext scriptingModuleContext)
    {
        if (
            scriptingModuleContext.ScriptingModule == null
            &&
            scriptingModuleContext.ScriptingModuleAssemblyLoadContext == null
        )
        {
            return;
        }

        if (scriptingModuleContext.ScriptingModule != null)
        {
            if (scriptingModuleContext.ScriptingModule is IDisposable == false)
            {
                return;
            }

            ((IDisposable)scriptingModuleContext.ScriptingModule).Dispose();
            scriptingModuleContext.ScriptingModule = null;
        }

        if (scriptingModuleContext.ScriptingModuleAssemblyLoadContext != null)
        {
            scriptingModuleContext.ScriptingModuleAssemblyLoadContext.Unload();
            scriptingModuleContext.ScriptingModuleAssemblyLoadContext = null;
        }
    }
}