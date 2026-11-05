/* eslint-disable @typescript-eslint/no-empty-object-type */
import { useSuspenseQuery } from "@tanstack/react-query";

import {
  BattleState,
  CharacterInfo,
  CreateNpcSpawnersModeConfig,
  JetpackBoosterConfig,
  JetpackBoosterState,
  JumpBoosterConfig,
  JumpBoosterState,
  MenuModeConfig,
  ModeState,
  NpcSpawnerConfig,
  NpcSpawnerState,
  QuickSpawnNpcsModeConfig,
} from "./runtime-shared-api";

interface RuntimeApi {
  fetchCharactersInfo: (argsAsString: string) => Promise<string>;
  fetchNpcSpawnersState: (argsAsString: string) => Promise<string>;
  fetchClosestNpcSpawnerInPlayerEntityRangeId: (argsAsString: string) => Promise<string>;
  setIsNpcSpawnerEnabled: (argsAsString: string) => Promise<void>;
  setNpcSpawnerConfig: (argsAsString: string) => Promise<void>;
  destroyNpcSpawner: (argsAsString: string) => Promise<void>;
  fetchBattleState: (argsAsString: string) => Promise<string>;
  setIsBattleActive: (argsAsString: string) => Promise<void>;
  fetchJetpackBoosterState: (argsAsString: string) => Promise<string>;
  setIsJetpackBoosterEnabled: (argsAsString: string) => Promise<void>;
  setJetpackBoosterConfig: (argsAsString: string) => Promise<void>;
  fetchJumpBoosterState: (argsAsString: string) => Promise<string>;
  setIsJumpBoosterEnabled: (argsAsString: string) => Promise<void>;
  setJumpBoosterConfig: (argsAsString: string) => Promise<void>;
  fetchModeState: (argsAsString: string) => Promise<string>;
  switchToPlayMode: (argsAsString: string) => Promise<void>;
  switchToMenuMode: (argsAsString: string) => Promise<void>;
  switchToQuickSpawnNpcsMode: (argsAsString: string) => Promise<void>;
  switchToCreateNpcSpawnersMode: (argsAsString: string) => Promise<void>;
  switchToManageBattleFlagMode: (argsAsString: string) => Promise<void>;
}

function getRuntimeApi() {
  return (
    window as unknown as {
      galaxyUnleashedRuntimeApi: RuntimeApi;
    }
  ).galaxyUnleashedRuntimeApi;
}

export const fetchCharactersInfo = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchCharactersInfo(JSON.stringify(args)),
  ) as CharacterInfo[];
};

export function useCharactersInfo() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.CharactersInfo`],
    queryFn: fetchCharactersInfo,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: Infinity,
    refetchInterval: false,
  }).data;
}

export const fetchNpcSpawnersState = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchNpcSpawnersState(JSON.stringify(args)),
  ) as NpcSpawnerState[];
};

export function useNpcSpawnersState() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.NpcSpawnersState`],
    queryFn: fetchNpcSpawnersState,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export const fetchClosestNpcSpawnerInPlayerEntityRangeId = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchClosestNpcSpawnerInPlayerEntityRangeId(JSON.stringify(args)),
  ) as string | null;
};

export function useClosestNpcSpawnerInPlayerEntityRangeId() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.ClosestNpcSpawnerInPlayerEntityRangeId`],
    queryFn: fetchClosestNpcSpawnerInPlayerEntityRangeId,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export async function setIsNpcSpawnerEnabled(
  args: {
    npcSpawnerId: string;
    isNpcSpawnerEnabled: boolean;
  },
) {
  await getRuntimeApi().setIsNpcSpawnerEnabled(JSON.stringify(args));
}

export async function setNpcSpawnerConfig(
  args: {
    npcSpawnerId: string;
    npcSpawnerConfig: NpcSpawnerConfig;
  },
) {
  await getRuntimeApi().setNpcSpawnerConfig(JSON.stringify(args));
}

export async function destroyNpcSpawner(
  args: {
    npcSpawnerId: string;
  },
) {
  await getRuntimeApi().destroyNpcSpawner(JSON.stringify(args));
}

export const fetchBattleState = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchBattleState(JSON.stringify(args)),
  ) as BattleState;
};

export function useBattleState() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.BattleState`],
    queryFn: fetchBattleState,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export async function setIsBattleActive(
  args: {
    isBattleActive: boolean;
  },
) {
  await getRuntimeApi().setIsBattleActive(JSON.stringify(args));
}

export const fetchJetpackBoosterState = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchJetpackBoosterState(JSON.stringify(args)),
  ) as JetpackBoosterState;
};

export function useJetpackBoosterState() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.JetpackBoosterState`],
    queryFn: fetchJetpackBoosterState,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export async function setIsJetpackBoosterEnabled(
  args: {
    isJetpackBoosterEnabled: boolean;
  },
) {
  await getRuntimeApi().setIsJetpackBoosterEnabled(JSON.stringify(args));
}

export async function setJetpackBoosterConfig(
  args: {
    jetpackBoosterConfig: JetpackBoosterConfig;
  },
) {
  await getRuntimeApi().setJetpackBoosterConfig(JSON.stringify(args));
}

export const fetchJumpBoosterState = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchJumpBoosterState(JSON.stringify(args)),
  ) as JumpBoosterState;
};

export function useJumpBoosterState() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.JumpBoosterState`],
    queryFn: fetchJumpBoosterState,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export async function setIsJumpBoosterEnabled(
  args: {
    isJumpBoosterEnabled: boolean;
  },
) {
  await getRuntimeApi().setIsJumpBoosterEnabled(JSON.stringify(args));
}

export async function setJumpBoosterConfig(
  args: {
    jumpBoosterConfig: JumpBoosterConfig;
  },
) {
  await getRuntimeApi().setJumpBoosterConfig(JSON.stringify(args));
}

export const fetchModeState = async (
  args: {},
) => {
  return JSON.parse(
    await getRuntimeApi().fetchModeState(JSON.stringify(args)),
  ) as ModeState;
};

export function useModeState() {
  return useSuspenseQuery({
    queryKey: [`GalaxyUnleashed.ModeState`],
    queryFn: fetchModeState,
    networkMode: `always`,
    retry: true,
    retryDelay: 100,
    staleTime: 100,
    refetchInterval: 100,
    refetchIntervalInBackground: true,
  }).data;
}

export async function switchToPlayMode(
  args: {
  },
) {
  await getRuntimeApi().switchToPlayMode(JSON.stringify(args));
}

export async function switchToMenuMode(
  args: {
    menuModeConfig: MenuModeConfig;
  },
) {
  await getRuntimeApi().switchToMenuMode(JSON.stringify(args));
}

export async function switchToQuickSpawnNpcsMode(
  args: {
    quickSpawnNpcsModeConfig: QuickSpawnNpcsModeConfig;
  },
) {
  await getRuntimeApi().switchToQuickSpawnNpcsMode(JSON.stringify(args));
}

export async function switchToCreateNpcSpawnersMode(
  args: {
    createNpcSpawnersModeConfig: CreateNpcSpawnersModeConfig;
  },
) {
  await getRuntimeApi().switchToCreateNpcSpawnersMode(JSON.stringify(args));
}

export async function switchToManageBattleFlagMode(
  args: {
  },
) {
  await getRuntimeApi().switchToManageBattleFlagMode(JSON.stringify(args));
}
