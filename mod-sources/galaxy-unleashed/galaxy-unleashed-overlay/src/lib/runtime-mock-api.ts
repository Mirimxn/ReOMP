import * as zustand from "zustand";

import {
  BattleState,
  CHARACTER_FACTIONS_ID,
  CharacterClass,
  CharacterInfo,
  CreateNpcSpawnersModeConfig,
  MenuModeConfig,
  ModeKind,
  ModeState,
  NpcSpawnerConfig,
  NpcSpawnerState,
  QuickSpawnNpcsModeConfig,
} from "./runtime-shared-api";

function getRandomInt(min: number, max: number) {
  const minCeiled = Math.ceil(min);
  const maxFloored = Math.floor(max);
  return Math.floor(Math.random() * (maxFloored - minCeiled) + minCeiled);
}

const charactersInfo: CharacterInfo[] = [
  {
    id: "luke-skywalker",
    name: "Luke Skywalker",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Jedi,
  },
  {
    id: "r2-d2",
    name: "R2-D2",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.AstromechDroid,
  },
  {
    id: "c3-po",
    name: "C-3PO",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.ProtocolDroid,
  },
  {
    id: "darth-vader",
    name: "Darth Vader",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Sith,
  },
  {
    id: "leia-organa",
    name: "Leia Organa",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.RebelResistance,
  },
  {
    id: "han-solo",
    name: "Han Solo",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Scoundrel,
  },
  {
    id: "stormtrooper",
    name: "Stormtrooper",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.GalacticEmpire,
  },
  {
    id: "imperial-officer",
    name: "Imperial Officer",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.GalacticEmpire,
  },
  {
    id: "boba-fett",
    name: "Boba Fett",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Scoundrel,
  },
  {
    id: "qui-gon-jinn",
    name: "Qui-Gon Jinn",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Jedi,
  },
  {
    id: "rey-jakku",
    name: "Rey (Jakku)",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Scavenger,
  },
  {
    id: "jabba-the-hut",
    name: "Jabba The Hut",
    prefabResourcePath: ".prefab_baked",
    class: CharacterClass.Civilian,
  },
];

export function useCharactersInfo() {
  return charactersInfo;
}

const npcSpawnersInitialState: NpcSpawnerState[] = [];

for (let i = 0; i < getRandomInt(5, 15); i++) {
  npcSpawnersInitialState.push({
    id: i.toString(),
    config: {
      maxNpcsCount: getRandomInt(1, 15),
      npcSpawningIntervalSeconds: getRandomInt(1, 60),
      npcPreset: {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        characterId: charactersInfo[getRandomInt(0, charactersInfo.length)]!.id,
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        characterOverrideFactionId: Math.random() > 0.5 ? CHARACTER_FACTIONS_ID[getRandomInt(0, CHARACTER_FACTIONS_ID.length)]! : null,
      },
      areNpcsBattleParticipants: Math.random() > 0.5,
    },
    isEnabled: Math.random() > 0.5,
  });
}

interface NpcSpawnersStateMock {
  npcSpawnersState: NpcSpawnerState[];
  setNpcSpawnersState: (npcSpawnersState: NpcSpawnerState[]) => void;
}

const useNpcSpawnersStateMock = zustand.create<NpcSpawnersStateMock>(setNpcSpawnersStateMock => ({
  npcSpawnersState: npcSpawnersInitialState,
  setNpcSpawnersState: (npcSpawnersState) => { setNpcSpawnersStateMock({ npcSpawnersState }); },
}));

export function useNpcSpawnersState() {
  return useNpcSpawnersStateMock(x => x.npcSpawnersState);
}

export async function setIsNpcSpawnerEnabled(
  {
    npcSpawnerId,
    isNpcSpawnerEnabled,
  }: {
    npcSpawnerId: string;
    isNpcSpawnerEnabled: boolean;
  },
) {
  const npcSpawnersState = useNpcSpawnersStateMock.getState().npcSpawnersState;

  const npcSpawnerStateIndex = npcSpawnersState.findIndex(x => x.id === npcSpawnerId);

  if (npcSpawnerStateIndex === -1) {
    return;
  }

  const npcSpawnerState: NpcSpawnerState = {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    ...npcSpawnersState[npcSpawnerStateIndex]!,
    isEnabled: isNpcSpawnerEnabled,
  };

  const npcSpawnersNewState = [
    ...npcSpawnersState,
  ];

  npcSpawnersNewState[npcSpawnerStateIndex] = npcSpawnerState;

  useNpcSpawnersStateMock.getState().setNpcSpawnersState(npcSpawnersNewState);
}

export async function setNpcSpawnerConfig(
  {
    npcSpawnerId,
    npcSpawnerConfig,
  }: {
    npcSpawnerId: string;
    npcSpawnerConfig: NpcSpawnerConfig;
  },
) {
  const npcSpawnersState = useNpcSpawnersStateMock.getState().npcSpawnersState;

  const npcSpawnerStateIndex = npcSpawnersState.findIndex(x => x.id === npcSpawnerId);

  if (npcSpawnerStateIndex === -1) {
    return;
  }

  const npcSpawnerState: NpcSpawnerState = {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    ...npcSpawnersState[npcSpawnerStateIndex]!,
    config: npcSpawnerConfig,
  };

  const npcSpawnersNewState = [
    ...npcSpawnersState,
  ];

  npcSpawnersNewState[npcSpawnerStateIndex] = npcSpawnerState;

  useNpcSpawnersStateMock.getState().setNpcSpawnersState(npcSpawnersNewState);
}

export async function destroyNpcSpawner(
  {
    npcSpawnerId,
  }: {
    npcSpawnerId: string;
  },
) {
  const npcSpawnersState = useNpcSpawnersStateMock.getState().npcSpawnersState;

  const npcSpawnerIndex = npcSpawnersState.findIndex(x => x.id === npcSpawnerId);

  if (npcSpawnerIndex === -1) {
    return;
  }

  const npcSpawnersNewState = [...npcSpawnersState];

  npcSpawnersNewState.splice(npcSpawnerIndex, 1);

  useNpcSpawnersStateMock.getState().setNpcSpawnersState(npcSpawnersNewState);
}

interface BattleStateMock {
  battleState: BattleState;
  setBattleState: (battleState: BattleState) => void;
}

const useBattleStateMock = zustand.create<BattleStateMock>(setBattleStateMock => ({
  battleState: {
    isActive: false,
  },
  setBattleState: (battleState) => { setBattleStateMock({ battleState }); },
}));

export function useBattleState() {
  return useBattleStateMock(x => x.battleState);
}

export async function setIsBattleActive(
  {
    isBattleActive,
  }: {
    isBattleActive: boolean;
  },
) {
  useBattleStateMock.getState().setBattleState({
    ...useBattleStateMock.getState().battleState,
    isActive: isBattleActive,
  });
}

interface ModeStateMock {
  modeState: ModeState;
  setModeState: (modeState: ModeState) => void;
}

const useModeStateMock = zustand.create<ModeStateMock>(setModeStateMock => ({
  modeState: {
    kind: ModeKind.MenuMode,
    config: {
      navigateParams: {
        to: "/menu-mode/menu",
        search: null,
      },
    },
  },
  setModeState: (modeState) => { setModeStateMock({ modeState }); },
}));

export function useModeState() {
  return useModeStateMock(x => x.modeState);
}

export async function switchToPlayMode(
  {
  }: {
  },
) {
  useModeStateMock.getState().setModeState({
    kind: ModeKind.PlayMode,
  });
}

export async function switchToMenuMode(
  {
    menuModeConfig,
  }: {
    menuModeConfig: MenuModeConfig;
  },
) {
  useModeStateMock.getState().setModeState({
    kind: ModeKind.MenuMode,
    config: menuModeConfig,
  });
}

export async function switchToQuickSpawnNpcsMode(
  {
    quickSpawnNpcsModeConfig,
  }: {
    quickSpawnNpcsModeConfig: QuickSpawnNpcsModeConfig;
  },
) {
  useModeStateMock.getState().setModeState({
    kind: ModeKind.QuickSpawnNpcsMode,
    config: quickSpawnNpcsModeConfig,
  });
}

export async function switchToCreateSpawnersMode(
  {
    createSpawnersModeConfig,
  }: {
    createSpawnersModeConfig: CreateNpcSpawnersModeConfig;
  },
) {
  useModeStateMock.getState().setModeState({
    kind: ModeKind.CreateNpcSpawnersMode,
    config: createSpawnersModeConfig,
  });
}

export async function switchToManageBattleFlagMode(
  {
  }: {
  },
) {
  useModeStateMock.getState().setModeState({
    kind: ModeKind.ManageBattleFlagMode,
  });
}
