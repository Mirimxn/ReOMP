import {
  createFileRoute,
  Navigate,
  Outlet,
} from "@tanstack/react-router";

import {
  ModeKind,
  useModeState,
} from "@/lib/runtime-api";

export const Route = createFileRoute("/menu-mode")({
  component: RouteComponent,
  notFoundComponent: RouteNotFoundComponent,
});

function RouteComponent() {
  const modeState = useModeState();

  if (modeState.kind !== ModeKind.MenuMode) {
    return <Navigate to="/" />;
  }

  return <Outlet />;
}

function RouteNotFoundComponent() {
  return <Navigate to="/" />;
}
