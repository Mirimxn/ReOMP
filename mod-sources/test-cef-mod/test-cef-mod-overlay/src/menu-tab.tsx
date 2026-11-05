export function MenuTab(
  {
    title,
    children,
  }: {
    title: string;
    children: React.ReactNode;
  }
) {
  return (
    <>
      <p className="text-center text-white text-4xl">{title}</p>
      <div className="p-4 flex flex-col gap-4 justify-start items-start">
        {children}
      </div>
    </>
  );
}