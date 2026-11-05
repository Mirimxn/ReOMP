# Hooks

Provide a shared way for mods to intercept native game functions and invoke the original function. Start from [the managed hook wrapper](../../mod-sources/c-func-hook1/c-func-hook1/src/CFuncHook1.cs) and [the native implementation](../../mod-sources/c-func-hook1/c-func-hook1-native/src/lib.rs).

## Implementation sequence

1. Reproduce attachment, original-call forwarding, disable and teardown in a controlled native fixture. The managed wrapper currently leaves native destruction disabled with a crash comment. Identify when native code or managed delegates remain in use before defining which cleanup and reload operations the provider supports.
2. Expose registration for a verified native target and delegate signature. Retain the delegate and original-call trampoline until callbacks have drained. Return a handle that makes ownership and unregistration explicit. Keep per-executable address resolution in the bindings or owning adapter.
3. Prove how two consumers share one target before exposing it to unrelated mods. Install one native detour per verified target. Call observers in registration order and isolate callback errors at the native boundary. Allow one replacement owner per target and reject another registration before changing the active detour.
4. Migrate one game-update hook and one existing consumer of the same target. Document calling convention, callback thread, re-entry and lifetime.

## Acceptance

The fixture verifies original-call counts with two observers, removal of one observer, and cleanup after callbacks finish. Confirm game startup and shutdown for each tested variant. Verify live reload separately if it becomes part of the contract.
