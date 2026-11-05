import * as React from "react";

import { cn } from "@/lib/utils";

const TypographySmall = React.forwardRef<HTMLElement, React.HTMLAttributes<HTMLElement>>(
  ({ className, ...props }, ref) => {
    return (
      <small
        ref={ref}
        className={cn(
          "text-sm font-medium leading-none",
          className,
        )}
        {...props}
      >
        {props.children}
      </small>
    );
  },
);
TypographySmall.displayName = "TypographySmall";

export { TypographySmall };
