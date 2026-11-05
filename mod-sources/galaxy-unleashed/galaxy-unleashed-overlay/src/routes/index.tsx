import {
  createFileRoute,
  Navigate,
} from "@tanstack/react-router";

import {
  ModeKind,
  useModeState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/")({
  component: RouteComponent,
});

function RouteComponent() {
  const modeState = useModeState();

  switch (modeState.kind) {
    case ModeKind.PlayMode: {
      return <Navigate to="/play-mode" />;
    }
    case ModeKind.MenuMode: {
      return (
        <Navigate
          to={modeState.config.navigateParams?.to ?? "/menu-mode"}
          search={modeState.config.navigateParams?.search ?? undefined}
        />
      );
    }
    case ModeKind.QuickSpawnNpcsMode: {
      return <Navigate to="/quick-spawn-npcs-mode" />;
    }
    case ModeKind.CreateNpcSpawnersMode: {
      return <Navigate to="/create-npc-spawners-mode" />;
    }
    case ModeKind.ManageBattleFlagMode: {
      return <Navigate to="/manage-battle-flag-mode" />;
    }
  }

  return null;
}
