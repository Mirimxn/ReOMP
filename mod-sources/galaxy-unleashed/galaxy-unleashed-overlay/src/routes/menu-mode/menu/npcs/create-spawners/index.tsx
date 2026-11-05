import { createFileRoute } from "@tanstack/react-router";
import { LogInIcon } from "lucide-react";
import { useMemo } from "react";
import * as zustand from "zustand";

import { Form } from "@/components/form";
import { FormNpcSpawnerConfigFields } from "@/components/form-npc-spawner-config-fields";
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
  CreateNpcSpawnersModeConfig,
  NpcSpawnerConfig,
  switchToCreateNpcSpawnersMode,
} from "@/lib/runtime-api";

interface RouteStore {
  npcSpawnerConfig: NpcSpawnerConfig | undefined;
  setNpcSpawnerConfig: (npcSpawnerConfig: NpcSpawnerConfig | undefined) => void;
}

const useRouteStore = zustand.create<RouteStore>(
  setRouteStore => ({
    npcSpawnerConfig: undefined,
    setNpcSpawnerConfig: (npcSpawnerConfig) => {
      setRouteStore({
        npcSpawnerConfig,
      });
    },
  }),
);

export const Route = createFileRoute("/menu-mode/menu/npcs/create-spawners/")({
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
    npcSpawnerConfig,
    setNpcSpawnerConfig,
  } = useRouteStore();

  const createNpcSpawnersModeConfig = useMemo<
    CreateNpcSpawnersModeConfig | undefined
  >(
    () => {
      if (npcSpawnerConfig === undefined) {
        return undefined;
      }

      return {
        npcSpawner: npcSpawnerConfig,
      };
    },
    [
      npcSpawnerConfig,
    ],
  );

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>Create Spawners</CardTitle>
        <CardDescription>Configure available options and click button on the bottom to enter &quot;Create Spawners Mode&quot;.</CardDescription>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <div className="size-full overflow-y-auto">
          <Form>
            <FormNpcSpawnerConfigFields
              npcSpawnerInitialConfig={npcSpawnerConfig}
              onNpcSpawnerConfigChange={setNpcSpawnerConfig}
            />
          </Form>
        </div>
      </CardContent>
      <CardFooter>
        <Button
          disabled={createNpcSpawnersModeConfig === undefined}
          className="w-full"
          onClick={async () => {
            if (createNpcSpawnersModeConfig === undefined) {
              return;
            }

            await switchToCreateNpcSpawnersMode({
              createNpcSpawnersModeConfig,
            });
          }}
        >
          <LogInIcon />
          Enter Create Spawners Mode
        </Button>
      </CardFooter>
    </Card>
  );
}
