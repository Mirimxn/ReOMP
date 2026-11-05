# Vehicle fixtures

Manifests of the two working gunship packages used as the VEH migration fixture. Game-derived files (models, textures, shaders, prefabs, scenes, icons) are not committed. Take them from `laat-gunships-1.0.zip` and copy each package's `CHARS/` folder and `icon.png` beside its `mod.json`.

| Package | ID | Template | Table | Prefab | Preview base |
| --- | --- | --- | --- | --- | --- |
| `coruscant-gunship` | `Coruscant_Gunship` | `Wookie_Gunship` | `Vehicles` | `chars/vehicles/republic_gunship/coruscantgunship.prefab_baked` (own `.ghg`, `.nxg_textures`, `.pc_shaders` with unique IDs) | `Micro_ARC_170` |
| `republic-gunship` | `Republic_Gunship` | `Wookie_Gunship` | `Vehicles` | `chars/vehicles/republic_gunship/republic_gunship.prefab_baked` (vanilla) | `Micro_Acclamator` |

Baseline, verified on Windows Steam with the v1 module from this repository (4–5 Nov 2026): both rows appear in the vehicle roster unlocked and free, with their own name, icon and preview; switching between them and the Wookiee Gunship shows the right model each time; both spawn from the roster.

Not covered yet: Epic variant, and the 27 collapsed mesh parts of the Coruscant model (static restore pending).
