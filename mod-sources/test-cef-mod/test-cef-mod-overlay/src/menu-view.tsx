import { FocusContext, useFocusable } from "@noriginmedia/norigin-spatial-navigation";

export function MenuView(
  {
    isOpened,
    children,
  }: {
    isOpened: boolean;
    children: React.ReactNode;
  },
) {

  const { ref, focusKey } = useFocusable({
    forceFocus: true,
    isFocusBoundary: true,
  });

  return (
    <FocusContext.Provider value={focusKey}>
      <div ref={ref} className={`${isOpened ? `opacity-100 right-6 pointer-events-auto` : `opacity-0 right-0 pointer-events-none`} transition-all duration-500 absolute top-6 h-auto w-96 bg-sky-500 rounded-lg flex flex-col`}>
        {children}
      </div>
    </FocusContext.Provider>
  );
}
