import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";
import react from "@vitejs/plugin-react";
import tsconfigPaths from "vite-tsconfig-paths";
import Unfonts from "unplugin-fonts/vite";
import { TanStackRouterVite } from "@tanstack/router-plugin/vite";

export default defineConfig({
  plugins: [
    tsconfigPaths({}),
    TanStackRouterVite(),
    react(),
    Unfonts({
      google: {
        families: [
          {
            name: `Roboto`,
            styles: `ital,wght@0,100;0,300;0,400;0,500;0,700;0,900;1,100;1,300;1,400;1,500;1,700;1,900`,
            defer: false,
          },
          {
            name: `Roboto Condensed`,
            styles: `ital,wght@0,100..900;1,100..900`,
            defer: false,
          },
          {
            name: `Roboto Mono`,
            styles: `ital,wght@0,100..700;1,100..700`,
            defer: false,
          },
          {
            name: `Noto Sans`,
            styles: `ital,wdth,wght@0,62.5..100,100..900;1,62.5..100,100..900`,
            defer: false,
          },
        ],
      },
    }),
    viteSingleFile(),
  ],
});
