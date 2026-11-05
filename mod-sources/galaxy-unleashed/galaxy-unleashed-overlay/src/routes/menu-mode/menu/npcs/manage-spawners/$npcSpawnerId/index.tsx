import { createFileRoute } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";
import { useCallback } from "react";

import { Form } from "@/components/form";
import { FormNpcSpawnerConfigFields } from "@/components/form-npc-spawner-config-fields";
import { FormSwitchField } from "@/components/form-switch-field";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  NpcSpawnerConfig,
  setIsNpcSpawnerEnabled,
  setNpcSpawnerConfig,
  useNpcSpawnersState,
} from "@/lib/runtime-api";

export const Route = createFileRoute(
  "/menu-mode/menu/npcs/manage-spawners/$npcSpawnerId/",
)({
  component: RouteComponent,
  staticData: {
    menuPage: {
      name: "NPCs",
      prevPageFullPath: "/menu-mode/menu/npcs",
    },
  },
});

function RouteComponent() {
  const navigate = Route.useNavigate();

  const { npcSpawnerId } = Route.useParams();

  const npcSpawnersState = useNpcSpawnersState();

  const npcSpawnerState = npcSpawnersState.find(npcSpawnerState => npcSpawnerState.id === npcSpawnerId);

  const onNpcSpawnerConfigChange = useCallback(
    async (npcSpawnerConfig: NpcSpawnerConfig) => {
      await setNpcSpawnerConfig({
        npcSpawnerId,
        npcSpawnerConfig,
      });
    },
    [
      npcSpawnerId,
    ],
  );

  const onNpcSpawnerIsEnabledChange = useCallback(
    async (isNpcSpawnerEnabled: boolean) => {
      await setIsNpcSpawnerEnabled({
        npcSpawnerId,
        isNpcSpawnerEnabled,
      });
    },
    [
      npcSpawnerId,
    ],
  );

  if (npcSpawnerState === undefined) {
    return null;
  }

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <div className="flex flex-row items-center gap-2">
          <Button
            size="icon"
            variant="ghost"
            onClick={() => navigate({ to: "/menu-mode/menu/npcs/manage-spawners" })}
          >
            <ArrowLeftIcon />
          </Button>
          <CardTitle>Manage Spawner</CardTitle>
        </div>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <div className="size-full overflow-y-auto">
          <Form>
            <FormSwitchField
              label="Is Enabled"
              description="When spawner is enabled, it spawns NPCs."
              checked={npcSpawnerState.isEnabled}
              onCheckedChange={onNpcSpawnerIsEnabledChange}
            />
            <FormNpcSpawnerConfigFields
              npcSpawnerInitialConfig={npcSpawnerState.config}
              onNpcSpawnerConfigChange={onNpcSpawnerConfigChange}
            />
          </Form>
        </div>
      </CardContent>
    </Card>
  );
}
