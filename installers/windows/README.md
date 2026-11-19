# Windows Mod Manager installer

Windows setup installs the ReOMP Mod Manager desktop app. [Installer implementation plan](../../plans/installer.md)

The setup executable is a Tauri NSIS installer for the current user, configured in [`apps/mod-manager/src-tauri/tauri.windows.conf.json`](../../apps/mod-manager/src-tauri/tauri.windows.conf.json). It bundles the offline WebView2 installer.

## Get a build

GitHub Actions builds it with [`.github/workflows/windows-installer.yml`](../../.github/workflows/windows-installer.yml):

- **Every push to `main` and every pull request:** open the run on the repository's **Actions** tab and download `ReOMP-<version>-windows-x64-setup` from **Artifacts**. GitHub delivers it zipped; the setup executable is inside.
- **Release:** set `version` in `apps/mod-manager/src-tauri/tauri.conf.json`, then push a matching tag such as `v0.1.0`. The setup executable is published on the **Releases** page as `ReOMP-<version>-windows-x64-setup.exe`.

To build locally instead, follow the commands in the [installer plan](../../plans/installer.md#windows).

## Bundled mods

The installer carries the OMP runtime, the core mods built from `mod-sources/`, and the default mods listed in [`bundled-mods.json`](../../bundled-mods.json). The default mod ZIPs are assets of the GitHub release named there; the workflow downloads them and checks their SHA-256s. The manager adds bundled mods to the library once per version and turns the default mods on, with the mods they need, the first time it adds them.

To change a default mod: upload the new ZIP to a new release on GitHub, then update the release name, file name and SHA-256 in `bundled-mods.json`.
