import {
  createFileRoute,
  Navigate,
  Outlet,
  useChildMatches,
  useNavigate,
} from "@tanstack/react-router";

import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs";

export const Route = createFileRoute("/menu-mode/menu/npcs")({
  component: RouteComponent,
  notFoundComponent: RouteNotFoundComponent,
});

function RouteComponent() {
  const navigate = useNavigate({
    from: "/menu-mode/menu/npcs" satisfies typeof Route.fullPath,
  });

  const childMatches = useChildMatches();

  const tab = childMatches[childMatches.length - 1]?.fullPath
    ?.split("/menu-mode/menu/npcs" satisfies typeof Route.fullPath)?.[1]
    ?.split(`/`)[1] ?? `unknown`;

  return (
    <Tabs
      value={tab}
      onValueChange={async (value) => {
        await navigate({
          to: "/menu-mode/menu/npcs" satisfies typeof Route.fullPath + `/${value}`,
        });
      }}
      className="flex size-full flex-col"
    >
      <TabsList className="grid w-full grid-cols-3">
        <TabsTrigger value="quick-spawn">Quick Spawn</TabsTrigger>
        <TabsTrigger value="create-spawners">Create Spawners</TabsTrigger>
        <TabsTrigger value="manage-spawners">Manage Spawners</TabsTrigger>
      </TabsList>
      <TabsContent className="grow overflow-hidden" value={tab}>
        <Outlet />
      </TabsContent>
    </Tabs>
  );
}

function RouteNotFoundComponent() {
  return <Navigate to="/" />;
}
