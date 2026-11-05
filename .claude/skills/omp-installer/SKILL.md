---
name: omp-installer
description: Plan, build or change the platform installers that distribute and set up the ReOMP Mod Manager.
---

# Work on a platform installer

Use `installers/windows/` for Windows setup and `installers/linux/` for Linux setup. Package the built ReOMP Mod Manager as the installer payload. The manager owns installing OMP and mods into the game.

Follow `plans/installer.md` for the payload, platform build commands and acceptance checks. The manager owns its configuration and archive storage.

`installers/linux/setup-linux.sh` is the inherited direct-to-game test script. Use it as reference for the existing Steam Deck test bundle. Platform setup for the manager belongs in its own installer workflow.

Check that the installed Mod Manager launches using the acceptance steps in the plan. Keep installer writes scoped to the desktop app and its launcher entries.
