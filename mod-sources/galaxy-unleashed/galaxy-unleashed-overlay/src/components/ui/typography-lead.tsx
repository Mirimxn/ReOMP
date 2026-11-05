import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyLead = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => {
    return (
      <p
        ref={ref}
        className={cn(
          "text-xl text-muted-foreground",
          className,
        )}
        {...props}
      >
        {props.children}
      </p>
    );
  },
);
TypographyLead.displayName = "TypographyLead";

export { TypographyLead };
