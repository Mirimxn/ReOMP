import { createFileRoute } from "@tanstack/react-router";

import {
  Sheet,
  SheetContent,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableRow,
} from "@/components/ui/table";
import {
  getCharacterFactionName,
  ModeKind,
  switchToMenuMode,
  useCharactersInfo,
  useModeState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/create-npc-spawners-mode/")({
  component: RouteComponent,
});

function RouteComponent() {
  const modeState = useModeState();

  const charactersInfo = useCharactersInfo();

  if (modeState.kind !== ModeKind.CreateNpcSpawnersMode) {
    return null;
  }

  const {
    maxNpcsCount,
    npcSpawningIntervalSeconds,
    npcPreset,
    areNpcsBattleParticipants,
  } = modeState.config.npcSpawner;

  const npcCharacterInfo = charactersInfo.find(
    characterInfo => characterInfo.id === npcPreset.characterId,
  );

  return (
    <Sheet
      modal={false}
      open={true}
      onOpenChange={async (value) => {
        if (!value) {
          await switchToMenuMode({
            menuModeConfig: {
              navigateParams: {
                to: `/menu-mode/menu/npcs/create-spawners`,
                search: null,
              },
            },
          });
        }
      }}
    >
      <SheetContent closeButtonHidden side="right">
        <SheetHeader>
          <SheetTitle>Create NPC Spawners</SheetTitle>
        </SheetHeader>
        <div className="flex size-full flex-col justify-between">
          <Table>
            <TableBody>
              <TableRow>
                <TableCell>Character</TableCell>
                <TableCell className="font-bold">
                  {npcCharacterInfo?.name}
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Character Override Faction</TableCell>
                <TableCell className="font-bold">
                  {npcPreset.characterOverrideFactionId === null
                    ? `None`
                    : getCharacterFactionName(
                      npcPreset.characterOverrideFactionId,
                    )}
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Maximum Number of NPCs</TableCell>
                <TableCell className="font-bold">
                  {maxNpcsCount}
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>NPC Spawning Interval</TableCell>
                <TableCell className="font-bold">
                  {npcSpawningIntervalSeconds}
                  {" "}
                  seconds
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Should NPCs Participate In Battle</TableCell>
                <TableCell className="font-bold">
                  {areNpcsBattleParticipants ? `Yes` : `No`}
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
          <Table>
            <TableBody>
              <TableRow>
                <TableCell className="font-bold">F1</TableCell>
                <TableCell>Back to Menu</TableCell>
              </TableRow>
              <TableRow>
                <TableCell className="font-bold">F2</TableCell>
                <TableCell>Create</TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </div>
        <SheetFooter></SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
