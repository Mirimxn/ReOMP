import {
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import {
  createRouter,
  RouterProvider,
} from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { routeTree } from "./route-tree";

import "./index.css";
import "unfonts.css";

const queryClient = new QueryClient();

const router = createRouter({
  routeTree,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
  interface StaticDataRouteOption {
    menuPage?: {
      name: string;
      prevPageFullPath: string;
    };
  }
}

// eslint-disable-next-line @typescript-eslint/no-floating-promises
(async () => {
  try {
    await (
      window as unknown as {
        CefSharp: {
          BindObjectAsync: () => Promise<void>;
        };
      }
    ).CefSharp.BindObjectAsync();
  } catch {
    // empty
  }

  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  createRoot(document.getElementById(`root`)!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </StrictMode>,
  );
})();
