using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

/// <summary>
/// Owns the game's background-character palette, <c>SYSTEMS/POPULATION/CHARACTERPALETTEDATA.PRJ</c>.
/// Mods declare their colour changes with <c>population-palette/set</c>; the Mod Manager writes the
/// game's palette with every enabled mod's changes to <c>merged/CHARACTERPALETTEDATA.PRJ</c> in this
/// mod's folder when it installs, and <c>population-palette/serve</c> hands that file to the game.
/// </summary>
public static class PopulationPalette
{
    public const string GamePath = "SYSTEMS/POPULATION/CHARACTERPALETTEDATA.PRJ";
    public const string MergedSrcPath = "merged/CHARACTERPALETTEDATA.PRJ";

    static readonly object Gate = new();
    static readonly List<(string ModId, string Character, string Element)> Declared = new();
    static bool _serving;

    /// <summary>The changes enabled mods declared, by mod.</summary>
    public static IReadOnlyList<(string ModId, string Character, string Element)> DeclaredChanges
    {
        get { lock (Gate) return Declared.ToArray(); }
    }

    static void Log(string message)
    {
        Console.WriteLine($"[population-palette] {message}");
    }

    sealed class SetPayload
    {
        [JsonProperty("character")]
        public string? Character { get; set; }

        [JsonProperty("element")]
        public string? Element { get; set; }

        [JsonProperty("palette")]
        public int? Palette { get; set; }

        [JsonProperty("apply")]
        public bool? Apply { get; set; }

        [JsonProperty("colour")]
        public string? Colour { get; set; }
    }

    /// <summary>
    /// Handler for <c>population-palette/set</c>. The change itself is already in the file the
    /// Mod Manager wrote; this checks the declaration and records it.
    /// </summary>
    public static class SetAction
    {
        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            var payload = JsonConvert.DeserializeObject<SetPayload>(modActionPayloadAsJson);
            if (payload == null || string.IsNullOrWhiteSpace(payload.Character) || string.IsNullOrWhiteSpace(payload.Element)
                || (payload.Apply == null && payload.Colour == null))
            {
                Log($"{modId}: a population-palette/set action needs character, element and apply or colour; it was ignored");
                return;
            }
            lock (Gate)
            {
                Declared.Add((modId, payload.Character, payload.Element));
                if (!_serving)
                {
                    Log($"{modId}: {payload.Character} {payload.Element} isn't in a combined palette; install the mod with the ReOMP Mod Manager to apply it");
                }
            }
        }
    }

    /// <summary>Handler for <c>population-palette/serve</c>, run from this mod's own manifest.</summary>
    public static class ServeAction
    {
        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            if (!File.Exists(Path.Combine(modDirPath, MergedSrcPath)))
            {
                Log("no mod changes background-character colours; the game's own palette is used");
                return;
            }
            AssetReplacement.InitIfNotInitialized();
            var served = AssetReplacement.RegisterModFile(modId, modDirPath, GamePath, MergedSrcPath);
            lock (Gate)
            {
                _serving = served;
            }
            Log(served ? "serving the combined background-character palette" : "couldn't serve the combined palette (see asset-replacement's log)");
        }
    }
}
