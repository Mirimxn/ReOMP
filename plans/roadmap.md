# ReOMP implementation roadmap

This is the starting point for working through the ReOMP plans. The table tracks what can start, what it depends on and what evidence closes each item. Follow the linked plans for implementation and acceptance details.

## Help the developer pick the next task

1. When asked what work remains, what to do next, or to continue the plan, read this roadmap and the latest progress entries. Check the repository for evidence that changes the status of the relevant items. Summarise what is finished, what is underway and what is blocked in plain language.
2. Offer up to three concrete choices that can be worked on now. Name the capability or deliverable the developer will get, and recommend one. Handle setup, source research, test fixtures and verification within the chosen task. Offer to resume unfinished work when it is ready to continue. If an item is blocked, say what is holding it up and offer another ready deliverable. Ask which option the developer wants before starting implementation, unless they already chose a task in their request.
3. After a choice, load the relevant repository skill and the linked plan. Use the plan to make implementation decisions. Ask the developer about product choices that affect the requested result. Investigate technical unknowns using the code and the plan's verification steps.
4. Before handing back, update the status and add a progress entry with the result, verification evidence and any remaining blocker. Present the next available choices when the developer asks to continue.

Examples of choices are **Let players import mod ZIPs**, **Let players browse game assets**, and **Build the Windows installer**. Use the current status to pick options.

## Work and dependencies

An item is ready when its prerequisites are done. The prerequisites apply to the named work item. Cross-references in the individual plans can also point to later integration work. **Pending** means the item has no recorded implementation progress. Use **In progress**, **Blocked** or **Done** as work proceeds. Record a blocker precisely enough for the next agent to pick it up.

| ID | Work | Prerequisites | Evidence for completion | Status |
| --- | --- | --- | --- | --- |
| HOOK | [Shared hooks](mods/hooks.md) | — | Verify native hook lifecycle, original calls and two consumers sharing a target. | Pending |
| GAME | [Game access](mods/game-access.md) | HOOK | Verify world and entity lifetime, game-thread execution and the reusable prefab path. | Pending |
| LIB | [Manager mod library](mod-manager.md#packages-and-versions) | — | Import and watch ZIPs, resolve versions and block invalid dependency sets using fixtures. | Pending |
| DEPLOY | [Manager deployment on Windows](mod-manager.md#game-targets-and-omp-payload) | LIB | Verify target toggles, existing OMP replacement, rollback, deferral and Retry. | Pending |
| PROTON | [Manager deployment adapter for Proton](mod-manager.md#game-targets-and-omp-payload) | DEPLOY | Implement prefix discovery, setup and removal. Record the Linux build result. | Pending |
| BROWSE | [Browse and extract game assets](mod-manager.md#game-assets-and-replacement-mods) | — | Verify the index and supported extraction paths against fixtures and Windows archives. Use fixtures to start before the manager's target setup is ready. | Pending |
| ASSET | [Asset replacement provider](mods/asset-replacement.md) | HOOK, DEPLOY | Verify replacement and addition using the manager's tracked deployment, including conflict rejection and restoration. | Pending |
| DRAFT | [Create replacement mods in the manager](mod-manager.md#game-assets-and-replacement-mods) | BROWSE, ASSET | Create, export, import, enable and remove a replacement ZIP through the UI. Confirm the original asset returns. | Pending |
| INPUT | [Shared input service](mods/input.md) | HOOK | Verify action declarations, keyboard dispatch and the saved binding contract. | Pending |
| UI | [In-game UI and input screen](mods/ui.md) | INPUT | Verify the shared render host, two mod panels and in-game rebinding. | Pending |
| EDIT | [Desktop input editor and shared bindings](mods/input.md#acceptance) | INPUT, DEPLOY | Build the desktop editor. Verify shared bindings and concurrent edits once the in-game screen is available. | Pending |
| CHAR | [Character registration](mods/character-registration.md) | GAME, ASSET | Verify a selectable custom character and the roster lookup. | Pending |
| VEH | [Vehicle registration](mods/vehicle-registration.md) | GAME, ASSET | Verify the migrated gunship fixture in its supported context and the vehicle lookup. | Pending |
| SPAWN | [NPC spawning](mods/npc-spawning.md) | CHAR | Verify spawn and removal, failure cleanup and scene changes. | Pending |
| ORDERS | [NPC orders](mods/npc-orders.md) | GAME | Verify navigation, follow and targeted combat with two consumers and conflict handling. | Pending |
| STATS | [NPC stats](mods/npc-stats.md) | GAME | Verify live health edits, game updates and two callers. | Pending |
| DRIVE | [Vehicle spawning and control](mods/vehicle-spawning.md) | VEH | Verify native spawn, seat entry and exit, and removal for the planned vehicle fixtures. | Pending |
| ANIM | [Animations](mods/animations.md) | GAME | Verify known clips for a player and an NPC, including invalid targets and world changes. | Pending |
| SCENE | [Gameplay scene loading](mods/scenes.md) | GAME | Verify built-in transitions and failure paths. Test the same path with a packaged scene when its fixture and asset provider are available. | Pending |
| SAVE | [Save-scoped mod storage](mods/storage.md) | GAME | Verify SQLite loads from the mod package and saves are isolated by storefront, account and slot. Check migrations and interruption recovery. | Pending |
| WIN | [Build the Windows installer](installer.md#windows) | — | Build the setup executable and verify installation, upgrade and uninstall using the available manager build. | Pending |
| LINUX | [Build the Linux installer](installer.md#linux) | — | Build the AppImage and the install script and verify the ZIP contents. | Pending |
| PAYLOAD | [Assemble the replacement release](mod-migration.md#migration-order) | DRAFT, UI, EDIT, SPAWN, ORDERS, STATS, DRIVE, ANIM, SCENE, SAVE | Build the release runtime and provider ZIPs from clean staging and verify the migrated consumer fixtures. Stage the same payload for both installers. | Pending |
| WINREL | [Windows release verification](installer.md#verification-and-handoff) | PAYLOAD, WIN | Package the finished manager. Verify installation, bundled ZIPs and the in-game deployment flow on Windows. | Pending |
| LINREL | [Linux release build](installer.md#verification-and-handoff) | PAYLOAD, PROTON, LINUX | Package the finished manager and verify the AppImage and install-script ZIP contents. | Pending |
| LEGACY | [Archive inherited sources and rebuild](mod-migration.md#migration-order) | WINREL, LINREL | Move retired sources to `legacy/mod-sources/`, then rebuild cleanly and check the intended packages and Windows launch. | Pending |

Use the Windows checks in each plan. Record Steam and Epic results separately where applicable, along with any variant or fixture the plan permits deferring. Record Linux build results here. Linux runtime checks are for later user testing. If an acceptance check required for the current scope fails or cannot run, record the blocker and keep the item open.

## Progress

Add an entry for each completed work session. Include the item ID, what changed, the command or in-game check and its result, and any remaining dependency or blocker. Keep the table status current so the next session can resume from evidence.

| Date | Item | Result and evidence | Remaining work |
| --- | --- | --- | --- |

## Completion

When every item is done, record the final Windows verification and Linux build results here, including the location of the release outputs. Remove the **Active implementation roadmap** section from `AGENTS.md`. Keep this roadmap and the individual plans as the implementation record.
