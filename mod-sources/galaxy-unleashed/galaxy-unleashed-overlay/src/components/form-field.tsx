import {
  forwardRef,
  HTMLAttributes,
} from "react";

import { cn } from "@/lib/utils";

import { TypographyLarge } from "./ui/typography-large";
import { TypographyMuted } from "./ui/typography-muted";

export type FormFieldProps = (
  & HTMLAttributes<HTMLDivElement>
  & {
    label: string;
    description?: string;
  }
);

const FormField = forwardRef<HTMLDivElement, FormFieldProps>(
  (
    {
      className,
      label,
      description,
      children,
      ...props
    },
    ref,
  ) => {
    return (
      <div
        ref={ref}
        className={cn(
          "grid grid-cols-[300px_1fr] gap-24 items-center justify-between rounded-lg border p-4",
          className,
        )}
        {...props}
      >
        <div className="flex flex-col gap-0.5">
          <TypographyLarge>{label}</TypographyLarge>
          <TypographyMuted>{description}</TypographyMuted>
        </div>
        {children}
      </div>
    );
  },
);

FormField.displayName = "FormField";

export { FormField };
