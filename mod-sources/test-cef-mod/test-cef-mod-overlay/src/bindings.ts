export enum CharacterClass {
  Jedi,
  Sith,
  RebelResistance,
  BountyHunter,
  AstromechDroid,
  ProtocolDroid,
  Scoundrel,
  GalacticEmpire,
  Scavenger,
  Civilian,
}

export interface CharacterInfo {
  nameStringId: string;
  descriptionStringId: string;
  class: CharacterClass;
  prefabResourcePath: string;
}

export interface QuickMenuState {}

export interface SpawnerMenuState {
  spawnerId: number;
}

export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export enum NpcFactionId {
  Empire,
  Rebel,
  Sith,
  Resistance,
}

export interface NpcPresetConfig {
  prefabResourcePath: string;
  isBattleParticipant: boolean;
  overrideFactionId: NpcFactionId | null;
}

export interface SpawnerConfig {
  npcPreset: NpcPresetConfig;
  npcsMaxCount: number;
  spawnNpcTasksIntervalAsSeconds: number;
}

export interface BattleConfig {
  centerPosition: Vector3 | null;
}

interface FetchCharactersInfoArgs {}

type FetchCharactersInfoResult = Array<CharacterInfo> | null;

interface FetchQuickMenuStateArgs {}

type FetchQuickMenuStateResult = QuickMenuState | null;

interface FetchSpawnerMenuStateArgs {}

type FetchSpawnerMenuStateResult = SpawnerMenuState | null;

interface SetSpawnerMenuStateArgs {
  spawnerMenuState: SpawnerMenuState | null;
}

interface FetchPlayerEntityLastPositionArgs {}

type FetchPlayerEntityLastPositionResult = Vector3 | null;

interface CreateSpawnNpcGlobalTaskArgs {
  npcPresetConfig: NpcPresetConfig;
  npcPosition: Vector3;
}

interface CreateSpawnerArgs {
  spawnerConfig: SpawnerConfig;
  spawnerPosition: Vector3;
}

type CreateSpawnerResult = number;

interface FetchIsSpawnerActiveArgs {
  spawnerId: number;
}

type FetchIsSpawnerActiveResult = boolean;

interface SetIsSpawnerActiveArgs {
  spawnerId: number;
  isSpawnerActive: boolean;
}

interface SetShouldSpawnerBeDestroyedArgs {
  spawnerId: number;
  shouldSpawnerBeDestroyed: boolean;
}

interface FetchSpawnerConfigArgs {
  spawnerId: number;
}

type FetchSpawnerConfigResult = SpawnerConfig | null;

interface SetSpawnerConfigArgs {
  spawnerId: number;
  spawnerConfig: SpawnerConfig;
}

interface FetchSpawnerInPlayerEntityRangeIdArgs {}

type FetchSpawnerInPlayerEntityRangeIdResult = number | null;

interface EnableAllSpawnersArgs {}

interface DisableAllSpawnersArgs {}

interface FetchBattleConfigArgs {}

type FetchBattleConfigResult = BattleConfig;

interface SetBattleConfigArgs {
  battleConfig: BattleConfig;
}

interface Bindings {
  fetchCharactersInfo: (argsAsString: string) => Promise<string>;
  fetchQuickMenuState: (argsAsString: string) => Promise<string>;
  fetchSpawnerMenuState: (argsAsString: string) => Promise<string>;
  setSpawnerMenuState: (argsAsString: string) => Promise<string>;
  fetchPlayerEntityLastPosition: (argsAsString: string) => Promise<string>;
  createSpawnNpcGlobalTask: (argsAsString: string) => Promise<void>;
  createSpawner: (argsAsString: string) => Promise<string>;
  fetchIsSpawnerActive: (argsAsString: string) => Promise<string>;
  setIsSpawnerActive: (argsAsString: string) => Promise<void>;
  setShouldSpawnerBeDestroyed: (argsAsString: string) => Promise<void>;
  fetchSpawnerConfig: (argsAsString: string) => Promise<string>;
  setSpawnerConfig: (argsAsString: string) => Promise<void>;
  fetchSpawnerInPlayerEntityRangeId: (argsAsString: string) => Promise<string>;
  enableAllSpawners: (argsAsString: string) => Promise<void>;
  disableAllSpawners: (argsAsString: string) => Promise<void>;
  fetchBattleConfig: (argsAsString: string) => Promise<string>;
  setBattleConfig: (argsAsString: string) => Promise<void>;
}

const getBindings = (): Bindings => {
  return (
    window as unknown as {
      testCefMod: Bindings;
    }
  ).testCefMod;
};

export const fetchCharactersInfo = async (
  args: FetchCharactersInfoArgs
): Promise<FetchCharactersInfoResult> => {
  return JSON.parse(
    await getBindings().fetchCharactersInfo(JSON.stringify(args))
  ) as FetchCharactersInfoResult;
};

export const fetchQuickMenuState = async (
  args: FetchQuickMenuStateArgs
): Promise<FetchQuickMenuStateResult> => {
  return JSON.parse(
    await getBindings().fetchQuickMenuState(JSON.stringify(args))
  ) as FetchQuickMenuStateResult;
};

export const fetchSpawnerMenuState = async (
  args: FetchSpawnerMenuStateArgs
): Promise<FetchSpawnerMenuStateResult> => {
  return JSON.parse(
    await getBindings().fetchSpawnerMenuState(JSON.stringify(args))
  ) as FetchSpawnerMenuStateResult;
};

export const setSpawnerMenuState = async (
  args: SetSpawnerMenuStateArgs
): Promise<void> => {
  await getBindings().setSpawnerMenuState(JSON.stringify(args));
};

export const fetchPlayerEntityLastPosition = async (
  args: FetchPlayerEntityLastPositionArgs
): Promise<FetchPlayerEntityLastPositionResult> => {
  return JSON.parse(
    await getBindings().fetchPlayerEntityLastPosition(JSON.stringify(args))
  ) as FetchPlayerEntityLastPositionResult;
};

export const createSpawnNpcGlobalTask = async (
  args: CreateSpawnNpcGlobalTaskArgs
): Promise<void> => {
  await getBindings().createSpawnNpcGlobalTask(JSON.stringify(args));
};

export const createSpawner = async (
  args: CreateSpawnerArgs
): Promise<CreateSpawnerResult> => {
  return JSON.parse(
    await getBindings().createSpawner(JSON.stringify(args))
  ) as CreateSpawnerResult;
};

export const fetchIsSpawnerActive = async (
  args: FetchIsSpawnerActiveArgs
): Promise<FetchIsSpawnerActiveResult> => {
  return JSON.parse(
    await getBindings().fetchIsSpawnerActive(JSON.stringify(args))
  ) as FetchIsSpawnerActiveResult;
};

export const setIsSpawnerActive = async (
  args: SetIsSpawnerActiveArgs
): Promise<void> => {
  await getBindings().setIsSpawnerActive(JSON.stringify(args));
};

export const setShouldSpawnerBeDestroyed = async (
  args: SetShouldSpawnerBeDestroyedArgs
): Promise<void> => {
  await getBindings().setShouldSpawnerBeDestroyed(JSON.stringify(args));
};

export const fetchSpawnerConfig = async (
  args: FetchSpawnerConfigArgs
): Promise<FetchSpawnerConfigResult> => {
  return JSON.parse(
    await getBindings().fetchSpawnerConfig(JSON.stringify(args))
  ) as FetchSpawnerConfigResult;
};

export const setSpawnerConfig = async (
  args: SetSpawnerConfigArgs
): Promise<void> => {
  await getBindings().setSpawnerConfig(JSON.stringify(args));
};

export const fetchSpawnerInPlayerEntityRangeId = async (
  args: FetchSpawnerInPlayerEntityRangeIdArgs
): Promise<FetchSpawnerInPlayerEntityRangeIdResult> => {
  return JSON.parse(
    await getBindings().fetchSpawnerInPlayerEntityRangeId(JSON.stringify(args))
  ) as FetchSpawnerInPlayerEntityRangeIdResult;
};

export const enableAllSpawners = async (
  args: EnableAllSpawnersArgs
): Promise<void> => {
  await getBindings().enableAllSpawners(JSON.stringify(args));
};

export const disableAllSpawners = async (
  args: DisableAllSpawnersArgs
): Promise<void> => {
  await getBindings().disableAllSpawners(JSON.stringify(args));
};

export const fetchBattleConfig = async (
  args: FetchBattleConfigArgs
): Promise<FetchBattleConfigResult> => {
  return JSON.parse(
    await getBindings().fetchBattleConfig(JSON.stringify(args))
  ) as FetchBattleConfigResult;
};

export const setBattleConfig = async (
  args: SetBattleConfigArgs
): Promise<void> => {
  await getBindings().setBattleConfig(JSON.stringify(args));
};
