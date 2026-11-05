import {
  createFileRoute,
  Navigate,
} from "@tanstack/react-router";

export const Route = createFileRoute("/menu-mode/")({
  component: RouteComponent,
});

function RouteComponent() {
  return <Navigate to="/menu-mode/menu" />;
}
