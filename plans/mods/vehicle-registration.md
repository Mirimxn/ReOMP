# Vehicle registration

Provide a manifest action for adding vehicles to the game's roster. Start from [the v1 vehicle action](../../mod-sources/v1/v1/src/ModRegisterCustomVehicleAction.cs), [table processing](../../mod-sources/v1/v1/src/ProcessCustomVehicles.cs) and the workspace gunship packages.

## Implementation sequence

1. Use one working gunship package as a migration fixture. Record its template, roster table, prefab references and the gameplay context where the player can use it.
2. Separate vehicle table registration from asset delivery. Use [asset replacement](asset-replacement.md) for packaged resources. Verify table and template identifiers against each tested executable and apply declarations only after the target table is available.
3. Report missing templates, duplicate vehicle IDs and unresolved required assets with the declaring mod ID. Port the fixture to the new manifest action once its required fields are verified. Expose a lookup by roster table and vehicle ID so [vehicle spawning](vehicle-spawning.md) can resolve built-in and registered vehicle definitions through the same contract.

## Acceptance

The migrated fixture registers and can be selected and used in the same verified gameplay context as the baseline. Two registrations in the same table retain distinct IDs. Verify one invalid template and one absent required asset.
