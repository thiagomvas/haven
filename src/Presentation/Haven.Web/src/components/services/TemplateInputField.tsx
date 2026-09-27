import { Lock } from 'lucide-react';
import { useState } from 'react';

import { TemplateInputFieldDto } from '@/api/types';

import { FormGroup, FormInput, FormLabel } from '../ui/Form';
import { SelectInput } from '../ui/SelectInput';

interface TemplateInputFieldProps {
  field: TemplateInputFieldDto;
  onChange: (value: string) => void;
  disabled?: boolean;
}

export function TemplateInputField({ field, onChange, disabled }: TemplateInputFieldProps) {
  const [value, setValue] = useState(field.defaultValue ?? '');
  const label = field.label ?? field.key;
  const required = !field.defaultValue;

  const handleChange = (next: string) => {
    setValue(next);
    onChange(next);
  };

  return (
    <FormGroup>
      <FormLabel htmlFor={field.key} required={required}>
        {label}
        {field.immutable && (
          <span
            title="This value cannot be changed after the service is created."
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              marginLeft: 'var(--space-1)',
              color: 'var(--color-text-muted)',
            }}
          >
            <Lock size={12} />
          </span>
        )}
      </FormLabel>

      {field.type === 'Select' ? (
        <SelectInput
          value={value}
          onChange={handleChange}
          options={(field.options ?? []).map(o => ({ value: o, label: o }))}
          disabled={disabled}
        />
      ) : (
        <FormInput
          id={field.key}
          type={field.type === 'Secret' ? 'password' : 'text'}
          value={value}
          onChange={e => handleChange(e.target.value)}
          disabled={disabled}
        />
      )}
    </FormGroup>
  );
}
