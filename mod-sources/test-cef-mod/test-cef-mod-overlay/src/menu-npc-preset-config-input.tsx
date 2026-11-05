import { NpcFactionId, NpcPresetConfig } from "./bindings";
import { MenuButton } from "./menu-button";
import { MenuInputLabel } from "./menu-input-label";
import { MenuNpcPrefabResourcePathInput } from "./menu-npc-prefab-resource-path-input";
import { MenuOptionInput } from "./menu-option-input";

export function MenuNpcPresetConfigInput(
  {
    npcPresetConfig,
    onNpcPresetConfigChange,
    isDefault,
  }: {
    npcPresetConfig: NpcPresetConfig;
    onNpcPresetConfigChange: (value: NpcPresetConfig) => void;
    isDefault?: boolean;
  }
) {

  return (
    <>
      <MenuInputLabel>NPC PREFAB</MenuInputLabel>
      <MenuNpcPrefabResourcePathInput
        isDefault={isDefault}
        value={npcPresetConfig.prefabResourcePath}
        onValueChange={(value) => {
          onNpcPresetConfigChange({
            ...npcPresetConfig,
            prefabResourcePath: value,
          });
        }}
      />
      <MenuInputLabel>IS NPC BATTLE PARTICIPANT</MenuInputLabel>
      <MenuButton
        label={`${npcPresetConfig.isBattleParticipant ? `Yes` : `No`}`}
        onClick={() => {
          onNpcPresetConfigChange({
            ...npcPresetConfig,
            isBattleParticipant: !npcPresetConfig.isBattleParticipant,
          });
        }}
      />
      <MenuInputLabel>NPC OVERRIDE FACTION</MenuInputLabel>
      <MenuOptionInput
        values={[`None`].concat(Object.values(NpcFactionId).filter(x => typeof x === `string`) as Array<string>)}
        currentValueIndex={npcPresetConfig.overrideFactionId === null ? 0 : npcPresetConfig.overrideFactionId + 1}
        onValueChange={(valueIndex) => {
          onNpcPresetConfigChange({
            ...npcPresetConfig,
            overrideFactionId: valueIndex === 0 ? null : valueIndex - 1,
          });
        }}
      />
    </>
  );
}