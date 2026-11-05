import "./main.css";
import "react-toastify/dist/ReactToastify.css";

import React from "react";
import ReactDOM from "react-dom/client";
import * as spatialNavigation from "@noriginmedia/norigin-spatial-navigation";

import { RootWidget } from "./root-widget";
import { StateProvider } from "./state-context";

(async () => {

  spatialNavigation.init({
    shouldFocusDOMNode: true,
    shouldUseNativeEvents: true,
  });

  spatialNavigation.setKeyMap({
    left: [65, 37, 'ArrowLeft'],
    up: [87, 38, 'ArrowUp'],
    right: [68, 39, 'ArrowRight'],
    down: [83, 40, 'ArrowDown'],
    enter: [13, 'Enter']
  });

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

  ReactDOM.createRoot(document.getElementById(`root`)!).render(
    <React.StrictMode>
      <StateProvider>
        <RootWidget />
      </StateProvider>
    </React.StrictMode>,
  );
})();