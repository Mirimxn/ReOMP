import { useContext, useState } from 'react';
import { StateContext } from './state-context';
import { NpcPresetConfig, SpawnerConfig, createSpawnNpcGlobalTask, createSpawner, disableAllSpawners, enableAllSpawners, fetchBattleConfig, fetchPlayerEntityLastPosition, setBattleConfig } from './bindings';

import { customToast } from './custom-toast';
import { MenuTab } from './menu-tab';
import { MenuButton } from './menu-button';
import { MenuButtonType } from './menu-button-type';
import { MenuView } from './menu-view';
import { MenuNpcPresetConfigInput } from './menu-npc-preset-config-input';
import { MenuSpawnerConfigInput } from './menu-spawner-config-input';

enum QuickMenuTabType {
  Main,
  SpawnNpc,
  CreateSpawner,
}

function QuickMenuMainTab({
  setTab,
  isDefault,
}: {
  setTab: (tab: QuickMenuTabType) => void;
  isDefault?: boolean;
}) {
  return (
    <MenuTab title="QUICK MENU">
      <MenuButton
        isDefault={isDefault}
        label="Spawn NPC"
        onClick={() => {
          setTab(QuickMenuTabType.SpawnNpc);
        }}
      />
      <MenuButton
        label="Create Spawner"
        onClick={() => {
          setTab(QuickMenuTabType.CreateSpawner);
        }}
      />
      <MenuButton
        label="Set Battle Center"
        onClick={async () => {

          const playerEntityLastPosition = await fetchPlayerEntityLastPosition({});

          if (playerEntityLastPosition != null) {

            const battleConfig = await fetchBattleConfig({});

            battleConfig.centerPosition = playerEntityLastPosition;

            await setBattleConfig({ battleConfig });

            customToast.info(`Battle Center set!`, {});
          }
        }}
      />
      <MenuButton
        label="Clear Battle Center"
        onClick={async () => {

          const battleConfig = await fetchBattleConfig({});

          battleConfig.centerPosition = null;

          await setBattleConfig({ battleConfig });

          customToast.info(`Battle Center cleared!`, {});
        }}
      />
      <MenuButton
        label="Enable All Spawners"
        onClick={async () => {

          await enableAllSpawners({});

          customToast.info(`Enabled all spawners!`, {});
        }}
      />
      <MenuButton
        label="Disable All Spawners"
        onClick={async () => {

          await disableAllSpawners({});

          customToast.info(`Disabled all spawners!`, {});
        }}
      />
    </MenuTab>
  );
}

function QuickMenuSpawnNpcTab({
  setTab,
  isDefault,
}: {
  setTab: (tab: QuickMenuTabType) => void;
  isDefault?: boolean;
}) {

  const [npcPresetConfig, setNpcPresetConfig] = useState<NpcPresetConfig>({
    isBattleParticipant: false,
    overrideFactionId: null,
    prefabResourcePath: `Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked`,
  });

  return (
    <MenuTab title="SPAWN NPC">
      <MenuNpcPresetConfigInput
        isDefault={isDefault}
        npcPresetConfig={npcPresetConfig}
        onNpcPresetConfigChange={setNpcPresetConfig}
      />
      <MenuButton
        label="Spawn"
        type={MenuButtonType.Approve}
        onClick={async () => {

          const playerEntityLastPosition = await fetchPlayerEntityLastPosition({});

          if (playerEntityLastPosition != null) {

            await createSpawnNpcGlobalTask({
              npcPresetConfig,
              npcPosition: playerEntityLastPosition,
            });
          }
        }}
      />
      <MenuButton
        label="Back"
        type={MenuButtonType.Disapprove}
        onClick={() => {
          setTab(QuickMenuTabType.Main);
        }}
      />
    </MenuTab>
  );
}

function QuickMenuCreateSpawnerTab({
  setTab,
  isDefault,
}: {
  setTab: (tab: QuickMenuTabType) => void;
  isDefault?: boolean;
}) {

  const [spawnerConfig, setSpawnerConfig] = useState<SpawnerConfig>({
    npcPreset: {
      isBattleParticipant: false,
      overrideFactionId: null,
      prefabResourcePath: `Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked`,
    },
    npcsMaxCount: 1,
    spawnNpcTasksIntervalAsSeconds: 1,
  });

  return (
    <MenuTab title="CREATE SPAWNER">
      <MenuSpawnerConfigInput
        isDefault={isDefault}
        spawnerConfig={spawnerConfig}
        onSpawnerConfigChange={setSpawnerConfig}
      />
      <MenuButton
        label="Create"
        type={MenuButtonType.Approve}
        onClick={async () => {

          const playerEntityLastPosition = await fetchPlayerEntityLastPosition({});

          if (playerEntityLastPosition != null) {

            await createSpawner({
              spawnerConfig,
              spawnerPosition: playerEntityLastPosition,
            });

            customToast.info(`Spawner created!`, {});
          }
        }}
      />
      <MenuButton
        label="Back"
        type={MenuButtonType.Disapprove}
        onClick={() => {
          setTab(QuickMenuTabType.Main);
        }}
      />
    </MenuTab>
  );
}

export function QuickMenuView() {

  const { quickMenuState } = useContext(StateContext);

  const [tab, setTab] = useState<QuickMenuTabType>(QuickMenuTabType.Main);

  return (
    <MenuView isOpened={quickMenuState !== null}>
      {
        tab === QuickMenuTabType.Main
          ? <QuickMenuMainTab isDefault={quickMenuState !== null} setTab={setTab} />
          : null
      }
      {
        tab === QuickMenuTabType.SpawnNpc
          ? <QuickMenuSpawnNpcTab isDefault={quickMenuState !== null} setTab={setTab} />
          : null
      }
      {
        tab === QuickMenuTabType.CreateSpawner
          ? <QuickMenuCreateSpawnerTab isDefault={quickMenuState !== null} setTab={setTab} />
          : null
      }
    </MenuView>
  );
}
