import { ChevronsUpDownIcon } from "lucide-react";
import {
  forwardRef,
  HTMLAttributes,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  CharacterInfo,
  getCharacterClassName,
  useCharactersInfo,
} from "@/lib/runtime-api";
import { cn } from "@/lib/utils";

import { Badge } from "./ui/badge";
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

function SelectCharacterComboboxElement(
  {
    characterInfo,
    isCharacterClassBadgeVisible,
  }: {
    characterInfo: CharacterInfo;
    isCharacterClassBadgeVisible: boolean;
  },
) {
  return (
    <>
      {characterInfo.name}
      {
        !isCharacterClassBadgeVisible
          ? <></>
          : (
              <Badge className="ml-auto flex w-28 justify-center">
                {getCharacterClassName(characterInfo.class)}
              </Badge>
            )
      }
    </>
  );
}

type SelectCharacterComboboxProps = (
  & Omit<HTMLAttributes<HTMLDivElement>, "children">
  & {
    label: string;
    initialSelectedCharacterId?: string;
    onSelectedCharacterIdChange: (selectedCharacterId: string) => void;
  }
);

const SelectCharacterCombobox = forwardRef<HTMLDivElement, SelectCharacterComboboxProps>(
  (
    {
      className,
      label,
      initialSelectedCharacterId,
      onSelectedCharacterIdChange,
      ...props
    },
    ref,
  ) => {
    const charactersInfo = useCharactersInfo();

    const [open, setOpen] = useState(false);

    const buttonRef = useRef<HTMLButtonElement>(null);

    const buttonWidth = buttonRef.current?.offsetWidth;

    const [selectedCharacterId, setSelectedCharacterId] = useState<string | undefined>(initialSelectedCharacterId);

    useEffect(
      () => {
        if (selectedCharacterId !== undefined) {
          onSelectedCharacterIdChange(selectedCharacterId);
        }
      },
      [
        selectedCharacterId,
        onSelectedCharacterIdChange,
      ],
    );

    const selectedCharacterInfo = charactersInfo.find(characterInfo => characterInfo.id === selectedCharacterId);

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
              {
                selectedCharacterInfo === undefined
                  ? `Select ${label}...`
                  : <SelectCharacterComboboxElement characterInfo={selectedCharacterInfo} isCharacterClassBadgeVisible={false} />
              }
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
              <CommandInput placeholder="Search characater..." />
              <CommandList className="px-1 py-2">
                <CommandEmpty>No character found.</CommandEmpty>
                <CommandGroup>
                  {charactersInfo.map(characterInfo => (
                    <CommandItem
                      key={characterInfo.id}
                      value={characterInfo.id}
                      keywords={[getCharacterClassName(characterInfo.class), characterInfo.name]}
                      onSelect={() => {
                        setSelectedCharacterId(characterInfo.id);
                        setOpen(false);
                      }}
                    >
                      <SelectCharacterComboboxElement characterInfo={characterInfo} isCharacterClassBadgeVisible={true} />
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

SelectCharacterCombobox.displayName = "SelectCharacterCombobox";

export { SelectCharacterCombobox };
