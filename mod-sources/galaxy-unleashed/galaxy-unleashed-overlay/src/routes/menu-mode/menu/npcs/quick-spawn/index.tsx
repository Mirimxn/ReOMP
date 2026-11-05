import { createFileRoute } from "@tanstack/react-router";
import { LogInIcon } from "lucide-react";
import {
  useMemo,
  useState,
} from "react";
import * as zustand from "zustand";

import { Form } from "@/components/form";
import { FormNpcPresetConfigField } from "@/components/form-npc-preset-config-field";
import { FormSwitchField } from "@/components/form-switch-field";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  NpcPresetConfig,
  QuickSpawnNpcsModeConfig,
  switchToQuickSpawnNpcsMode,
} from "@/lib/runtime-api";

interface RouteStore {
  npcPresetConfig: NpcPresetConfig | undefined;
  setNpcPresetConfig: (npcPresetConfig: NpcPresetConfig | undefined) => void;
}

const useRouteStore = zustand.create<RouteStore>(
  setRouteStore => ({
    npcPresetConfig: undefined,
    setNpcPresetConfig: (npcPresetConfig) => {
      setRouteStore({
        npcPresetConfig,
      });
    },
  }),
);

export const Route = createFileRoute("/menu-mode/menu/npcs/quick-spawn/")({
  component: RouteComponent,
  staticData: {
    menuPage: {
      name: "NPCs",
      prevPageFullPath: "/menu-mode/menu",
    },
  },
});

function RouteComponent() {
  const {
    npcPresetConfig,
    setNpcPresetConfig,
  } = useRouteStore();

  const [isNpcBattleParticipant, setIsNpcBattleParticipant] = useState(true);

  const quickSpawnNpcsModeConfig = useMemo<QuickSpawnNpcsModeConfig | undefined>(
    () => {
      if (npcPresetConfig === undefined) {
        return undefined;
      }

      return {
        npcPreset: npcPresetConfig,
        isNpcBattleParticipant,
      };
    },
    [
      npcPresetConfig,
      isNpcBattleParticipant,
    ],
  );

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>Quick Spawn</CardTitle>
        <CardDescription>Configure available options and click button on the bottom to enter &quot;Quick Spawn Mode&quot;.</CardDescription>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <div className="size-full overflow-y-auto">
          <Form>
            <FormNpcPresetConfigField
              label="NPC Preset"
              description="Select which character to spawn."
              npcPresetInitialConfig={npcPresetConfig}
              onNpcPresetConfigChange={setNpcPresetConfig}
            />
            <FormSwitchField
              label="Should NPC Participate In Battle"
              description="Decide whether the NPC should participate in battle."
              checked={isNpcBattleParticipant}
              onCheckedChange={setIsNpcBattleParticipant}
            />
          </Form>
        </div>
      </CardContent>
      <CardFooter>
        <Button
          disabled={quickSpawnNpcsModeConfig === undefined}
          className="w-full"
          onClick={async () => {
            if (quickSpawnNpcsModeConfig === undefined) {
              return;
            }

            await switchToQuickSpawnNpcsMode({
              quickSpawnNpcsModeConfig,
            });
          }}
        >
          <LogInIcon />
          Enter Quick Spawn Mode
        </Button>
      </CardFooter>
    </Card>
  );
}
