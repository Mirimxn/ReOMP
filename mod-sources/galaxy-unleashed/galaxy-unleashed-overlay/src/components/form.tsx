import {
  forwardRef,
  HTMLAttributes,
} from "react";

import { cn } from "@/lib/utils";

export type FormProps = (
  & HTMLAttributes<HTMLDivElement>
);

const Form = forwardRef<HTMLDivElement, FormProps>(
  (
    {
      className,
      children,
      ...props
    },
    ref,
  ) => {
    return (
      <div
        ref={ref}
        className={cn(
          "flex flex-col gap-6 px-4",
          className,
        )}
        {...props}
      >
        {children}
      </div>
    );
  },
);

Form.displayName = "Form";

export { Form };
