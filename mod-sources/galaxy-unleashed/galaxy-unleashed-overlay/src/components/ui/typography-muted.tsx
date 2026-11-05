import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyMuted = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => {
    return (
      <p
        ref={ref}
        className={cn(
          "text-sm text-muted-foreground",
          className,
        )}
        {...props}
      >
        {props.children}
      </p>
    );
  },
);
TypographyMuted.displayName = "TypographyMuted";

export { TypographyMuted };
