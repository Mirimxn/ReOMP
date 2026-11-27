# Background Character Colours (`population-palette`)

The game paints background ("population") characters, such as the clone troopers walking around Coruscant, with colours from one shared file: `SYSTEMS/POPULATION/CHARACTERPALETTEDATA.PRJ`. Every character name there (for example `City_Clone_Couruscant`) has one or more palettes. Each palette has a colour and an on/off switch for each part: `Skin`, `Hands`, `Upper Body`, `Arms`, `Lower Body`, `Legs`, `Hips`, `Skirt attachment`, `Hat attachment` and `Hair attachment`. A switched-on part is painted with its colour over the character's own colours and textures.

This mod owns that file. Mods don't ship a copy of it, because each copy would undo every other mod's changes. Instead they declare only the parts they change, and the ReOMP Mod Manager combines them.

## Declaring a change

Depend on `population-palette` (1.0.0 or newer) and add one action per part:

```json
{
  "typeId": "population-palette/set",
  "payload": { "character": "City_Clone_Couruscant", "element": "Arms", "apply": false }
}
```

| Field | Meaning |
| --- | --- |
| `character` | The character name in the palette file (any letter case). |
| `element` | The part (any letter case). |
| `palette` | Optional. The palette's index for characters with several; without it, the change applies to all of them. |
| `apply` | Optional. `false` stops the game painting the part, so the character's own colours and textures show; `true` turns the paint on. |
| `colour` | Optional. The paint colour as `"#rrggbb"`. |

Each action needs `apply`, `colour` or both.

## How changes combine

- When it installs, the Mod Manager reads the game's own palette and writes every enabled mod's changes into `merged/CHARACTERPALETTEDATA.PRJ` in this mod's folder. `population-palette/serve` then registers that file with `asset-replacement`. With no changes there is no file, and the game uses its own palette.
- Changes to different parts, or the same value set twice, combine. Two mods setting the same part of the same character (the same palette, or one of them for all palettes) to different values block the change: the Mod Manager shows both mods and leaves the installed files alone.
- A mod that replaces the whole palette file through `asset-replacement/register` blocks any mod with `population-palette/set` changes. Dropping an edited `CHARACTERPALETTEDATA.PRJ` on the Mod Manager turns its differences from the game's file into a `palette-changes` mod.
- A change for a character or part the game's file doesn't have is a blocking error.
- Without the Mod Manager there is no combined file. The changes are logged as not applied, and the game keeps its own palette.

The Characters tab writes these actions for a background character: for each part you change it writes that part's colour from the tab (with `apply: true`) into the palette entry you choose. Far away the game draws background characters with the palette's colours only, so switching a part off isn't enough there.
