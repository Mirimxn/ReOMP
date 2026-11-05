# Vehicle spawning and control

Give feature mods an API to place a vehicle in the active world and use the game's native driver or pilot controls. Use [vehicle registration](vehicle-registration.md) to resolve vehicle definitions and [game access](game-access.md) for prefab and entity lifetime.

## Implementation sequence

1. Trace how the game creates, initialises and removes one built-in playable vehicle in a known ground or space context. Record the required world, vehicle components and seat initialisation. Use the existing vehicle definitions for table, ID and prefab lookup. Prove the native spawn sequence before exposing a spawn API. [Inherited vehicle fields](../../mod-sources/v1/v1/src/ModRegisterCustomVehicleActionPayload.cs)
2. Accept a roster table and vehicle ID plus a position and orientation. Resolve the definition, load its prefab through game access and run the verified spawn sequence on the game thread. Return a world-scoped handle on success. Reject unsupported vehicles or contexts before changing the world. Cancel pending work when its owner leaves or the world changes, and release partial entities and resources on failure.
3. Trace normal player entry and exit to identify the native seat and control handoff. The game bindings include `VehicleMountListener` as a tracing lead. Offer entry and exit for a specified player and spawned vehicle once the corresponding calls are verified. Reject an occupied seat, a player already controlling another vehicle, or an unsupported variant or context. Pass driving and flight input to the game through its normal control path. Feature mod shortcuts use [shared input](input.md).
4. Scope spawn removal to its owner. Reject removal while the vehicle has an occupant so the caller can request a safe exit and retry. On owner or world teardown, release pending work and clean up live vehicles through the verified game lifecycle. Avoid issuing native calls on stale handles.

## Acceptance

On Windows, spawn a known built-in vehicle, enter its seat, confirm normal controls, exit and remove it. Verify separate ground and space cases and a registered custom vehicle in its supported context. Exercise an occupied seat, conflicting control requests, an invalid vehicle ID and a scene change during prefab loading. Verify that rejected or cancelled operations leave the player and world in a usable state. Record which vehicles, contexts and game variants passed.
