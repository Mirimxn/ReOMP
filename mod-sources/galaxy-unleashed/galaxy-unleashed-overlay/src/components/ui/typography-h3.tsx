import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyH3 = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => {
    return (
      <h3
        ref={ref}
        className={cn(
          "scroll-m-20 text-2xl font-semibold tracking-tight",
          className,
        )}
        {...props}
      >
        {props.children}
      </h3>
    );
  },
);
TypographyH3.displayName = "TypographyH3";

export { TypographyH3 };
