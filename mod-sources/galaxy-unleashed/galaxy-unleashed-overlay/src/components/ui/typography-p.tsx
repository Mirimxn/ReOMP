import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyP = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => {
    return (
      <p
        ref={ref}
        className={cn(
          "leading-7 [&:not(:first-child)]:mt-6",
          className,
        )}
        {...props}
      >
        {props.children}
      </p>
    );
  },
);
TypographyP.displayName = "TypographyP";

export { TypographyP };
