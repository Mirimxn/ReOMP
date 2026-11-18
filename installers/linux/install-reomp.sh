#!/usr/bin/env bash
# Installs the ReOMP Mod Manager for the current user, including on SteamOS
# in Desktop Mode. Run it from the extracted release ZIP:
#
#   bash install-reomp.sh              install, or upgrade an existing install
#   bash install-reomp.sh --uninstall  remove the app and its menu entry
#
# Only the app files under <data>/reomp/app/ and the menu entry are written.
# The manager's settings and mod library are never touched.
# See plans/installer.md#install-script.

set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
data="${XDG_DATA_HOME:-$HOME/.local/share}"
app="$data/reomp/app"
current="$app/current"
apps_dir="$data/applications"
desktop="$apps_dir/com.reomp.manager.desktop"

stage=""
link_tmp=""
launcher_tmp=""

cleanup() {
    [[ -n "$stage" ]] && rm -rf "$stage"
    [[ -n "$link_tmp" ]] && rm -f "$link_tmp"
    [[ -n "$launcher_tmp" ]] && rm -f "$launcher_tmp"
    return 0
}
trap cleanup EXIT

fail() {
    echo "ReOMP setup failed: $*" >&2
    exit 1
}

# Quotes a path for a desktop entry's Exec key: escape the characters the
# spec reserves inside quotes, then the string-level backslash escape, and
# double % so it isn't read as a field code.
# https://specifications.freedesktop.org/desktop-entry/1.2/exec-variables.html
exec_quote() {
    local s="$1"
    s="${s//\\/\\\\}"
    s="${s//\"/\\\"}"
    s="${s//\`/\\\`}"
    s="${s//\$/\\\$}"
    s="${s//\\/\\\\}"
    s="${s//%/%%}"
    printf '"%s"' "$s"
}

# A desktop entry string value only escapes backslashes.
string_value() {
    printf '%s' "${1//\\/\\\\}"
}

if [[ "${1:-}" == "--uninstall" ]]; then
    rm -f "$desktop"
    rm -rf "$app"
    rmdir "$data/reomp" 2>/dev/null || true
    command -v update-desktop-database >/dev/null && update-desktop-database "$apps_dir" 2>/dev/null || true
    echo "ReOMP Mod Manager was removed. Your mod library and settings were kept."
    exit 0
fi
[[ $# -eq 0 ]] || fail "unknown option '$1'. Run without options to install, or with --uninstall to remove."

image="$here/ReOMP.AppImage"
[[ -f "$image" ]] || fail "ReOMP.AppImage must be in the same folder as this script."

mkdir -p "$app" "$apps_dir" || fail "couldn't create $app"

# 1. Unpack the AppImage beside the final location, so no FUSE is needed.
stage="$(mktemp -d "$app/.stage.XXXXXX")" || fail "couldn't create a staging folder in $app"
cp "$image" "$stage/ReOMP.AppImage" || fail "couldn't copy ReOMP.AppImage"
chmod +x "$stage/ReOMP.AppImage"
(cd "$stage" && ./ReOMP.AppImage --appimage-extract >/dev/null) || fail "couldn't unpack ReOMP.AppImage"
[[ -x "$stage/squashfs-root/AppRun" ]] || fail "the unpacked app has no AppRun"

release="$app/release-$(date +%Y%m%d%H%M%S)-$$"
mv "$stage/squashfs-root" "$release" || fail "couldn't move the unpacked app into place"

# 2. Prepare the menu entry.
launcher_tmp="$(mktemp "$apps_dir/.com.reomp.manager.XXXXXX")" || fail "couldn't write in $apps_dir"
{
    echo "[Desktop Entry]"
    echo "Type=Application"
    echo "Name=ReOMP Mod Manager"
    echo "Comment=Install OMP and manage mods for LEGO Star Wars: The Skywalker Saga"
    echo "Exec=$(exec_quote "$current/AppRun")"
    [[ -e "$release/.DirIcon" ]] && echo "Icon=$(string_value "$current/.DirIcon")"
    echo "Terminal=false"
    echo "Categories=Game;Utility;"
} > "$launcher_tmp" || fail "couldn't write the menu entry"
chmod 644 "$launcher_tmp"

# 3. Switch to the new release, then publish the menu entry.
previous="$(readlink "$current" 2>/dev/null || true)"
link_tmp="$app/.current.$$"
ln -sfn "$release" "$link_tmp" || fail "couldn't link the new version"
if ! mv -Tf "$link_tmp" "$current"; then
    rm -rf "$release"
    fail "couldn't switch to the new version"
fi
link_tmp=""
if ! mv -f "$launcher_tmp" "$desktop"; then
    if [[ -n "$previous" ]]; then
        ln -sfn "$previous" "$app/.restore.$$" && mv -Tf "$app/.restore.$$" "$current"
    else
        rm -f "$current"
    fi
    rm -rf "$release"
    fail "couldn't add the menu entry; the previous version was kept"
fi
launcher_tmp=""

# 4. Remove older releases.
for old in "$app"/release-*; do
    [[ "$old" == "$release" ]] || rm -rf "$old"
done
command -v update-desktop-database >/dev/null && update-desktop-database "$apps_dir" 2>/dev/null || true

echo "ReOMP Mod Manager is installed. Open it from your applications menu."
