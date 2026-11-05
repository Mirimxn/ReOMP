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

export const CHARACTER_CLASSES = Object.values(CharacterClass).filter(x => typeof x === "number") as CharacterClass[];

export enum CharacterFactionId {
  Empire,
  Rebel,
  Sith,
  Resistance,
}

export const CHARACTER_FACTIONS_ID = Object.values(CharacterFactionId).filter(x => typeof x === "number") as CharacterFactionId[];

export interface CharacterInfo {
  id: string;
  name: string;
  prefabResourcePath: string;
  class: CharacterClass;
}

export interface NpcPresetConfig {
  characterId: string;
  characterOverrideFactionId: CharacterFactionId | null;
}

export interface NpcSpawnerConfig {
  maxNpcsCount: number;
  npcSpawningIntervalSeconds: number;
  npcPreset: NpcPresetConfig;
  areNpcsBattleParticipants: boolean;
}

export interface JetpackBoosterConfig {
  isUnlimitedFuelEnabled: boolean;
  turboSpeedMultiplier: number;
}

export interface JumpBoosterConfig {
  isUnlimitedDoubleJumpsEnabled: boolean;
  jumpHeightMultiplier: number;
}

export interface MenuModeNavigateParams {
  to: string;
  search: object | null;
}

export interface MenuModeConfig {
  navigateParams: MenuModeNavigateParams | null;
}

export interface QuickSpawnNpcsModeConfig {
  npcPreset: NpcPresetConfig;
  isNpcBattleParticipant: boolean;
}

export interface CreateNpcSpawnersModeConfig {
  npcSpawner: NpcSpawnerConfig;
}

export interface NpcSpawnerState {
  id: string;
  config: NpcSpawnerConfig;
  isEnabled: boolean;
}

export interface BattleState {
  isActive: boolean;
}

export interface JetpackBoosterState {
  isEnabled: boolean;
  config: JetpackBoosterConfig;
}

export interface JumpBoosterState {
  isEnabled: boolean;
  config: JumpBoosterConfig;
}

export enum ModeKind {
  PlayMode,
  MenuMode,
  QuickSpawnNpcsMode,
  CreateNpcSpawnersMode,
  ManageBattleFlagMode,
}

export interface PlayModeState {
  kind: ModeKind.PlayMode;
}

export interface MenuModeState {
  kind: ModeKind.MenuMode;
  config: MenuModeConfig;
}

export interface QuickSpawnNpcsModeState {
  kind: ModeKind.QuickSpawnNpcsMode;
  config: QuickSpawnNpcsModeConfig;
}

export interface CreateNpcSpawnersModeState {
  kind: ModeKind.CreateNpcSpawnersMode;
  config: CreateNpcSpawnersModeConfig;
}

export interface ManageBattleFlagModeState {
  kind: ModeKind.ManageBattleFlagMode;
}

export type ModeState = (
  | PlayModeState
  | MenuModeState
  | QuickSpawnNpcsModeState
  | CreateNpcSpawnersModeState
  | ManageBattleFlagModeState
);

export function getCharacterClassName(characterClass: CharacterClass) {
  switch (characterClass) {
    case CharacterClass.Jedi:
      return "Jedi";
    case CharacterClass.Sith:
      return "Sith";
    case CharacterClass.RebelResistance:
      return "Rebel/Resistance";
    case CharacterClass.BountyHunter:
      return "Bounty Hunter";
    case CharacterClass.AstromechDroid:
      return "Astromech Droid";
    case CharacterClass.ProtocolDroid:
      return "Protocol Droid";
    case CharacterClass.Scoundrel:
      return "Scoundrel";
    case CharacterClass.GalacticEmpire:
      return "Galactic Empire";
    case CharacterClass.Scavenger:
      return "Scavenger";
    case CharacterClass.Civilian:
      return "Civilian";
  }
}
export function getCharacterFactionName(characterFactionId: CharacterFactionId) {
  switch (characterFactionId) {
    case CharacterFactionId.Empire:
      return "Empire";
    case CharacterFactionId.Rebel:
      return "Rebel";
    case CharacterFactionId.Sith:
      return "Sith";
    case CharacterFactionId.Resistance:
      return "Resistance";
  }
}
