import {
  createRootRoute,
  Navigate,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Toaster } from "@/components/ui/sonner";
import { TypographyInlineCode } from "@/components/ui/typography-inline-code";
export const Route = createRootRoute({
  component: RouteComponent,
  notFoundComponent: RouteNotFoundComponent,
  errorComponent: RouteErrorComponent,
});

function RouteComponent() {
  return (
    <>
      <Outlet />
      <Toaster />
    </>
  );
}

function RouteNotFoundComponent() {
  return <Navigate to="/" />;
}

function RouteErrorComponent(
  {
    error,
  }: {
    error: unknown;
  },
) {
  const navigate = useNavigate();

  return (
    <div className="flex h-lvh w-lvw items-center justify-center p-24">
      <Card className="bg-destructive">
        <CardHeader>
          <CardTitle className="text-center text-destructive-foreground">
            Whoops!
          </CardTitle>
          <CardDescription className="text-center text-destructive-foreground">
            Something went wrong. Please use the &quot;Restart&quot; button below to reload the app.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex items-center justify-center">
          <TypographyInlineCode className="max-h-96 overflow-y-auto">
            {
              error instanceof Error
                ? (error.stack ?? String(error))
                : String(error)
            }
          </TypographyInlineCode>
        </CardContent>
        <CardFooter className="flex flex-col gap-6">
          <Button
            className="w-full"
            onClick={() => navigate({ to: "/" })}
          >
            Restart
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
