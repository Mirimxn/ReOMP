import { useState } from "react";
import { MenuButton } from "./menu-button";
import { MenuOptionInputList } from "./menu-option-input-list";

export function MenuOptionInput(
  {
    isDefault,
    values,
    currentValueIndex,
    onValueChange,
  }: {
    isDefault?: boolean;
    values: string[];
    currentValueIndex: number;
    onValueChange: (valueIndex: number) => void;
  }
) {

  const [isExpanded, setIsExpanded] = useState<boolean>(false);
  const [shouldBeFocused, setShouldBeFocused] = useState<boolean>(false);

  if (isExpanded) {
    return (
      <MenuOptionInputList
        values={values}
        currentValueIndex={currentValueIndex}
        onValueChange={(value) => {

          onValueChange(value);
          setIsExpanded(false);
          setShouldBeFocused(true);
        }}
      />
    )
  }

  return (
    <MenuButton isDefault={shouldBeFocused || isDefault === true} label={values[currentValueIndex]} onClick={() => setIsExpanded(true)} />
  );
}