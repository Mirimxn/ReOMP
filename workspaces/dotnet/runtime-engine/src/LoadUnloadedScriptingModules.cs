using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    public static void LoadUnloadedScriptingModules()
    {
        Console.WriteLine("Loading unloaded scripting modules...");

        foreach (var scriptingModuleContext in _scriptingModuleContexts)
        {
            try
            {
                LoadScriptingModuleIfUnloaded(scriptingModuleContext);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to load scripting module '"
                    + scriptingModuleContext.ScriptingModuleInfo.TypeName
                    + "' from '"
                    + scriptingModuleContext.ScriptingModuleInfo.AssemblyPath
                    + "'. Skipping it so other modules and the game keep running."
                );
                Console.WriteLine(exception);

                // Roll back any half-initialized state so the failed module is
                // left cleanly unloaded rather than partially loaded.
                scriptingModuleContext.ScriptingModule = null;
                scriptingModuleContext.ScriptingModuleAssemblyLoadContext?.Unload();
                scriptingModuleContext.ScriptingModuleAssemblyLoadContext = null;
            }
        }

        Console.WriteLine("Loaded unloaded scripting modules!");
    }
}