# Windows Mod Manager installer

Windows setup installs the ReOMP Mod Manager desktop app. [Installer implementation plan](../../plans/installer.md)

The setup executable is a Tauri NSIS installer for the current user, configured in [`apps/mod-manager/src-tauri/tauri.windows.conf.json`](../../apps/mod-manager/src-tauri/tauri.windows.conf.json). It bundles the offline WebView2 installer.

## Get a build

GitHub Actions builds it with [`.github/workflows/windows-installer.yml`](../../.github/workflows/windows-installer.yml):

- **Every push to `main` and every pull request:** open the run on the repository's **Actions** tab and download `ReOMP-<version>-windows-x64-setup` from **Artifacts**. GitHub delivers it zipped; the setup executable is inside.
- **Release:** set `version` in `apps/mod-manager/src-tauri/tauri.conf.json`, then push a matching tag such as `v0.1.0`. The setup executable is published on the **Releases** page as `ReOMP-<version>-windows-x64-setup.exe`.

To build locally instead, follow the commands in the [installer plan](../../plans/installer.md#windows).
