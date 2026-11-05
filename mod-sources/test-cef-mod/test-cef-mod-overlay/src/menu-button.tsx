import { useEffect } from "react";
import { MenuButtonType } from "./menu-button-type";
import { useFocusable } from "@noriginmedia/norigin-spatial-navigation";

export function MenuButton(
  {
    label,
    onClick,
    type,
    isDefault,
  }: {
    label: string;
    onClick: () => void;
    type?: MenuButtonType;
    isDefault?: boolean;
  }
) {

  const { ref, focusSelf } = useFocusable();

  useEffect(() => {
    if (isDefault === true) {
      focusSelf();
    }
  }, [focusSelf, isDefault]);

  const bgClassName = (() => {
    switch (type) {
      case MenuButtonType.Approve:
        return `bg-green-400 focus:bg-green-300`;
      case MenuButtonType.Disapprove:
        return `bg-red-400 focus:bg-red-300`;
      case MenuButtonType.Normal:
      default:
        return `bg-sky-400 focus:bg-sky-300`;
    }
  })();

  return (
    <button
      ref={ref}
      className={`w-full outline-none ${bgClassName} rounded-lg p-2`}
      onClick={onClick}
    >
      <p className="text-white text-2xl">{label}</p>
    </button>
  );
}
