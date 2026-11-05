import * as React from "react";

import { cn } from "@/lib/utils";

const TypographyBlockquote = React.forwardRef<HTMLQuoteElement, React.HTMLAttributes<HTMLQuoteElement>>(
  ({ className, ...props }, ref) => {
    return (
      <blockquote
        ref={ref}
        className={cn(
          "mt-6 border-l-2 pl-6 italic",
          className,
        )}
        {...props}
      >
        {props.children}
      </blockquote>
    );
  },
);
TypographyBlockquote.displayName = "TypographyBlockquote";

export { TypographyBlockquote };
