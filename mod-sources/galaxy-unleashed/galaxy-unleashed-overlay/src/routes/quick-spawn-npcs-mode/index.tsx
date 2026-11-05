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

export const Route = createFileRoute("/quick-spawn-npcs-mode/")({
  component: RouteComponent,
});

function RouteComponent() {
  const modeState = useModeState();

  const charactersInfo = useCharactersInfo();

  if (modeState.kind !== ModeKind.QuickSpawnNpcsMode) {
    return null;
  }

  const {
    npcPreset,
    isNpcBattleParticipant,
  } = modeState.config;

  const npcCharacterInfo = charactersInfo.find(characterInfo => characterInfo.id === npcPreset.characterId);

  return (
    <Sheet
      modal={false}
      open={true}
      onOpenChange={async (value) => {
        if (!value) {
          await switchToMenuMode({
            menuModeConfig: {
              navigateParams: {
                to: `/menu-mode/menu/npcs/quick-spawn`,
                search: null,
              },
            },
          });
        }
      }}
    >
      <SheetContent closeButtonHidden side="right">
        <SheetHeader>
          <SheetTitle>Quick Spawn NPCs</SheetTitle>
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
                  {
                    npcPreset.characterOverrideFactionId === null
                      ? `None`
                      : getCharacterFactionName(npcPreset.characterOverrideFactionId)
                  }
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell>Should Participate In Battle</TableCell>
                <TableCell className="font-bold">
                  {
                    isNpcBattleParticipant ? `Yes` : `No`
                  }
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
          <Table>
            <TableBody>
              <TableRow>
                <TableCell className="font-bold">F1</TableCell>
                <TableCell>
                  Back to Menu
                </TableCell>
              </TableRow>
              <TableRow>
                <TableCell className="font-bold">F2</TableCell>
                <TableCell>
                  Spawn
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </div>
        <SheetFooter>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
