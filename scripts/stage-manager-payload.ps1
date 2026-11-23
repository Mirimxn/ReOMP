# Stages the OMP game-runtime files the Mod Manager installs into game folders.
#
# Takes the release ZIP from package-release.ps1, copies its game-root runtime
# (proxy DLL, driver, driver config, runtime engine and .NET runtime) into
# -OutDir and writes payload.json with every file's SHA-256. The manager reads
# payload.json from its bundled resources.
#
# Each core mod in the release's mods/ folder is packed as an ordinary mod ZIP
# in -CoreModsOut. Its version and dependency minimums come from
# mod-sources/<id>/mod.json, so a component kept from the upstream bundle is
# versioned the same way as one rebuilt here.
# See plans/mod-manager.md#game-targets-and-omp-payload.
#
# Usage:
#   ./scripts/stage-manager-payload.ps1 -ReleaseZip omp-release.zip -OutDir apps/mod-manager/omp-payload `
#       -CoreModsOut apps/mod-manager/core-mods -Version 0.1.0

param(
    [Parameter(Mandatory)][string]$ReleaseZip,
    [Parameter(Mandatory)][string]$OutDir,
    [Parameter(Mandatory)][string]$CoreModsOut,
    [Parameter(Mandatory)][string]$Version,
    [string]$ModSources = (Join-Path $PSScriptRoot "../mod-sources")
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$ErrorActionPreference = "Stop"

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("omp-payload-" + [System.Guid]::NewGuid().ToString("N"))
Expand-Archive -Path $ReleaseZip -DestinationPath $work -Force

# Every item must be present; versioned folders are matched by prefix.
$required = @(
    @{ Name = "dinput8.dll"; Kind = "file" },
    @{ Name = "omp-lswtss-driver.dll"; Kind = "file" },
    @{ Name = "omp-lswtss-driver-config.json"; Kind = "file" },
    @{ Name = "omp-lswtss-runtime-engine-*"; Kind = "dir" },
    @{ Name = "omp-lswtss-dotnet-runtime-*"; Kind = "dir" }
)

if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

foreach ($item in $required) {
    $found = @(Get-ChildItem -Path $work -Filter $item.Name | Where-Object {
        ($item.Kind -eq "dir") -eq $_.PSIsContainer
    })
    if ($found.Count -ne 1) {
        throw "The OMP release must contain exactly one $($item.Name) $($item.Kind); found $($found.Count)."
    }
    Copy-Item -Path $found[0].FullName -Destination $OutDir -Recurse -Force
    Write-Host "    staged $($found[0].Name)"
}

$root = (Resolve-Path $OutDir).Path
$files = Get-ChildItem -Path $root -Recurse -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{
        path   = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
        sha256 = (Get-FileHash -Algorithm SHA256 -Path $_.FullName).Hash.ToLowerInvariant()
    }
}
if (@($files).Count -eq 0) { throw "No payload files were staged." }

$payload = [ordered]@{ version = $Version; files = @($files) }
$payload | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $root "payload.json") -Encoding utf8NoBOM
Write-Host "==> Staged $(@($files).Count) OMP files for version $Version in $root"

# ---- core mods as versioned ZIPs
if (Test-Path $CoreModsOut) { Remove-Item $CoreModsOut -Recurse -Force }
New-Item -ItemType Directory -Force -Path $CoreModsOut | Out-Null
$modsDir = Join-Path $work "mods"
$packed = 0
foreach ($mod in @(Get-ChildItem -Path $modsDir -Directory -ErrorAction SilentlyContinue)) {
    $id = $mod.Name
    $sourceManifest = Join-Path $ModSources "$id/mod.json"
    if (-not (Test-Path $sourceManifest)) { throw "Core mod $id has no source manifest at $sourceManifest." }
    $source = Get-Content $sourceManifest -Raw | ConvertFrom-Json
    if (-not $source.version) { throw "$sourceManifest has no version." }

    $manifestPath = Join-Path $mod.FullName "mod.json"
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $manifest | Add-Member -NotePropertyName version -NotePropertyValue $source.version -Force
    if ($null -eq $manifest.actions) { $manifest | Add-Member -NotePropertyName actions -NotePropertyValue @() -Force }
    $dependencies = @(foreach ($dependency in @($manifest.dependencies | Where-Object { $_ })) {
        $declared = @($source.dependencies | Where-Object { $_.id -eq $dependency.id })
        if ($declared.Count -ne 1 -or -not $declared[0].minVersion) {
            throw "$sourceManifest doesn't give a minVersion for dependency $($dependency.id)."
        }
        [ordered]@{ id = $dependency.id; minVersion = $declared[0].minVersion }
    })
    $manifest | Add-Member -NotePropertyName dependencies -NotePropertyValue $dependencies -Force
    ConvertTo-Json -InputObject $manifest -Depth 32 | Set-Content -Path $manifestPath -Encoding utf8NoBOM

    $zipPath = Join-Path $CoreModsOut "$id-$($source.version).zip"
    [System.IO.Compression.ZipFile]::CreateFromDirectory($mod.FullName, $zipPath,
        [System.IO.Compression.CompressionLevel]::Optimal, $true)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $bad = @($zip.Entries | Where-Object { $_.FullName.Contains('\') -or -not $_.FullName.StartsWith("$id/") })
        if ($bad.Count -gt 0) { throw "$zipPath has entries outside $id/: $($bad[0].FullName)" }
    } finally { $zip.Dispose() }
    Write-Host "    packed $id $($source.version)"
    $packed++
}
if ($packed -eq 0) { throw "The OMP release contains no core mods." }
# The release must carry this repository's v1 with asset-replacement, not the
# upstream v1 that installs the file hooks itself.
if (-not (Test-Path (Join-Path $CoreModsOut "asset-replacement-*.zip"))) {
    throw "The OMP release has no asset-replacement mod; v1 or asset-replacement failed to build (see 'Build the OMP release')."
}
Write-Host "==> Packed $packed core mods in $CoreModsOut"
Remove-Item $work -Recurse -Force
