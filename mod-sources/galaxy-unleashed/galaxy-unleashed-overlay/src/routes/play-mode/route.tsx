import {
  createFileRoute,
  Navigate,
  Outlet,
} from "@tanstack/react-router";

import {
  ModeKind,
  useModeState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/play-mode")({
  component: RouteComponent,
  notFoundComponent: RouteNotFoundComponent,
});

function RouteComponent() {
  const modeState = useModeState();

  if (modeState.kind !== ModeKind.PlayMode) {
    return <Navigate to="/" />;
  }

  return <Outlet />;
}

function RouteNotFoundComponent() {
  return <Navigate to="/" />;
}
