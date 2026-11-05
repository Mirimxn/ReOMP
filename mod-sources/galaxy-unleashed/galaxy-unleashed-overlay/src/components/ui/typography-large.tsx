import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyLarge = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          "text-lg font-semibold",
          className,
        )}
        {...props}
      >
        {props.children}
      </div>
    );
  },
);
TypographyLarge.displayName = "TypographyLarge";

export { TypographyLarge };
