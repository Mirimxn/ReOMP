import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  NpcPresetConfig,
  NpcSpawnerConfig,
} from "@/lib/runtime-api";

import { FormNpcPresetConfigField } from "./form-npc-preset-config-field";
import { FormSliderField } from "./form-slider-field";
import { FormSwitchField } from "./form-switch-field";

export function FormNpcSpawnerConfigFields(
  {
    npcSpawnerInitialConfig,
    onNpcSpawnerConfigChange,
  }: {
    npcSpawnerInitialConfig?: NpcSpawnerConfig;
    onNpcSpawnerConfigChange: (npcPresetConfig: NpcSpawnerConfig) => void;
  },
) {
  const [maxNpcsCount, setMaxNpcsCount] = useState<number>(npcSpawnerInitialConfig?.maxNpcsCount ?? 5);
  const [npcSpawningIntervalSeconds, setNpcSpawningIntervalSeconds] = useState<number>(npcSpawnerInitialConfig?.npcSpawningIntervalSeconds ?? 10);
  const [npcPresetConfig, setNpcPresetConfig] = useState<NpcPresetConfig | undefined>(npcSpawnerInitialConfig?.npcPreset);
  const [areNpcsBattleParticipants, setAreNpcsBattleParticipants] = useState<boolean>(npcSpawnerInitialConfig?.areNpcsBattleParticipants ?? true);

  const npcSpawnerConfig = useMemo<NpcSpawnerConfig | undefined>(
    () => {
      if (npcPresetConfig === undefined) {
        return undefined;
      }

      return {
        maxNpcsCount,
        npcSpawningIntervalSeconds,
        npcPreset: npcPresetConfig,
        areNpcsBattleParticipants,
      };
    },
    [
      maxNpcsCount,
      npcSpawningIntervalSeconds,
      npcPresetConfig,
      areNpcsBattleParticipants,
    ],
  );

  useEffect(
    () => {
      if (npcSpawnerConfig !== undefined) {
        onNpcSpawnerConfigChange(npcSpawnerConfig);
      }
    },
    [
      npcSpawnerConfig,
      onNpcSpawnerConfigChange,
    ],
  );

  return (
    <>
      <FormNpcPresetConfigField
        label="NPC Preset"
        description="NPC preset to use for spawning NPCs."
        npcPresetInitialConfig={npcPresetConfig}
        onNpcPresetConfigChange={setNpcPresetConfig}
      />
      <FormSliderField
        label="Max NPCs Count"
        description="Maximum number of NPCs to spawn."
        min={0}
        max={10}
        value={maxNpcsCount}
        onValueChange={(newValue) => {
          setMaxNpcsCount(newValue);
        }}
        formatValue={value => String(value)}
      />
      <FormSliderField
        label="NPC Spawning Interval"
        description="Number of seconds between NPCs are spawned."
        min={1}
        max={60}
        value={npcSpawningIntervalSeconds}
        onValueChange={(newValue) => {
          setNpcSpawningIntervalSeconds(newValue);
        }}
        formatValue={value => `${String(value)} seconds`}
      />
      <FormSwitchField
        label="Should NPCs Participate In Battle"
        description="If enabled, NPCs will participate in the battle."
        checked={areNpcsBattleParticipants}
        onCheckedChange={setAreNpcsBattleParticipants}
      />
    </>
  );
}
