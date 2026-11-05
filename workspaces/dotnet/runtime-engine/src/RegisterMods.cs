using System;
using System.IO;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void RegisterMods()
    {
        var modsDirPath = Path.Combine(Environment.CurrentDirectory, "mods");

        if (!Directory.Exists(modsDirPath))
        {
            return;
        }

        foreach (var modDirPath in Directory.GetDirectories(modsDirPath))
        {
            try
            {
                var modId = Path.GetFileName(modDirPath);

                var modInfoFilePath = Path.Combine(modDirPath, "mod.json");

                var modInfoAsJson = File.ReadAllText(modInfoFilePath);

                ModInfo modInfo = JsonConvert.DeserializeObject<ModInfo>(modInfoAsJson) ?? throw new InvalidOperationException();

                _modsState.Add(
                    new ModState
                    {
                        Id = modId,
                        DirPath = modDirPath,
                        Info = modInfo,
                        IsLoaded = false,
                        IsBeingLoaded = false,
                    }
                );

                Console.WriteLine($"Registered mod from {modDirPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to register mod from {modDirPath}");
                Console.WriteLine(e.ToString());
            }
        }
    }
}