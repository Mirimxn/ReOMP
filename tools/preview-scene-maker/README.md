# Preview scene maker

Drop a vehicle `.ghg` or `.prefab_baked` on `MAKE-PREVIEW-SCENE.bat`. It writes `PREVIEW/<NAME>_PREVIEW.SCENE_BAKED` next to the model, pointing the Wookiee Gunship's cheap scene at that model (dependency list, model path, block sizes, model ID and a new header ID). Use the result as `previewScene` in a `v1/register-custom-vehicle-action`.

Put the vanilla Steam `CHARS/VEHICLES/WOOKIE_GUNSHIP/WOOKIE_GUNSHIP_CHEAP.SCENE_BAKED` (21964 bytes) next to the script first. It is game data and is not committed.

Not yet in this tool: fitting the `EntityExtentCalculator` box to the model and choosing visible render layers. Both were needed for the LAAT/c package (the Wookiee scene hides layers 1–5).
