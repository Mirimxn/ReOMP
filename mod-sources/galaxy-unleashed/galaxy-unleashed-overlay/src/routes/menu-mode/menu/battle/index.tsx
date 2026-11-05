import {
  createFileRoute,
  useNavigate,
} from "@tanstack/react-router";
import { LogInIcon } from "lucide-react";

import { Form } from "@/components/form";
import { FormSwitchField } from "@/components/form-switch-field";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  setIsBattleActive,
  switchToManageBattleFlagMode,
  useBattleState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/menu-mode/menu/battle/")({
  component: RouteComponent,
  staticData: {
    menuPage: {
      name: "Battle",
      prevPageFullPath: "/menu-mode/menu",
    },
  },
});

function RouteComponent() {
  const navigate = useNavigate({
    from: "/menu-mode/menu/battle" satisfies typeof Route.fullPath,
  });

  const battleState = useBattleState();

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>
          Manage Battle
        </CardTitle>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <Form>
          <FormSwitchField
            label="Is Battle Active"
            description="When battle is active, NPCs that participate in the battle will try to reach the Battle Flag."
            checked={battleState.isActive}
            onCheckedChange={async (checked) => {
              await setIsBattleActive({
                isBattleActive: checked,
              });
            }}
          />
        </Form>

      </CardContent>
      <CardFooter className="flex flex-col gap-2">
        <Button
          variant="outline"
          className="w-full"
          onClick={async () => {
            await navigate({
              to: "/menu-mode/menu/npcs/manage-spawners",
            });
          }}
        >
          <LogInIcon />
          Manage NPC Spawners
        </Button>
        <Button
          className="w-full"
          onClick={async () => {
            await switchToManageBattleFlagMode({});
          }}
        >
          <LogInIcon />
          Enter Manage Battle Flag Mode
        </Button>
      </CardFooter>
    </Card>
  );
}
