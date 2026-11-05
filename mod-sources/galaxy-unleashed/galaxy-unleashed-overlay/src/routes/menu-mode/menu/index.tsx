import { createFileRoute } from "@tanstack/react-router";
import {
  ClockIcon,
  HelpingHandIcon,
  LucideProps,
  RocketIcon,
  SwordsIcon,
  UsersIcon,
  WandSparklesIcon,
} from "lucide-react";
import {
  ForwardRefExoticComponent,
  RefAttributes,
} from "react";

import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { cn } from "@/lib/utils";

export const Route = createFileRoute("/menu-mode/menu/")({
  component: RouteComponent,
});

function MenuButton({
  title,
  description,
  stage,
  IconComponent,
  onClick,
}: {
  title: string;
  description: string;
  stage?: "Alpha" | "Beta" | "Coming soon!";
  IconComponent: ForwardRefExoticComponent<
    Omit<LucideProps, "ref"> & RefAttributes<SVGSVGElement>
  >;
  onClick?: () => void;
}) {
  return (
    <Card
      className={cn(
        "group grid h-72 cursor-pointer grid-rows-[auto_1fr] ring-primary transition-all hover:scale-110 hover:ring-1",
        stage === "Coming soon!" ? "opacity-50 cursor-not-allowed" : "",
      )}
      onClick={onClick}
    >
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="flex items-center justify-center">
        <IconComponent
          className="size-24 text-muted-foreground transition-all group-hover:size-28 group-hover:animate-jump group-hover:text-primary"
          strokeWidth={1}
        />
      </CardContent>
      <CardFooter>
        {
          stage === undefined
            ? (
                <></>
              )
            : (
                <Badge className="ml-auto" variant="outline">
                  {stage}
                </Badge>
              )
        }
      </CardFooter>
    </Card>
  );
}

function RouteComponent() {
  const navigate = Route.useNavigate();

  return (
    <div className="size-full overflow-y-auto">
      <div className="grid grid-cols-2 justify-items-stretch gap-12 p-12 xl:grid-cols-3">
        <MenuButton
          title="NPCs"
          stage="Beta"
          description="Spawn NPCs in the world."
          IconComponent={UsersIcon}
          onClick={() => navigate({ to: "/menu-mode/menu/npcs/quick-spawn", search: {} })}
        />
        <MenuButton
          title="Battle"
          stage="Beta"
          description="Create a battle scenario."
          IconComponent={SwordsIcon}
          onClick={() => navigate({ to: "/menu-mode/menu/battle" })}
        />
        <MenuButton
          title="Character Customizer"
          stage="Coming soon!"
          description="Customize your character."
          IconComponent={WandSparklesIcon}
        />
        <MenuButton
          title="Boosters"
          stage="Alpha"
          description="Apply various boosters to your character."
          IconComponent={RocketIcon}
          onClick={() => navigate({ to: "/menu-mode/menu/boosters" })}
        />
        <MenuButton
          title="More Coming Soon!"
          description="Visit Galaxy Unleashed GitHub page for more information."
          IconComponent={ClockIcon}
          onClick={
            () => window.open(
              "https://github.com/open-modding-platform/omp-lswtss",
              "_blank",
            )
          }
        />
        <MenuButton
          title="Help"
          description="Report issues and post feedback on Galaxy Unleashed GitHub page."
          IconComponent={HelpingHandIcon}
          onClick={
            () => window.open(
              "https://github.com/open-modding-platform/omp-lswtss",
              "_blank",
            )
          }
        />
      </div>
    </div>
  );
}
