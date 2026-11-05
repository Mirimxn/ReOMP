# ReOMP

ReOMP is a modding platform for **LEGO Star Wars: The Skywalker Saga**, maintained by **mirimon**.

## Custom vehicles and characters

This copy adds tools for making your own roster mods:

- **Custom vehicles:** a mod can add a vehicle to the in-game vehicle roster with its own name, description, icon and spinning preview, unlocked and free. See [`mod-sources/v1/mod.md`](mod-sources/v1/mod.md).
- **Characters (early):** the same action can add rows to the character roster (`"tableName": "Characters"`). Icons for characters are not done yet.
- **Preview scene maker:** drag a ship model onto [`tools/preview-scene-maker`](tools/preview-scene-maker/README.md) to make its roster preview.
- **Examples:** [`fixtures/vehicles/`](fixtures/vehicles/README.md) shows two working gunship mods (the manifests only; game files are never stored here).

## Installation

Run the ReOMP installer to set up the **ReOMP Mod Manager**. Open the manager to install OMP into your game and install mods through its interface.

The installer and mod manager are in development.

## Make your own mod

Open an empty workspace in your coding agent and point it at the setup guide. [Agentic setup guide](Agentic-Setup.md)

## Support

For support, include the build you installed, your platform and the steps that reproduce the issue. [TTGames LEGO Modding Discord server](https://discord.gg/9gYXPka)
