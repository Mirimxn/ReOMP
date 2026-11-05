import { forwardRef } from "react";

import {
  FormField,
  FormFieldProps,
} from "./form-field";
import { Slider } from "./ui/slider";
import { TypographySmall } from "./ui/typography-small";

type FormSliderFieldProps = (
  & FormFieldProps
  & {
    min: number;
    max: number;
    value: number;
    onValueChange: (value: number) => void;
    formatValue: (value: number) => string;
  }
);

const FormSliderField = forwardRef<HTMLDivElement, FormSliderFieldProps>(
  (
    {
      min,
      max,
      value,
      onValueChange,
      formatValue,
      ...props
    },
    ref,
  ) => {
    return (
      <FormField
        ref={ref}
        {...props}
      >
        <div className="flex flex-col items-center gap-2">
          <Slider
            min={min}
            max={max}
            value={[value]}
            onValueChange={([newValue]) => { onValueChange(newValue ?? min); }}
          />
          <TypographySmall>{formatValue(value)}</TypographySmall>
        </div>
      </FormField>
    );
  },
);

FormSliderField.displayName = "FormSliderField";

export { FormSliderField };
