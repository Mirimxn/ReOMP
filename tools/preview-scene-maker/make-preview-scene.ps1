# MAKE-PREVIEW-SCENE: drop a vehicle .GHG (model) or .PREFAB_BAKED onto MAKE-PREVIEW-SCENE.bat.
# It writes <MODEL>_PREVIEW.SCENE_BAKED (a roster preview scene for that model) into a PREVIEW folder
# next to the model, and prints the lines to add to the vehicle's mod.json.
# The scene is built from the game's Wookiee Gunship preview (embedded below), pointed at your model.
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Files)
$ErrorActionPreference = "Stop"

$code = @"
using System;
using System.Collections.Generic;
using System.Text;

public static class PreviewSceneMaker
{
    static readonly byte[] OldBody = Encoding.ASCII.GetBytes("Chars/Vehicles/Wookie_Gunship/WOOKIE_GUNSHIP_dx11.ghg");
    static readonly byte[] OldId = Hex("662c515f965f4674a8d840eb0e36fc00");
    static readonly int[] Sizes = { 19368, 19387, 19758 };
    const int BlockEnd = 21944;

    static byte[] Hex(string h)
    {
        var b = new byte[h.Length / 2];
        for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
        return b;
    }
    public static string ToHex(byte[] b) { var sb = new StringBuilder(); foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString(); }
    static int Be16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; }
    static int Be32(byte[] b, int o) { return (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]; }
    static int Le32(byte[] b, int o) { return b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24); }
    static void PutBe16(byte[] b, int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
    static void PutBe32(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
    static void PutLe32(byte[] b, int o, int v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24); }
    static int IndexOf(byte[] h, byte[] n, int start)
    {
        for (int i = start; i + n.Length <= h.Length; i++)
        {
            int j = 0;
            while (j < n.Length && h[i + j] == n[j]) j++;
            if (j == n.Length) return i;
        }
        return -1;
    }
    static int Count(byte[] h, byte[] n) { int c = 0, i = 0; while ((i = IndexOf(h, n, i)) >= 0) { c++; i += n.Length; } return c; }
    static string Title(string s)
    {
        var parts = s.Split('_');
        for (int i = 0; i < parts.Length; i++) if (parts[i].Length > 0) parts[i] = parts[i].Substring(0, 1).ToUpperInvariant() + parts[i].Substring(1).ToLowerInvariant();
        return string.Join("_", parts);
    }

    // Returns { folder, name, idHex } from a GHG's header: its own path (CHARS/VEHICLES/<FOLDER>/<NAME>_DX11.GHG) and id.
    public static string[] GhgInfo(byte[] g)
    {
        if (g.Length < 64 || Encoding.ASCII.GetString(g, 4, 12) != ".CC4HSERHSER") throw new Exception("this is not a GHG model file");
        int n = Be16(g, 20);
        string p = Encoding.ASCII.GetString(g, 22, n - 1).Replace('\\', '/');
        var id = new byte[16]; Array.Copy(g, 22 + n, id, 0, 16);
        var parts = p.Split('/');
        if (parts.Length != 4 || !parts[0].Equals("chars", StringComparison.OrdinalIgnoreCase) || !parts[1].Equals("vehicles", StringComparison.OrdinalIgnoreCase))
            throw new Exception("the model's own path is " + p + "; only models at CHARS/VEHICLES/<FOLDER>/<NAME>_DX11.GHG are supported");
        if (!parts[3].ToUpperInvariant().EndsWith("_DX11.GHG")) throw new Exception("unexpected model name " + parts[3]);
        return new[] { parts[2], parts[3].Substring(0, parts[3].Length - "_DX11.GHG".Length), ToHex(id) };
    }

    public static byte[] Make(byte[] t, string folder, string name, string idHex)
    {
        var d = (byte[])t.Clone();
        // 1. the model path inside the scene's body (an LE length + path, inside three blocks whose sizes grow with it)
        int i = IndexOf(d, OldBody, 0);
        if (i < 4 || Le32(d, i - 4) != OldBody.Length + 1) throw new Exception("unexpected template (model path)");
        var nb = Encoding.ASCII.GetBytes("Chars/Vehicles/" + Title(folder) + "/" + name.ToUpperInvariant() + "_dx11.ghg");
        int delta = nb.Length - OldBody.Length;
        foreach (var q in Sizes)
        {
            int v = Le32(d, q);
            if (q + 4 + v != BlockEnd) throw new Exception("unexpected template (block sizes)");
            PutLe32(d, q, v + delta);
        }
        PutLe32(d, i - 4, nb.Length + 1);
        var body = new List<byte>();
        for (int k = 0; k < i; k++) body.Add(d[k]);
        body.AddRange(nb);
        for (int k = i + OldBody.Length; k < d.Length; k++) body.Add(d[k]);
        d = body.ToArray();

        // 2. the dependency list in the header: folder and model names, each followed by two NULs, padded to 4
        int blen = Be32(d, 117);
        if (blen != 84) throw new Exception("unexpected template (dependency list)");
        var strs = new[] { "chars", "vehicles", folder.ToLowerInvariant(), name.ToLowerInvariant() + "_dx11.ghg", name.ToLowerInvariant() + "_nxg.ghg" };
        var blob = new List<byte>(); var offs = new int[strs.Length];
        for (int k = 0; k < strs.Length; k++) { offs[k] = blob.Count; blob.AddRange(Encoding.ASCII.GetBytes(strs[k])); blob.Add(0); blob.Add(0); }
        while (blob.Count % 4 != 0) blob.Add(0);
        int hd = blob.Count - blen;
        int[] fields = { 235, 247, 259, 271 };
        int[] expect = { 7, 17, 33, 58 };
        for (int k = 0; k < 4; k++) if (Be16(d, fields[k]) != expect[k]) throw new Exception("unexpected template (dependency entries)");
        var outList = new List<byte>();
        for (int k = 0; k < 121; k++) outList.Add(d[k]);
        outList.AddRange(blob);
        for (int k = 121 + blen; k < d.Length; k++) outList.Add(d[k]);
        var o = outList.ToArray();
        PutBe32(o, 117, blob.Count);
        for (int k = 0; k < 4; k++) PutBe16(o, fields[k] + hd, offs[k + 1]);
        PutBe32(o, 0, Be32(d, 0) + hd);

        // 3. the model's id (twice)
        if (Count(o, OldId) != 2) throw new Exception("unexpected template (model id)");
        var nid = Hex(idHex);
        int at = 0;
        while ((at = IndexOf(o, OldId, at)) >= 0) { Array.Copy(nid, 0, o, at, 16); at += 16; }
        return o;
    }
}
"@
Add-Type -TypeDefinition $code -Language CSharp

$templatePath = Join-Path $PSScriptRoot "WOOKIE_GUNSHIP_CHEAP.SCENE_BAKED"
if (-not (Test-Path $templatePath)) {
    Write-Host "Missing template: $templatePath"
    Write-Host "Extract CHARS/VEHICLES/WOOKIE_GUNSHIP/WOOKIE_GUNSHIP_CHEAP.SCENE_BAKED from the game and put it next to this script."
    Read-Host "Press Enter to close" | Out-Null
    exit 1
}
$template = [IO.File]::ReadAllBytes($templatePath)
if ($template.Length -ne 21964) { Write-Host "Unexpected template size $($template.Length) (expected 21964, the vanilla Steam file)."; Read-Host "Press Enter to close" | Out-Null; exit 1 }

function Find-ModRoot($dir) {
    $d = Get-Item $dir
    for ($k = 0; $k -lt 8 -and $d; $k++) {
        if (Test-Path (Join-Path $d.FullName "mod.json")) { return $d.FullName }
        $d = $d.Parent
    }
    return $null
}

function Find-GhgForPrefab($prefab) {
    $text = [System.Text.Encoding]::GetEncoding(28591).GetString([IO.File]::ReadAllBytes($prefab))
    $names = [regex]::Matches($text, '[A-Za-z0-9_]+_dx11\.ghg', 'IgnoreCase') | ForEach-Object { $_.Value } | Select-Object -Unique
    if (-not $names) { throw "the prefab doesn't reference any _dx11.ghg model" }
    $root = Find-ModRoot (Split-Path $prefab)
    if (-not $root) { $root = Split-Path $prefab }
    foreach ($n in $names) {
        $hit = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -ieq $n } | Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    throw "the prefab uses $($names -join ', ') but that file isn't in this mod folder. Drop the .GHG itself instead."
}

if (-not $Files -or $Files.Count -eq 0) {
    Write-Host "Drag a vehicle .GHG (or its .PREFAB_BAKED) onto MAKE-PREVIEW-SCENE.bat."
    Read-Host "Press Enter to close"; exit
}

foreach ($f in $Files) {
    Write-Host ""
    Write-Host "==> $f" -ForegroundColor Cyan
    try {
        if ($f -match '\.prefab_baked$') { $ghg = Find-GhgForPrefab $f; Write-Host "   model: $ghg" } else { $ghg = $f }
        $info = [PreviewSceneMaker]::GhgInfo([IO.File]::ReadAllBytes($ghg))
        $folder, $name, $id = $info
        $scene = [PreviewSceneMaker]::Make($template, $folder, $name, $id)
        $outDir = Join-Path (Split-Path $ghg) "PREVIEW"
        New-Item -ItemType Directory -Force $outDir | Out-Null
        $out = Join-Path $outDir ("{0}_PREVIEW.SCENE_BAKED" -f $name.ToUpperInvariant())
        [IO.File]::WriteAllBytes($out, $scene)
        Write-Host "   model CHARS/VEHICLES/$folder/$($name)_DX11.GHG, id $id"
        Write-Host "   wrote $out" -ForegroundColor Green
        $root = Find-ModRoot (Split-Path $ghg)
        $rel = if ($root) { $out.Substring($root.Length).TrimStart('\').Replace('\', '/') } else { $out }
        Write-Host ""
        Write-Host "   Add to the vehicle's payload in mod.json (pick a previewBase from"
        Write-Host "   omp-data\vehicle-previews\preview-bases.txt in the game folder):"
        Write-Host "     `"previewScene`": `"$rel`","
        Write-Host "     `"previewBase`": `"Micro_Vulture_Droid`""
    }
    catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""
Read-Host "Press Enter to close"
