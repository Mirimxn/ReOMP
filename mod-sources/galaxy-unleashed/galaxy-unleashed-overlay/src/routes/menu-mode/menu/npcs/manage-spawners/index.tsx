import {
  createFileRoute,
  useNavigate,
} from "@tanstack/react-router";
import {
  SettingsIcon,
  TrashIcon,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  destroyNpcSpawner,
  getCharacterFactionName,
  setIsNpcSpawnerEnabled,
  useCharactersInfo,
  useNpcSpawnersState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/menu-mode/menu/npcs/manage-spawners/")({
  component: RouteComponent,
  staticData: {
    menuPage: {
      name: "NPCs",
      prevPageFullPath: "/menu-mode/menu",
    },
  },
});

function RouteComponent() {
  const navigate = useNavigate({
    from: "/menu-mode/menu/npcs/manage-spawners" satisfies typeof Route.fullPath,
  });

  const charactersInfo = useCharactersInfo();

  const npcSpawnersState = useNpcSpawnersState();

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>Manage Spawners</CardTitle>
        <CardDescription>
          Configure or delete existing spawners.
        </CardDescription>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <div className="size-full overflow-y-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-12">#</TableHead>
                <TableHead>Character</TableHead>
                <TableHead className="w-24">Is Enabled</TableHead>
                <TableHead className="w-32">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {npcSpawnersState.map((npcSpawnerState, npcSpawnerStateIndex) => (
                <TableRow key={npcSpawnerState.id}>
                  <TableCell>{npcSpawnerStateIndex + 1}</TableCell>
                  <TableCell>
                    {
                      charactersInfo.find(characterInfo =>
                        characterInfo.id == npcSpawnerState.config.npcPreset.characterId,
                      )?.name
                    }
                    {
                      npcSpawnerState.config.npcPreset.characterOverrideFactionId == null
                        ? <></>
                        : ` (${getCharacterFactionName(npcSpawnerState.config.npcPreset.characterOverrideFactionId)})`
                    }
                  </TableCell>
                  <TableCell>
                    <Switch
                      checked={npcSpawnerState.isEnabled}
                      onCheckedChange={async (isNpcSpawnerEnabled) => {
                        await setIsNpcSpawnerEnabled({
                          npcSpawnerId: npcSpawnerState.id,
                          isNpcSpawnerEnabled,
                        });
                      }}
                    />
                  </TableCell>
                  <TableCell className="flex flex-row justify-start gap-2">
                    <Button
                      size="icon"
                      onClick={async () => {
                        await navigate({
                          to: `/menu-mode/menu/npcs/manage-spawners/${npcSpawnerState.id}`,
                        });
                      }}
                    >
                      <SettingsIcon />
                    </Button>
                    <Button
                      variant="destructive"
                      size="icon"
                      onClick={async () => {
                        await destroyNpcSpawner({
                          npcSpawnerId: npcSpawnerState.id,
                        });
                      }}
                    >
                      <TrashIcon />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}
