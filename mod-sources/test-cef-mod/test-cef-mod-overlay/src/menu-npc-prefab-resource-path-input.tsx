import { useContext, useState } from "react";
import { MenuButton } from "./menu-button";
import { CharacterClass } from "./bindings";
import { StateContext } from "./state-context";
import { MenuOptionInputList } from "./menu-option-input-list";

export function MenuNpcPrefabResourcePathInput(
  {
    isDefault,
    value,
    onValueChange,
  }: {
    isDefault?: boolean;
    value: string;
    onValueChange: (value: string) => void;
  }
) {
  const { charactersInfo } = useContext(StateContext);

  const [shouldBeFocused, setShouldBeFocused] = useState<boolean>(false);
  const [isChanging, setIsChanging] = useState<boolean>(false);
  const [characterClass, setCharacterClass] = useState<CharacterClass | null>(null);

  if (charactersInfo === null) {
    return;
  }

  if (isChanging) {
    if (characterClass === null) {
      return (
        <MenuOptionInputList
          values={Object.values(CharacterClass).filter(x => typeof x === `string`) as Array<string>}
          currentValueIndex={0}
          onValueChange={(valueIndex) => {
            setCharacterClass(valueIndex);
          }}
        />
      );
    }

    const charactersWithSelectedClassInfo = charactersInfo.filter(characterInfo => characterInfo.class === characterClass);

    if (characterClass !== null) {
      return (
        <MenuOptionInputList
          values={charactersWithSelectedClassInfo.map(characterInfo => characterInfo.nameStringId)}
          currentValueIndex={0}
          onValueChange={(valueIndex) => {
            setCharacterClass(null);
            setIsChanging(false);
            setShouldBeFocused(true);
            onValueChange(charactersWithSelectedClassInfo[valueIndex].prefabResourcePath);
          }}
        />
      );
    }
  }

  return (
    <MenuButton
      isDefault={shouldBeFocused || isDefault === true}
      label={charactersInfo.find(characterInfo => characterInfo.prefabResourcePath === value)?.nameStringId ?? `None`}
      onClick={() => setIsChanging(true)}
    />
  );
}