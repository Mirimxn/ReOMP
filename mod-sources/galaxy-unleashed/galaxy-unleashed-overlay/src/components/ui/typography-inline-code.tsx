import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyInlineCode = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          "relative rounded bg-muted px-[0.3rem] py-[0.2rem] font-mono text-sm font-semibold",
          className,
        )}
        {...props}
      >
        {props.children}
      </div>
    );
  },
);
TypographyInlineCode.displayName = "TypographyInlineCode";

export { TypographyInlineCode };
