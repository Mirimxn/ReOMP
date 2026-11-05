import { createContext, useEffect, useState } from "react";
import { CharacterInfo, QuickMenuState, SpawnerMenuState, fetchCharactersInfo, fetchQuickMenuState, fetchSpawnerInPlayerEntityRangeId, fetchSpawnerMenuState } from "./bindings";

export interface State {
  charactersInfo: Array<CharacterInfo> | null;
  quickMenuState: QuickMenuState | null;
  spawnerMenuState: SpawnerMenuState | null;
  spawnerInPlayerEntityRangeId: number | null;
}

export const StateContext = createContext<State>(undefined!);

export const StateProvider = (
  {
    children,
  }: {
    children: React.ReactNode;
  },
) => {

  const [charactersInfo, setCharactersInfo] = useState<Array<CharacterInfo> | null>(null);

  const [quickMenuState, setQuickMenuState] = useState<QuickMenuState | null>(null);

  const [spawnerMenuState, setSpawnerMenuState] = useState<SpawnerMenuState | null>(null);

  const [spawnerInPlayerEntityRangeId, setSpawnerInPlayerEntityRangeId] = useState<number | null>(null);

  useEffect(() => {

    const interval = setInterval(
      async () => {

        if (charactersInfo === null) {
          setCharactersInfo(await fetchCharactersInfo({}));
        }

        setQuickMenuState(await fetchQuickMenuState({}));
        setSpawnerMenuState(await fetchSpawnerMenuState({}));
        setSpawnerInPlayerEntityRangeId(await fetchSpawnerInPlayerEntityRangeId({}));
      },
      100,
    );

    return () => clearInterval(interval);
  }, [charactersInfo]);

  useEffect(() => {

    const interval = setInterval(async () => {

    }, 100);

    return () => clearInterval(interval);
  }, []);

  return (
    <StateContext.Provider
      value={{
        charactersInfo,
        quickMenuState,
        spawnerMenuState,
        spawnerInPlayerEntityRangeId,
      }}
    >
      {children}
    </StateContext.Provider>
  );
};