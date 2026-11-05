# V1

Registers custom game resources, characters and vehicles. Depend on `v1` and use `v1/register-custom-resource-action`, `v1/register-custom-character-action` or `v1/register-custom-vehicle-action` in the manifest. The payload contracts live in the matching `ModRegisterCustom*ActionPayload.cs` files under `v1/src/`.

Managed mods can also reference the shared `omp-lswtss-v1` assembly for APIs such as `V1.PrefabResource` and `V1.FetchCharactersInfo`.
