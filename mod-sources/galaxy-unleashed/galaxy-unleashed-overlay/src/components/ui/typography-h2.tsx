import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyH2 = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => {
    return (
      <h2
        ref={ref}
        className={cn(
          "scroll-m-20 border-b pb-2 text-3xl font-semibold tracking-tight first:mt-0",
          className,
        )}
        {...props}
      >
        {props.children}
      </h2>
    );
  },
);
TypographyH2.displayName = "TypographyH2";

export { TypographyH2 };
