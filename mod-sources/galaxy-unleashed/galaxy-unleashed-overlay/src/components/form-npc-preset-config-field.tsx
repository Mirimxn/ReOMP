import {
  forwardRef,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  CharacterFactionId,
  NpcPresetConfig,
} from "@/lib/runtime-api";

import {
  FormField,
  FormFieldProps,
} from "./form-field";
import { SelectCharacterCombobox } from "./select-character-combobox";
import { SelectCharacterOverrideFactionCombobox } from "./select-character-override-faction-combobox";

type FormNpcPresetConfigFieldProps = (
  & FormFieldProps
  & {
    npcPresetInitialConfig?: NpcPresetConfig;
    onNpcPresetConfigChange: (npcPresetConfig: NpcPresetConfig) => void;
  }
);

const FormNpcPresetConfigField = forwardRef<HTMLDivElement, FormNpcPresetConfigFieldProps>(
  (
    {
      npcPresetInitialConfig,
      onNpcPresetConfigChange,
      ...props
    },
    ref,
  ) => {
    const [characterId, setCharacterId] = useState<string | undefined>(npcPresetInitialConfig?.characterId);
    const [characterOverrideFactionId, setCharacterOverrideFactionId] = useState<CharacterFactionId | null>(npcPresetInitialConfig?.characterOverrideFactionId ?? null);

    const npcPresetConfig = useMemo<NpcPresetConfig | undefined>(
      () => {
        if (characterId === undefined) {
          return undefined;
        }

        return {
          characterId,
          characterOverrideFactionId,
        };
      },
      [
        characterId,
        characterOverrideFactionId,
      ],
    );

    useEffect(
      () => {
        if (npcPresetConfig !== undefined) {
          onNpcPresetConfigChange(npcPresetConfig);
        }
      },
      [
        npcPresetConfig,
        onNpcPresetConfigChange,
      ],
    );

    return (
      <FormField
        ref={ref}
        {...props}
      >
        <div className="grid grid-rows-[auto_auto] items-center gap-2">
          <SelectCharacterCombobox
            label="Character"
            initialSelectedCharacterId={characterId}
            onSelectedCharacterIdChange={setCharacterId}
          />
          <SelectCharacterOverrideFactionCombobox
            label="Override Faction"
            characterInitialSelectedOverrideFactionId={characterOverrideFactionId}
            onCharacterSelectedOverrideFactionIdChange={setCharacterOverrideFactionId}
          />
        </div>
      </FormField>
    );
  },
);

FormNpcPresetConfigField.displayName = "FormNpcPresetConfigField";

export { FormNpcPresetConfigField };
