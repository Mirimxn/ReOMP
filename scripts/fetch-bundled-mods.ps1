# Downloads the default mod ZIPs listed in bundled-mods.json from their GitHub
# release and checks each SHA-256. Needs GH_TOKEN with read access.
#
# Usage:
#   ./scripts/fetch-bundled-mods.ps1 -OutDir apps/mod-manager/default-mods

param(
    [Parameter(Mandatory)][string]$OutDir,
    [string]$List = (Join-Path $PSScriptRoot "../bundled-mods.json"),
    [string]$Repo = $env:GITHUB_REPOSITORY
)

$ErrorActionPreference = "Stop"
$config = Get-Content $List -Raw | ConvertFrom-Json
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

foreach ($mod in $config.mods) {
    gh release download $config.release --repo $Repo --pattern $mod.file --dir $OutDir
    if ($LASTEXITCODE -ne 0) {
        throw "Couldn't download $($mod.file) from release $($config.release). Upload it to that release on GitHub."
    }
    $actual = (Get-FileHash -Algorithm SHA256 -Path (Join-Path $OutDir $mod.file)).Hash.ToLowerInvariant()
    if ($actual -ne $mod.sha256) {
        throw "$($mod.file) has SHA-256 $actual but bundled-mods.json expects $($mod.sha256)."
    }
    Write-Host "    fetched $($mod.file)"
}
Write-Host "==> Fetched $(@($config.mods).Count) default mods into $OutDir"
