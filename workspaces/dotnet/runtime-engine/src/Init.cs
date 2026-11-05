using System;
using System.Reflection;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    public static int Init(IntPtr args, int sizeBytes)
    {
        Console.WriteLine("Initializing Runtime Engine...");

        AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
        {
            var assemblyName = new AssemblyName(eventArgs.Name).Name;

            Console.WriteLine("Requesting CurrentDomain assembly: " + assemblyName + " for " + eventArgs.RequestingAssembly?.Location ?? "null");

            var sharedAssemblyInfo = _sharedAssembliesInfo.Find(assemblyInfo => assemblyInfo.Name == assemblyName);

            if (sharedAssemblyInfo != null)
            {
                var assembly = Assembly.LoadFrom(sharedAssemblyInfo.Path);

                Console.WriteLine("Loaded shared assembly from: " + sharedAssemblyInfo.Path);

                return assembly;
            }

            if (assemblyName == "omp-lswtss-runtime-engine")
            {
                Console.WriteLine("Returning omp-lswtss-runtime-engine assembly");

                return Assembly.GetAssembly(typeof(RuntimeEngine));
            }

            Console.WriteLine("Falling back to default assembly resolver: " + assemblyName);

            return null;
        };

        Console.WriteLine("Runtime Engine initialized!");

        RegisterMods();

        LoadMods();

        foreach (var sharedAssemblyInfo in _sharedAssembliesInfo)
        {
            Assembly.LoadFrom(sharedAssemblyInfo.Path);
        }

        LoadUnloadedScriptingModules();

        return 1138;
    }
}