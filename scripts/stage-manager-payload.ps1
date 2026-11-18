# Stages the OMP game-runtime files the Mod Manager installs into game folders.
#
# Takes the release ZIP from package-release.ps1, copies its game-root runtime
# (proxy DLL, driver, driver config, runtime engine and .NET runtime) into
# -OutDir and writes payload.json with every file's SHA-256. The manager reads
# payload.json from its bundled resources. Core mods in the release's mods/
# folder are not part of this payload; they ship as mod ZIPs.
# See plans/mod-manager.md#game-targets-and-omp-payload.
#
# Usage:
#   ./scripts/stage-manager-payload.ps1 -ReleaseZip omp-release.zip -OutDir apps/mod-manager/omp-payload -Version 0.1.0

param(
    [Parameter(Mandatory)][string]$ReleaseZip,
    [Parameter(Mandatory)][string]$OutDir,
    [Parameter(Mandatory)][string]$Version
)

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
Remove-Item $work -Recurse -Force
