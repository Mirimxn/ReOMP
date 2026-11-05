using System;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ScriptingModuleContext
    {
        public required ScriptingModuleInfo ScriptingModuleInfo { get; set; }

        public System.IO.FileSystemWatcher? ScriptingModuleAssemblyWatcher;

        public DateTime? ScriptingModuleAssemblyChangedDateTime;

        public ScriptingModuleAssemblyLoadContext? ScriptingModuleAssemblyLoadContext;

        public object? ScriptingModule;
    }
}