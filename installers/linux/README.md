# Linux Mod Manager installer

The Linux release is `ReOMP-<version>-linux-x64.zip`, holding `ReOMP.AppImage` and `install-reomp.sh`. [Installer plan](../../plans/installer.md#linux)

## Install (also SteamOS Desktop Mode)

Extract the ZIP, open a terminal in that folder and run:

```bash
bash install-reomp.sh
```

ReOMP Mod Manager then appears in the applications menu. Run the same command with a newer ZIP to upgrade. `bash install-reomp.sh --uninstall` removes the app and its menu entry and keeps the mod library and settings.

## Build

GitHub Actions builds the ZIP with [`.github/workflows/linux-installer.yml`](../../.github/workflows/linux-installer.yml): the [`Dockerfile`](Dockerfile) builds the AppImage on Ubuntu 22.04, then the workflow test-installs, upgrades and uninstalls it in a temporary data folder. Download the ZIP from the run's **Artifacts**; a `v<version>` tag also publishes it on **Releases**.

Installing OMP into a Proton game from Linux is the PROTON roadmap item. Until then the Linux app manages the mod library but can't install into the game. `setup-linux.sh` is the inherited direct-to-game script for testing the OMP payload on a Steam Deck. [Steam Deck test notes](../../notes/installer/Linux-SteamDeck-Install.md)
