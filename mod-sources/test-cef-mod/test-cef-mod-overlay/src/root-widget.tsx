import { useContext, useEffect } from "react"
import { ToastContainer, toast } from "react-toastify";
import { QuickMenuView } from "./quick-menu-view";
import { customToast } from "./custom-toast";
import { StateContext } from "./state-context";
import { SpawnerMenuView } from "./spawner-menu-view";

export function RootWidget() {

  const { spawnerMenuState, spawnerInPlayerEntityRangeId } = useContext(StateContext);

  useEffect(
    () => {

      customToast.info(
        `Press F1 to open the mod menu`, 
        { 
          toastId: `open-mod-menu-hint`,
          autoClose: 5000, 
        },
      );
    },
    [
    ],
  );

  useEffect(
    () => {

      const interval = setInterval(
        () => {
          if (spawnerMenuState === null && spawnerInPlayerEntityRangeId !== null) {

            customToast.info(
              `Press F1 to open the spawner menu`,
              {
                toastId: `open-spawner-menu-hint`,
                autoClose: false,
                position: `bottom-center`,
              },
            );
    
          }
          else {
    
            toast.dismiss(`open-spawner-menu-hint`);
          }
        },
        100,
      );

      return () => clearInterval(interval);
    },
    [
      spawnerMenuState,
      spawnerInPlayerEntityRangeId,
    ],
  );

  return (
    <div className="w-screen h-screen">
      <QuickMenuView />
      <SpawnerMenuView />
      <ToastContainer />
    </div>
  )
}
