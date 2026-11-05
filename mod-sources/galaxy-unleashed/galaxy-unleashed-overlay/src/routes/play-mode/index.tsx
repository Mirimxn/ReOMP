import { createFileRoute } from "@tanstack/react-router";
import { useEffect } from "react";
import { toast } from "sonner";

import { TypographyLarge } from "@/components/ui/typography-large";
import {
  switchToMenuMode,
  useCharactersInfo,
  useClosestNpcSpawnerInPlayerEntityRangeId,
  useNpcSpawnersState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/play-mode/")({
  component: RouteComponent,
});

function RouteComponent() {
  const closestNpcSpawnerInPlayerEntityRangeId = useClosestNpcSpawnerInPlayerEntityRangeId();

  const npcSpawnersState = useNpcSpawnersState();

  const charactersInfo = useCharactersInfo();

  useEffect(
    () => {
      toast(
        "Galaxy Unleashed",
        {
          id: "play-mode-open-menu-toast",
          description: "Click F1 to open the menu.",
          position: "top-right",
        },
      );

      return () => {
        toast.getHistory()
          .map(x => x.id)
          .filter(toastId => typeof toastId === `string` && toastId.startsWith(`play-mode-`))
          .forEach(toast.dismiss);
      };
    },
    [],
  );

  useEffect(
    () => {
      if (closestNpcSpawnerInPlayerEntityRangeId !== null) {
        toast(
          "Manage NPC Spawner",
          {
            id: "play-mode-manage-npc-spawner-toast",
            description: "Click E to open Manage Spawner Menu.",
            position: "top-right",
            duration: Number.MAX_SAFE_INTEGER,
          },
        );

        return () => {
          toast.dismiss("play-mode-manage-npc-spawner-toast");
        };
      }
    },
    [
      closestNpcSpawnerInPlayerEntityRangeId,
    ],
  );

  const closestNpcSpawnerInPlayerEntityRangeState = closestNpcSpawnerInPlayerEntityRangeId === null
    ? undefined
    : npcSpawnersState.find(npcSpawnerState => npcSpawnerState.id === closestNpcSpawnerInPlayerEntityRangeId);

  const closestNpcSpawnerInPlayerEntityRangeStateNpcPresetCharacterInfo = closestNpcSpawnerInPlayerEntityRangeState === undefined
    ? undefined
    : charactersInfo.find(characterInfo => characterInfo.id === closestNpcSpawnerInPlayerEntityRangeState.config.npcPreset.characterId);

  return (
    <div
      className="h-screen w-screen"
      onClick={async () => {
        await switchToMenuMode({
          menuModeConfig: {
            navigateParams: {
              to: `/menu-mode/menu`,
              search: null,
            },
          },
        });
      }}
    >
      {
        closestNpcSpawnerInPlayerEntityRangeState !== undefined && (
          <div
            className="absolute left-0 top-0 flex h-screen w-screen items-center justify-center"
          >
            <TypographyLarge className="drop-shadow-[0_1.2px_1.2px_rgba(0,0,0,0.8)]">
              Click F1 to Manage NPC Spawner (
              {closestNpcSpawnerInPlayerEntityRangeStateNpcPresetCharacterInfo?.name}
              )
            </TypographyLarge>
          </div>
        )
      }
    </div>
  );
}
