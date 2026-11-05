using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    public static void UnloadUnloadableScriptingModules()
    {
        Console.WriteLine("Unloading unloadable scripting modules...");

        foreach (var scriptingModuleContext in _scriptingModuleContexts)
        {
            UnloadScriptingModuleIfLoadedAndUnloadable(scriptingModuleContext);
        }

        Console.WriteLine("Unloaded unloadable scripting modules!");
    }
}