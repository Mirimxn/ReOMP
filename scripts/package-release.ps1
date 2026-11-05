# Builds a release bundle without needing the EGS version.
#
# Downloads the latest upstream release bundle (which provides a prebuilt
# c-api1 and the prebuilt DLLs the .NET components compile against), rebuilds
# whichever components your toolchain can build, overlays them onto the bundle,
# and zips the result. Optionally publishes it.
#
# Any component that fails to build (e.g. a missing toolchain) is kept from the
# upstream release. c-api1 is only rebuilt if you pass both -SteamDir and
# -EgsDir, because building it launches each game version to scrape its native
# API; without them it is kept from upstream like any other un-buildable
# component.
#
# Usage:
#   ./scripts/package-release.ps1
#   ./scripts/package-release.ps1 -SteamDir "C:\...\Steam game" -EgsDir "C:\...\EGS game"
#   ./scripts/package-release.ps1 -Tag v0.5.1 -PublishRepo <owner>/<repo> -Publish
#
# Requires: dotnet, gh (authenticated). Run from anywhere in the repo.

param(
    [string]$Tag,
    [string]$Title,
    [string]$Notes,
    [string]$UpstreamRepo = "open-modding-platform/omp-lswtss",
    [string]$PublishRepo,
    [string]$OutZip = "omp-release.zip",
    [string]$SteamDir,
    [string]$EgsDir,
    [string]$DotnetRuntime,
    # Same runtime OMP downloads itself (see download_dotnet_runtime_if_missing.rs).
    [string]$DotnetRuntimeUrl = "https://download.visualstudio.microsoft.com/download/pr/8abf4502-4a22-4a2e-bea0-9fe73379d62e/88146c1d41e53e08f9dbc92a217143de/dotnet-runtime-8.0.2-win-x64.zip",
    # Branch/commit the release tag points at (e.g. steam-deck). Defaults to the
    # repo's default branch.
    [string]$Target,
    [switch]$Prerelease,
    [switch]$Publish
)

$ErrorActionPreference = "Stop"

$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$devTools = Join-Path $repoRoot "workspaces/dotnet/dev-tools"
$distDir  = Join-Path $repoRoot "dist"

# 1. Download and extract the latest upstream release bundle.
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("omp-release-" + [System.Guid]::NewGuid().ToString("N"))
$bundleDir = Join-Path $work "bundle"
New-Item -ItemType Directory -Force -Path $bundleDir | Out-Null

Write-Host "==> Downloading latest upstream release from $UpstreamRepo..."
gh release download --repo $UpstreamRepo --pattern "*.zip" --dir $work
$upstreamZip = Get-ChildItem -Path $work -Filter "*.zip" | Select-Object -First 1
Expand-Archive -Path $upstreamZip.FullName -DestinationPath $bundleDir -Force

# 2. Seed dist/ from the bundle's prebuilt components. The .NET components
#    compile against DLLs in dist/ (e.g. overlay1 needs c-func-hook1's
#    CFuncHook1), and those come from components that need Rust/Node to build.
#    Seeding from the upstream bundle lets the .NET components build without
#    that toolchain; a component you do rebuild then overwrites its own dist
#    folder.
Write-Host "==> Seeding dist/ from upstream bundle..."
# Native CEF files in dist/overlay1 must not be overwritten by the upstream bundle.
# The current upstream and NuGet-restored libcef.dll crashes on startup under Wine/Proton.
# A known-good version lives in dist/overlay1/ and must be preserved across seeding runs.
$protectedNatives = @(
    "libcef.dll", "chrome_elf.dll", "libEGL.dll", "libGLESv2.dll",
    "d3dcompiler_47.dll", "dxcompiler.dll", "dxil.dll",
    "icudtl.dat", "chrome_100_percent.pak", "chrome_200_percent.pak", "Ijwhost.dll"
)
Get-ChildItem -Path (Join-Path $bundleDir "mods") -Directory | ForEach-Object {
    $modName = $_.Name
    $src     = $_.FullName
    $dst     = Join-Path $distDir $modName
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    Get-ChildItem -Path $src | ForEach-Object {
        $dstItem = Join-Path $dst $_.Name
        if ($_.PSIsContainer) {
            # Subdirectory: copy only if not already present (e.g. locales/).
            if (-not (Test-Path $dstItem)) {
                Copy-Item -Path $_.FullName -Destination $dstItem -Recurse -Force
            }
        } elseif ($modName -eq "overlay1" -and $protectedNatives -contains $_.Name -and (Test-Path $dstItem)) {
            # Keep existing native CEF file rather than seeding the upstream version.
        } else {
            Copy-Item -Path $_.FullName -Destination $dstItem -Force
        }
    }
}

# The dev-tools build/install commands resolve their paths relative to the
# current directory and assume it is the dev-tools folder, so run them from
# there. All paths handed to them below are absolute.
Push-Location $devTools
try {

# 3. Build every component except c-api1. Build each separately and continue
#    past failures, so a missing toolchain (Rust for c-func-hook1/bundle,
#    Node/Yarn for galaxy-unleashed) only skips that component - its seeded
#    upstream version is then kept - instead of aborting the whole build.
Write-Host "==> Building components..."
# Try every component. Some need an extra toolchain (Rust/cargo, Node/Yarn) and
# will fail without it - that's fine, the seeded upstream version is kept. A
# failed build may have wiped its own dist/ folder (which other components
# reference), so on failure we restore it from the bundle. This avoids having
# to hard-code which component needs which toolchain.
$components = @(
    "overlay1", "input-hook1", "v1", "debug-tools", "test-cef-mod",
    "c-func-hook1", "galaxy-unleashed", "bundle"
)
# c-api1 needs both game versions to build (it launches each to scrape its API),
# so only attempt it when both directories are given; otherwise it is kept from
# upstream like any other component that can't be built here.
if ($SteamDir -and $EgsDir) { $components += "c-api1" }

$built = @()
foreach ($c in $components) {
    Write-Host "    build-$c"
    # Capture output so a successful build stays quiet but a failed one shows why
    # inline. Pass args directly (NOT splatted - splatting breaks the build).
    # c-api1 takes the two game directories; the rest take none.
    if ($c -eq "c-api1") {
        $buildOutput = dotnet run -- build-c-api1 $SteamDir $EgsDir 2>&1 | Out-String
    } else {
        $buildOutput = dotnet run -- "build-$c" 2>&1 | Out-String
    }
    if ($LASTEXITCODE -eq 0) {
        $built += $c
    } else {
        $reason = ($buildOutput -split "`r?`n" |
            Where-Object { $_ -match 'error |not recognized|Exception' } |
            Select-Object -First 4) -join "`n        "
        Write-Warning "build-$c failed; keeping upstream's $c."
        if ($reason) { Write-Host "        reason: $reason" }
        # Restore the seeded reference the failed build may have wiped.
        $seedSrc = Join-Path $bundleDir "mods/$c"
        if (Test-Path $seedSrc) {
            $seedDst = Join-Path $distDir $c
            Remove-Item $seedDst -Recurse -Force -ErrorAction SilentlyContinue
            New-Item -ItemType Directory -Force -Path $seedDst | Out-Null
            Copy-Item -Path (Join-Path $seedSrc "*") -Destination $seedDst -Recurse -Force
        }
    }
}
Write-Host "    built: $($built -join ', ')"

# 4. Overlay the components that actually built onto the bundle, reusing the
#    install-* commands. Anything not in $built keeps its upstream version.
Write-Host "==> Overlaying rebuilt components..."
if ($built -contains "bundle") {
    Write-Host "    install-bundle"
    dotnet run -- install-bundle $bundleDir
}
Get-ChildItem -Path (Join-Path $bundleDir "mods") -Directory |
    Where-Object { $built -contains $_.Name } |
    ForEach-Object {
        Write-Host "    install-$($_.Name)"
        dotnet run -- "install-$($_.Name)" $bundleDir
    }

} finally {
    Pop-Location
}

# 4b. Bundle bcryptprimitives.dll (Wine 9.x implementation, LGPL). GE-Proton8-21
#     uses Wine 8.0 which registers this DLL by name but ships no implementation.
#     omp-lswtss-driver.dll imports it; without it the mod silently fails to load.
#     Wine finds it in the game directory because that is on the DLL search path.
$bcryptSrc = Join-Path $PSScriptRoot "wine-system32/bcryptprimitives.dll"
if (Test-Path $bcryptSrc) {
    Copy-Item -Path $bcryptSrc -Destination (Join-Path $bundleDir "bcryptprimitives.dll") -Force
    Write-Host "==> Bundled bcryptprimitives.dll"
} else {
    Write-Warning "wine-system32/bcryptprimitives.dll not found — skipping (GE-Proton8-21 users will need it manually)"
}

$setupScript = Join-Path $repoRoot "installers/linux/setup-linux.sh"
if (Test-Path $setupScript) {
    Copy-Item -Path $setupScript -Destination (Join-Path $bundleDir "setup-linux.sh") -Force
    Write-Host "==> Bundled setup-linux.sh"
}

# 4c. Bundle the self-contained .NET runtime OMP needs. Without it OMP downloads
#     the runtime on first launch (a Microsoft redistributable), which fails
#     under Wine/Proton (Steam Deck). By default we download the exact same
#     runtime OMP would and extract it into the folder OMP looks for
#     (omp-lswtss-dotnet-runtime-8.0.2 in the game root), so every release is
#     self-contained. -DotnetRuntime overrides with a local copy (e.g. offline).
$rtName = "omp-lswtss-dotnet-runtime-8.0.2"
$rtDst = Join-Path $bundleDir $rtName
if ($DotnetRuntime) {
    if (-not (Test-Path $DotnetRuntime)) { throw "-DotnetRuntime path not found: $DotnetRuntime" }
    Write-Host "==> Bundling .NET runtime from $DotnetRuntime"
    Copy-Item -Path $DotnetRuntime -Destination $rtDst -Recurse -Force
} else {
    Write-Host "==> Downloading + bundling .NET runtime ($rtName)..."
    $rtZip = Join-Path $work "dotnet-runtime.zip"
    Invoke-WebRequest -Uri $DotnetRuntimeUrl -OutFile $rtZip
    Expand-Archive -Path $rtZip -DestinationPath $rtDst -Force
}

# 5. Zip the bundle's contents. This is a complete, standalone-installable
#    bundle (it includes c-api1 and everything else from upstream, plus the
#    components rebuilt above).
$outZip = if ([System.IO.Path]::IsPathRooted($OutZip)) { $OutZip } else { Join-Path $repoRoot $OutZip }
if (Test-Path $outZip) { Remove-Item $outZip -Force }
Compress-Archive -Path (Join-Path $bundleDir "*") -DestinationPath $outZip
Write-Host "==> Release bundle (complete, installable): $outZip"

# 6. Optionally publish.
if ($Publish) {
    if (-not $Tag) { throw "-Publish requires -Tag" }
    # -PublishRepo is mandatory so we never fall back to gh's default repo and
    # accidentally publish to the wrong place (e.g. upstream).
    if (-not $PublishRepo) { throw "-Publish requires -PublishRepo <owner>/<repo>" }
    # Title/notes must be non-empty: empty values make GitHub fall back to
    # rendering the tag's commit message as the release title/body.
    $relTitle = if ([string]::IsNullOrWhiteSpace($Title)) { $Tag } else { $Title }
    $relNotes = if ([string]::IsNullOrWhiteSpace($Notes)) { $Tag } else { $Notes }
    $ghArgs = @("release", "create", $Tag, $outZip, "--repo", $PublishRepo, "--title", $relTitle, "--notes", $relNotes)
    if ($Target) { $ghArgs += @("--target", $Target) }
    if ($Prerelease) { $ghArgs += "--prerelease" }
    gh @ghArgs
}
