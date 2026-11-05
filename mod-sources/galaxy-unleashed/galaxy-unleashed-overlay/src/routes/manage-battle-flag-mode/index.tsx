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
import { switchToMenuMode } from "@/lib/runtime-api";

export const Route = createFileRoute("/manage-battle-flag-mode/")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <Sheet
      modal={false}
      open={true}
      onOpenChange={async (value) => {
        if (!value) {
          await switchToMenuMode({
            menuModeConfig: {
              navigateParams: {
                to: `/menu-mode/menu/battle`,
                search: null,
              },
            },
          });
        }
      }}
    >
      <SheetContent closeButtonHidden side="right">
        <SheetHeader>
          <SheetTitle>Manage Battle Flag</SheetTitle>
        </SheetHeader>
        <div className="flex size-full flex-col justify-end">
          <Table>
            <TableBody>
              <TableRow>
                <TableCell className="font-bold">F1</TableCell>
                <TableCell>Back to Menu</TableCell>
              </TableRow>
              <TableRow>
                <TableCell className="font-bold">F2</TableCell>
                <TableCell>Place Battle Flag</TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </div>
        <SheetFooter></SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
