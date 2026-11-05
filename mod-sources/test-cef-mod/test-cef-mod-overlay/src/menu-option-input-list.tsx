import { FocusContext, useFocusable } from "@noriginmedia/norigin-spatial-navigation";
import { useEffect } from "react";
import { MenuButton } from "./menu-button";

export function MenuOptionInputList(
  {
    values,
    currentValueIndex,
    onValueChange,
  }: {
    values: string[];
    currentValueIndex: number;
    onValueChange: (valueIndex: number) => void;
  }
) {
  const { ref, focusKey, focusSelf } = useFocusable({
    isFocusBoundary: true,
  });

  useEffect(() => {
    focusSelf();
  }, [focusSelf])

  return (
    <FocusContext.Provider value={focusKey}>
      <div
        ref={ref}
        className="w-full h-48 bg-sky-400 rounded-lg flex flex-col gap-4 justify-start items-start p-2 overflow-x-hidden overflow-y-scroll"
      >
        {
          values.map(
            (value, valueIndex) => (
              <MenuButton
                isDefault={valueIndex === currentValueIndex}
                label={value}
                onClick={() => {
                  onValueChange(valueIndex)
                }}
              />
            )
          )
        }
      </div>
    </FocusContext.Provider>
  );
}