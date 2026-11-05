import {
  cva,
  type VariantProps,
} from "class-variance-authority";
import { LoaderCircleIcon } from "lucide-react";
import * as React from "react";

const spinnerVariant = cva(
  "animate-spin",
  {
    variants: {
      variant: {
        default:
          "size-5",
        sm:
          "size-4",
        xs:
          "size-3",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

import { cn } from "@/lib/utils";
const Spinner = React.forwardRef<SVGSVGElement, React.HTMLAttributes<SVGSVGElement> & VariantProps<typeof spinnerVariant>>(
  ({ className, variant, ...props }, ref) => {
    return (
      <LoaderCircleIcon
        ref={ref}
        className={cn(
          spinnerVariant({ variant }),
          className,
        )}
        {...props}
      >
        {props.children}
      </LoaderCircleIcon>
    );
  },
);
Spinner.displayName = "Spinner";

export { Spinner };
