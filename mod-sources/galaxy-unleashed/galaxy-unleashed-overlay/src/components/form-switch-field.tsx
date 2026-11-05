import { forwardRef } from "react";

import {
  FormField,
  FormFieldProps,
} from "./form-field";
import { Switch } from "./ui/switch";

type FormSwitchFieldProps = (
  & FormFieldProps
  & {
    checked: boolean;
    onCheckedChange: (checked: boolean) => void;
  }
);

const FormSwitchField = forwardRef<HTMLDivElement, FormSwitchFieldProps>(
  (
    {
      checked,
      onCheckedChange,
      ...props
    },
    ref,
  ) => {
    return (
      <FormField
        ref={ref}
        {...props}
      >
        <div className="flex flex-row justify-end">
          <Switch
            checked={checked}
            onCheckedChange={onCheckedChange}
          />
        </div>
      </FormField>
    );
  },
);

FormSwitchField.displayName = "FormSwitchField";

export { FormSwitchField };
