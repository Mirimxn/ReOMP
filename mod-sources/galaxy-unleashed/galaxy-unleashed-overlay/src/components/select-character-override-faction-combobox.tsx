import { ChevronsUpDownIcon } from "lucide-react";
import {
  forwardRef,
  HTMLAttributes,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  CHARACTER_FACTIONS_ID,
  CharacterFactionId,
  getCharacterFactionName,
} from "@/lib/runtime-api";
import { cn } from "@/lib/utils";

import { Button } from "./ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "./ui/command";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "./ui/popover";

type SelectCharacterOverrideFactionComboboxProps = (
  & Omit<HTMLAttributes<HTMLDivElement>, "children">
  & {
    label: string;
    characterInitialSelectedOverrideFactionId: CharacterFactionId | null;
    onCharacterSelectedOverrideFactionIdChange: (characterSelectedOverrideFactionId: CharacterFactionId | null) => void;
  }
);

function SelectCharacterOverrideFactionComboboxElement(
  {
    label,
    characterOverrideFactionId,
  }: {
    label?: string;
    characterOverrideFactionId: CharacterFactionId | null;
  },
) {
  return (
    <>
      {label === undefined ? <></> : `${label}: `}
      {characterOverrideFactionId === null ? `None` : getCharacterFactionName(characterOverrideFactionId)}
    </>
  );
}

const SelectCharacterOverrideFactionCombobox = forwardRef<HTMLDivElement, SelectCharacterOverrideFactionComboboxProps>(
  (
    {
      className,
      label,
      characterInitialSelectedOverrideFactionId,
      onCharacterSelectedOverrideFactionIdChange,
      ...props
    },
    ref,
  ) => {
    const [open, setOpen] = useState(false);

    const buttonRef = useRef<HTMLButtonElement>(null);

    const buttonWidth = buttonRef.current?.offsetWidth;

    const [characterSelectedOverrideFactionId, setCharacterSelectedOverrideFactionId] = useState<CharacterFactionId | null>(characterInitialSelectedOverrideFactionId);

    useEffect(
      () => {
        onCharacterSelectedOverrideFactionIdChange(characterSelectedOverrideFactionId);
      },
      [
        characterSelectedOverrideFactionId,
        onCharacterSelectedOverrideFactionIdChange,
      ],
    );

    return (
      <div
        ref={ref}
        className={cn(
          className,
        )}
        {...props}
      >
        <Popover open={open} onOpenChange={setOpen}>
          <PopoverTrigger asChild>
            <Button
              ref={buttonRef}
              variant="outline"
              role="combobox"
              aria-expanded={open}
              className="w-full justify-between"
            >
              <SelectCharacterOverrideFactionComboboxElement
                label={label}
                characterOverrideFactionId={characterSelectedOverrideFactionId}
              />
              <ChevronsUpDownIcon className="opacity-50" />
            </Button>
          </PopoverTrigger>
          <PopoverContent
            className="p-0"
            style={{
              width: buttonWidth,
            }}
          >
            <Command>
              <CommandInput placeholder="Search characater faction..." />
              <CommandList className="px-1 py-2">
                <CommandEmpty>No character faction found.</CommandEmpty>
                <CommandGroup>
                  <CommandItem
                    key="none"
                    keywords={[`None`]}
                    onSelect={() => {
                      setCharacterSelectedOverrideFactionId(null);
                      setOpen(false);
                    }}
                  >
                    None
                  </CommandItem>
                  {CHARACTER_FACTIONS_ID.map(characterFactionId => (
                    <CommandItem
                      key={characterFactionId}
                      keywords={[getCharacterFactionName(characterFactionId)]}
                      onSelect={() => {
                        setCharacterSelectedOverrideFactionId(characterFactionId);
                        setOpen(false);
                      }}
                    >
                      <SelectCharacterOverrideFactionComboboxElement
                        characterOverrideFactionId={characterFactionId}
                      />
                    </CommandItem>
                  ))}
                </CommandGroup>
              </CommandList>
            </Command>
          </PopoverContent>
        </Popover>
      </div>
    );
  },
);

SelectCharacterOverrideFactionCombobox.displayName = "SelectCharacterOverrideFactionCombobox";

export { SelectCharacterOverrideFactionCombobox };
