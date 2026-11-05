import {
  createFileRoute,
  Navigate,
  Outlet,
  useChildMatches,
  useNavigate,
} from "@tanstack/react-router";
import {
  ArrowLeftIcon,
  XIcon,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { TypographyMuted } from "@/components/ui/typography-muted";
import { switchToPlayMode } from "@/lib/runtime-api";

export const Route = createFileRoute("/menu-mode/menu")({
  component: RouteComponent,
  notFoundComponent: RouteNotFoundComponent,
});

function RouteComponent() {
  const childMatches = useChildMatches();

  const nonIndexChildMatches = childMatches.filter(childMatch => childMatch.fullPath != "/menu-mode/menu/");

  const lastNonIndexChildMatch = nonIndexChildMatches[nonIndexChildMatches.length - 1];

  const navigate = useNavigate();

  return (
    <div className="flex h-screen w-screen animate-fade items-center justify-center bg-black/50 animate-duration-300">
      <Card className="flex h-[90vh] w-[75vw] flex-col">
        <CardHeader className="flex flex-row items-center justify-between">
          <div className="flex flex-row items-center gap-2">
            {
              lastNonIndexChildMatch === undefined
                ? (
                    <></>
                  )
                : (
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => navigate({ to: lastNonIndexChildMatch.staticData.menuPage?.prevPageFullPath ?? `..` })}
                    >
                      <ArrowLeftIcon />
                    </Button>
                  )
            }
            <CardTitle>
              {childMatches[childMatches.length - 1]?.staticData.menuPage?.name ?? `Galaxy Unleashed`}
            </CardTitle>
          </div>
          <Button
            className="text-sm"
            size="icon"
            variant="ghost"
            onClick={async () => {
              await switchToPlayMode({});
            }}
          >
            <XIcon />
          </Button>
        </CardHeader>
        <CardContent className="grow overflow-hidden">
          <Outlet />
        </CardContent>
        <CardFooter className="flex justify-center">
          <TypographyMuted>
            Made with ❤️ by Darth Witcher for all LEGO Star Wars: The Skywalker Saga players!
          </TypographyMuted>
        </CardFooter>
      </Card>
    </div>
  );
}

function RouteNotFoundComponent() {
  return <Navigate to="/" />;
}
