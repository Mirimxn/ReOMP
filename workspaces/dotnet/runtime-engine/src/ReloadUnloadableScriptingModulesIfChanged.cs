using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    public static void ReloadUnloadableScriptingModulesIfChanged()
    {
        bool _wasAnyScriptingModuleAssemblyChanged = false;

        foreach (var scriptingModuleContext in _scriptingModuleContexts)
        {
            if (DateTime.Now - scriptingModuleContext.ScriptingModuleAssemblyChangedDateTime > TimeSpan.FromSeconds(1))
            {
                Console.WriteLine($"Reloading scripting module {scriptingModuleContext.ScriptingModuleInfo.AssemblyPath}");

                _wasAnyScriptingModuleAssemblyChanged = true;

                scriptingModuleContext.ScriptingModuleAssemblyChangedDateTime = null;

                UnloadScriptingModuleIfLoadedAndUnloadable(scriptingModuleContext);
            }
        }

        if (_wasAnyScriptingModuleAssemblyChanged)
        {
            LoadUnloadedScriptingModules();
        }
    }
}