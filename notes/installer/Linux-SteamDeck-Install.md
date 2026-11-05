# Steam Deck test bundle setup

These maintainer notes document the inherited test bundle that installs OMP directly. The player workflow uses the Mod Manager to install OMP and mods.

The original setup was tested on Steam Deck with GE-Proton8-21 and the Steam version of LEGO Star Wars: The Skywalker Saga (App ID 920210).

---

## Requirements

- **GE-Proton8-21** — standard Steam Proton is not supported. The mod hooks into DXVK's internal swap chain; the required layout is only present in DXVK v2.3.x as shipped by GE-Proton8-21. Other versions will crash or show a white screen.

---

## 1. Install GE-Proton8-21

1. Install **ProtonUp-Qt** from the Discover store.
2. Open ProtonUp-Qt → **Add version** → select **GE-Proton** → **8-21** → **Install**.
3. Restart Steam.
4. In Steam, right-click **LEGO Star Wars: The Skywalker Saga** → **Properties** → **Compatibility** → tick **Force the use of a specific Steam Play compatibility tool** → select **GE-Proton8-21**.

---

## 2. Run the setup script

Switch to Desktop Mode, extract the mod zip, and run `setup-linux.sh`.

The script will:
- Copy the mod files into the game directory
- Launch the game once if needed to initialise the Proton prefix (quit as soon as it starts)
- Copy the bundled .NET runtime into the Wine prefix
- Register the dinput8 DLL override so the mod loads

---

## 3. Launch the game

Launch normally from Steam.
