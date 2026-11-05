#!/usr/bin/env bash
set -eu

SOURCE_DIR="$(cd "$(dirname "$0")" && pwd)"

# Find game directory
GAME_EXE="$(find /home/deck /run/media -maxdepth 8 -name 'LEGOSTARWARSSKYWALKERSAGA_DX11.exe' 2>/dev/null | head -1)"
if [ -z "$GAME_EXE" ]; then
    echo "ERROR: LEGOSTARWARSSKYWALKERSAGA_DX11.exe not found. Install the game in Steam first." >&2
    exit 1
fi
GAME_DIR="$(dirname "$GAME_EXE")"
echo "Game dir : $GAME_DIR"

# Find or initialise Proton prefix
PREFIX_PARENT="$(find "$HOME/.local/share/Steam/steamapps/compatdata" /run/media -maxdepth 10 -name "920210" -type d 2>/dev/null | head -1)"
if [ -z "$PREFIX_PARENT" ] || [ ! -d "$PREFIX_PARENT/pfx/drive_c" ]; then
    notify-send "OMP Setup" "Launching game to initialise prefix — quit as soon as it starts." 2>/dev/null || true
    echo "Proton prefix not yet initialised. Launching game — quit as soon as it starts."
    DISPLAY=:0 xdg-open "steam://run/920210"
    until PREFIX_PARENT="$(find "$HOME/.local/share/Steam/steamapps/compatdata" /run/media -maxdepth 10 -name "920210" -type d 2>/dev/null | head -1)" \
          && [ -n "$PREFIX_PARENT" ] && [ -d "$PREFIX_PARENT/pfx/drive_c" ]; do
        sleep 3
    done
    notify-send "OMP Setup" "Prefix ready — quit the game to continue." 2>/dev/null || true
    echo "Prefix ready. Waiting for game to exit..."
    sleep 5
    until ! pgrep -f 'LEGOSTARWARSSKYWALKERSAGA_DX11' > /dev/null 2>&1; do
        sleep 3
    done
fi
PREFIX="$PREFIX_PARENT/pfx"
echo "Prefix   : $PREFIX"

# Copy mod files into game directory
echo "Copying mod files..."
for item in dinput8.dll omp-lswtss-driver.dll omp-lswtss-driver-config.json \
            omp-lswtss-runtime-engine-0.0.1 omp-lswtss-dotnet-runtime-8.0.2 mods; do
    [ -e "$SOURCE_DIR/$item" ] && cp -r "$SOURCE_DIR/$item" "$GAME_DIR/"
done

# bcryptprimitives.dll must go in the Wine prefix system32 — Wine 8.0 treats it as a
# KnownDLL so it never searches the game directory for it.
if [ -f "$SOURCE_DIR/bcryptprimitives.dll" ]; then
    cp "$SOURCE_DIR/bcryptprimitives.dll" "$PREFIX/drive_c/windows/system32/bcryptprimitives.dll"
    echo "Installed bcryptprimitives.dll to Wine prefix."
fi

# Copy .NET runtime into Wine prefix
cp -r "$GAME_DIR/omp-lswtss-dotnet-runtime-8.0.2/." "$PREFIX/drive_c/Program Files/dotnet/"
grep -q 'dotnet\\Setup' "$PREFIX/system.reg" || \
    printf '%s\n' '' '[Software\\dotnet\\Setup\\InstalledVersions\\x64] 1750291200' \
        '"installLocation"="C:\\Program Files\\dotnet\\"' >> "$PREFIX/system.reg"

# Write dinput8 DLL override into Wine registry (scoped to this game's exe)
# Equivalent to WINEDLLOVERRIDES="dinput8=n,b" but persistent without needing Steam launch options.
OVERRIDE_KEY='Software\\Wine\\AppDefaults\\LEGOSTARWARSSKYWALKERSAGA_DX11.exe\\DllOverrides'
if ! grep -q "$OVERRIDE_KEY" "$PREFIX/user.reg"; then
    UNIX_NOW=$(date +%s)
    WIN_FILETIME=$(printf '%x' $(( (UNIX_NOW + 11644473600) * 10000000 )))
    printf '\n[%s] %d\n#time=%s\n"dinput8"="native,builtin"\n' \
        "$OVERRIDE_KEY" "$UNIX_NOW" "$WIN_FILETIME" >> "$PREFIX/user.reg"
fi

notify-send "OMP Setup" "Done. Launch the game from Steam." 2>/dev/null || true
echo "Done. Launch the game from Steam."
read -rp "Press Enter to close." 2>/dev/null || true
