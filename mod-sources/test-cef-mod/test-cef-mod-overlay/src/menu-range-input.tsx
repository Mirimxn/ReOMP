import { useFocusable } from "@noriginmedia/norigin-spatial-navigation";

export function MenuRangeInput(
  {
    label,
    value,
    minValue,
    maxValue,
    onValueChange,
  }: {
    label: string;
    value: number;
    minValue: number;
    maxValue: number;
    onValueChange: (value: number) => void;
  }
) {

  const { ref: decreaseButtonRef } = useFocusable();
  const { ref: increaseButtonRef } = useFocusable();

  return (
    <div className="w-full bg-sky-400 flex flex-row justify-between items-center rounded-lg">
      <button
        ref={decreaseButtonRef}
        className={`w-12 outline-none bg-sky-400 focus:bg-sky-300 rounded-lg p-2`}
        onClick={() => onValueChange(Math.max(value - 1, minValue))}
      >
        <p className="text-white text-2xl text-center">-</p>
      </button>
      <p className="grow text-white text-2xl text-center">{label}</p>
      <button
        ref={increaseButtonRef}
        className={`w-12 outline-none bg-sky-400 focus:bg-sky-300 rounded-lg p-2`}
        onClick={() => onValueChange(Math.min(value + 1, maxValue))}
      >
        <p className="text-white text-2xl text-center">+</p>
      </button>
    </div>
  );
}