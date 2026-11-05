import { useContext, useEffect, useState } from 'react';
import { StateContext } from './state-context';
import { SpawnerConfig, fetchIsSpawnerActive, fetchSpawnerConfig, setIsSpawnerActive, setShouldSpawnerBeDestroyed, setSpawnerConfig, setSpawnerMenuState } from './bindings';

import { MenuTab } from './menu-tab';
import { MenuButton } from './menu-button';
import { MenuView } from './menu-view';
import { MenuButtonType } from './menu-button-type';
import { MenuSpawnerConfigInput } from './menu-spawner-config-input';

enum SpawnerMenuTabType {
  Main,
  Config,
}

function SpawnerMenuMainTab({
  spawnerId,
  setTab,
  isDefault,
}: {
  spawnerId: number | null;
  setTab: (tab: SpawnerMenuTabType) => void;
  isDefault?: boolean;
}) {

  const [isSpawnerActive, setIsSpawnerActiveReactTemp] = useState<boolean | null>(null);

  useEffect(
    () => {

      const interval = setInterval(
        async () => {

          if (spawnerId === null) {
            return;
          }

          setIsSpawnerActiveReactTemp(await fetchIsSpawnerActive({ spawnerId }));
        },
        100,
      );

      return () => clearInterval(interval);
    },
    [
      spawnerId,
    ],
  );

  if (isSpawnerActive === null) {
    return null;
  }

  return (
    <MenuTab title="SPAWNER MENU">
      <MenuButton
        isDefault={isDefault}
        label="Config"
        onClick={() => {
          setTab(SpawnerMenuTabType.Config);
        }}
      />
      <MenuButton
        label={isSpawnerActive ? "Disable" : "Enable"}
        onClick={async () => {
          if (spawnerId === null) {
            return;
          }

          await setIsSpawnerActive({
            spawnerId,
            isSpawnerActive: !isSpawnerActive,
          });
        }}
      />
      <MenuButton
        type={MenuButtonType.Disapprove}
        label="Destroy"
        onClick={async () => {
          if (spawnerId === null) {
            return;
          }

          await setSpawnerMenuState({
            spawnerMenuState: null,
          });

          await setShouldSpawnerBeDestroyed({
            spawnerId,
            shouldSpawnerBeDestroyed: true,
          });
        }}
      />
    </MenuTab>
  );
}

function SpawnerMenuConfigTab({
  spawnerId,
  setTab,
  isDefault,
}: {
  spawnerId: number | null;
  setTab: (tab: SpawnerMenuTabType) => void;
  isDefault?: boolean;
}) {

  const [spawnerConfig, setSpawnerConfigReactTemp] = useState<SpawnerConfig | null>(null);

  useEffect(
    () => {

      const interval = setInterval(
        async () => {

          if (spawnerId === null) {
            return;
          }

          setSpawnerConfigReactTemp(await fetchSpawnerConfig({ spawnerId }));
        },
        100,
      );

      return () => clearInterval(interval);
    },
    [
      spawnerId,
    ],
  );

  if (spawnerConfig === null) {
    return null;
  }

  return (
    <MenuTab title="SPAWNER CONFIG">
      <MenuSpawnerConfigInput
        isDefault={isDefault}
        spawnerConfig={spawnerConfig}
        onSpawnerConfigChange={async (value) => {
          if (spawnerId === null) {
            return;
          }

          await setSpawnerConfig({
            spawnerId,
            spawnerConfig: value,
          });

          setSpawnerConfigReactTemp(value);
        }}
      />
      <MenuButton
        label="Back"
        type={MenuButtonType.Disapprove}
        onClick={() => {
          setTab(SpawnerMenuTabType.Main);
        }}
      />
    </MenuTab>
  );
}

export function SpawnerMenuView() {

  const { spawnerMenuState } = useContext(StateContext);

  const [tab, setTab] = useState<SpawnerMenuTabType>(SpawnerMenuTabType.Main);

  return (
    <MenuView isOpened={spawnerMenuState !== null}>
      {
        tab === SpawnerMenuTabType.Main
          ? <SpawnerMenuMainTab isDefault={spawnerMenuState !== null} spawnerId={spawnerMenuState === null ? null : spawnerMenuState.spawnerId} setTab={setTab} />
          : null
      }
      {
        tab === SpawnerMenuTabType.Config
          ? <SpawnerMenuConfigTab isDefault={spawnerMenuState !== null} spawnerId={spawnerMenuState === null ? null : spawnerMenuState.spawnerId} setTab={setTab} />
          : null
      }
    </MenuView>
  );
}
