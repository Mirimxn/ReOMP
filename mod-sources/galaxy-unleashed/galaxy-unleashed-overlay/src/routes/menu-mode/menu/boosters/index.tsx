import { createFileRoute } from "@tanstack/react-router";
import {
  useEffect,
  useMemo,
  useState,
} from "react";

import { Form } from "@/components/form";
import { FormSliderField } from "@/components/form-slider-field";
import { FormSwitchField } from "@/components/form-switch-field";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableRow,
} from "@/components/ui/table";
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs";
import {
  JetpackBoosterConfig,
  JumpBoosterConfig,
  setIsJetpackBoosterEnabled,
  setIsJumpBoosterEnabled,
  setJetpackBoosterConfig,
  setJumpBoosterConfig,
  useJetpackBoosterState,
  useJumpBoosterState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/menu-mode/menu/boosters/")({
  component: RouteComponent,
  staticData: {
    menuPage: {
      name: "Boosters",
      prevPageFullPath: "/menu-mode/menu",
    },
  },
});

function JetpackBoosterTab() {
  const jetpackBoosterState = useJetpackBoosterState();

  const [isUnlimitedFuelEnabled, setIsUnlimitedFuelEnabled] = useState(jetpackBoosterState.config.isUnlimitedFuelEnabled);
  const [turboSpeedMultiplier, setTurboSpeedMultiplier] = useState(jetpackBoosterState.config.turboSpeedMultiplier);

  const jetpackBoosterConfig = useMemo<JetpackBoosterConfig>(
    () => ({
      isUnlimitedFuelEnabled,
      turboSpeedMultiplier,
    }),
    [
      isUnlimitedFuelEnabled,
      turboSpeedMultiplier,
    ],
  );

  useEffect(
    () => {
      // eslint-disable-next-line @typescript-eslint/no-floating-promises
      setJetpackBoosterConfig({
        jetpackBoosterConfig,
      });
    },
    [
      jetpackBoosterConfig,
    ],
  );

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>
          Jetpack Booster
        </CardTitle>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <Form>
          <FormSwitchField
            label="Is Enabled"
            description="When enabled, you can use the Jetpack Booster."
            checked={jetpackBoosterState.isEnabled}
            onCheckedChange={async (checked) => {
              await setIsJetpackBoosterEnabled({
                isJetpackBoosterEnabled: checked,
              });
            }}
          />
          {
            !jetpackBoosterState.isEnabled
              ? <></>
              : (
                  <>
                    <FormSwitchField
                      label="Unlimited Fuel"
                      description="When enabled, you have unlimited fuel."
                      checked={isUnlimitedFuelEnabled}
                      onCheckedChange={setIsUnlimitedFuelEnabled}
                    />
                    <FormSliderField
                      label="Turbo Speed Multiplier"
                      description="Multiplies the speed when flying with Turbo."
                      value={turboSpeedMultiplier}
                      onValueChange={setTurboSpeedMultiplier}
                      min={1}
                      max={10}
                      formatValue={value => `${String(value)}x`}
                    />
                  </>
                )
          }
        </Form>
      </CardContent>
      <CardFooter>
        <Table>
          <TableBody>
            <TableRow>
              <TableCell className="font-bold">Shift (hold)</TableCell>
              <TableCell>
                Activate Turbo
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell className="font-bold">C</TableCell>
              <TableCell>
                Fly Upwards
              </TableCell>
            </TableRow>
            <TableRow>
              <TableCell className="font-bold">X</TableCell>
              <TableCell>
                Fly Downwards
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </CardFooter>
    </Card>
  );
}

function JumpBoosterTab() {
  const jumpBoosterState = useJumpBoosterState();

  const [isUnlimitedDoubleJumpsEnabled, setIsUnlimitedDoubleJumpsEnabled] = useState(jumpBoosterState.config.isUnlimitedDoubleJumpsEnabled);
  const [jumpHeightMultiplier, setJumpHeightMultiplier] = useState(jumpBoosterState.config.jumpHeightMultiplier);

  const jumpBoosterConfig = useMemo<JumpBoosterConfig>(
    () => ({
      isUnlimitedDoubleJumpsEnabled,
      jumpHeightMultiplier,
    }),
    [
      isUnlimitedDoubleJumpsEnabled,
      jumpHeightMultiplier,
    ],
  );

  useEffect(
    () => {
      // eslint-disable-next-line @typescript-eslint/no-floating-promises
      setJumpBoosterConfig({
        jumpBoosterConfig,
      });
    },
    [
      jumpBoosterConfig,
    ],
  );

  return (
    <Card className="flex size-full flex-col">
      <CardHeader>
        <CardTitle>
          Jump Booster
        </CardTitle>
      </CardHeader>
      <CardContent className="grow overflow-hidden">
        <Form>
          <FormSwitchField
            label="Is Enabled"
            description="When enabled, you can use the Jump Booster."
            checked={jumpBoosterState.isEnabled}
            onCheckedChange={async (checked) => {
              await setIsJumpBoosterEnabled({
                isJumpBoosterEnabled: checked,
              });
            }}
          />
          {
            !jumpBoosterState.isEnabled
              ? <></>
              : (
                  <>
                    <FormSwitchField
                      label="Unlimited Double Jumps"
                      description="When enabled, you can double jump an unlimited amount of times."
                      checked={isUnlimitedDoubleJumpsEnabled}
                      onCheckedChange={setIsUnlimitedDoubleJumpsEnabled}
                    />
                    <FormSliderField
                      label="Jump Height Multiplier"
                      description="Multiplies the height of your jumps."
                      value={jumpHeightMultiplier}
                      onValueChange={setJumpHeightMultiplier}
                      min={1}
                      max={10}
                      formatValue={value => `${String(value)}x`}
                    />
                  </>
                )
          }
        </Form>
      </CardContent>
    </Card>
  );
}
function RouteComponent() {
  return (
    <Tabs
      className="flex size-full flex-col"
      defaultValue="jetpack-booster"
    >
      <TabsList className="grid w-full grid-cols-2">
        <TabsTrigger value="jetpack-booster">Jetpack Booster</TabsTrigger>
        <TabsTrigger value="jump-booster">Jump Booster</TabsTrigger>
      </TabsList>
      <TabsContent className="grow overflow-hidden" value="jetpack-booster">
        <JetpackBoosterTab />
      </TabsContent>
      <TabsContent className="grow overflow-hidden" value="jump-booster">
        <JumpBoosterTab />
      </TabsContent>
    </Tabs>
  );
}
