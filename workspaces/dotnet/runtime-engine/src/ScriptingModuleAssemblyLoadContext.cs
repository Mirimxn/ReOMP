using System;
using System.Reflection;
using System.Runtime.Loader;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    class ScriptingModuleAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        private readonly string scriptingModuleAssemblyPath;

        public ScriptingModuleAssemblyLoadContext(string scriptingModuleAssemblyPath) : base(true)
        {
            this.scriptingModuleAssemblyPath = scriptingModuleAssemblyPath;
            _resolver = new AssemblyDependencyResolver(scriptingModuleAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            Console.WriteLine("Requesting Scripting Module assembly: " + assemblyName.Name + " for " + scriptingModuleAssemblyPath);

            var sharedAssemblyInfo = _sharedAssembliesInfo.Find(assemblyInfo => assemblyInfo.Name == assemblyName.Name);

            if (sharedAssemblyInfo != null)
            {
                var sharedAssembly = Assembly.LoadFrom(sharedAssemblyInfo.Path);

                Console.WriteLine("Loaded shared assembly from: " + sharedAssemblyInfo.Path);

                return sharedAssembly;
            }
            else
            {
                var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

                if (assemblyPath != null)
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);

                    Console.WriteLine("Loaded global assembly from: " + assemblyPath);

                    return assembly;
                }
            }

            Console.WriteLine("Falling back to default assembly resolver: " + assemblyName.Name);

            return null;
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            Console.WriteLine("Requesting unmanaged DLL: " + unmanagedDllName + " for " + scriptingModuleAssemblyPath);

            var unmanagedDllPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            if (unmanagedDllPath != null)
            {
                Console.WriteLine("Loaded unmanaged DLL from :" + unmanagedDllPath);

                return LoadUnmanagedDllFromPath(unmanagedDllPath);
            }

            Console.WriteLine("Falling back to default unmanaged DLL resolver: " + unmanagedDllName);

            return nint.Zero;
        }
    }
}