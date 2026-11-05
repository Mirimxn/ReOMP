import { SpawnerConfig } from "./bindings";
import { MenuInputLabel } from "./menu-input-label";
import { MenuNpcPresetConfigInput } from "./menu-npc-preset-config-input";
import { MenuRangeInput } from "./menu-range-input";

export function MenuSpawnerConfigInput(
  {
    spawnerConfig,
    onSpawnerConfigChange,
    isDefault,
  }: {
    spawnerConfig: SpawnerConfig;
    onSpawnerConfigChange: (value: SpawnerConfig) => void;
    isDefault?: boolean;
  }
) {

  return (
    <>
      <MenuNpcPresetConfigInput
        isDefault={isDefault}
        npcPresetConfig={spawnerConfig.npcPreset}
        onNpcPresetConfigChange={
          (npcPresetConfig) => {
            onSpawnerConfigChange({
              ...spawnerConfig,
              npcPreset: npcPresetConfig,
            });
          }
        }
      />
      <MenuInputLabel>NPCS MAX COUNT</MenuInputLabel>
      <MenuRangeInput
        label={`${spawnerConfig.npcsMaxCount}`}
        value={spawnerConfig.npcsMaxCount}
        minValue={1}
        maxValue={10}
        onValueChange={(value) => onSpawnerConfigChange({
          ...spawnerConfig,
          npcsMaxCount: value,
        })}
      />
      <MenuInputLabel>SPAWN INTERVAL</MenuInputLabel>
      <MenuRangeInput
        label={`${spawnerConfig.spawnNpcTasksIntervalAsSeconds}s`}
        value={spawnerConfig.spawnNpcTasksIntervalAsSeconds}
        minValue={1}
        maxValue={10}
        onValueChange={(value) => onSpawnerConfigChange({
          ...spawnerConfig,
          spawnNpcTasksIntervalAsSeconds: value,
        })}
      />
    </>
  );
}