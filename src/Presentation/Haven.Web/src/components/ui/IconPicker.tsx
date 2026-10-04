import { useMemo, useState } from 'react';

import { Row, Stack } from '../layout';
import { Button } from './Button';
import { Input } from './Input';
import { Label } from './Label';
import { isLucideIconName, LucideIcon, searchLucideIcons } from './LucideIcon';

interface IconPickerProps {
  label: string;
  value: string;
  onChange: (name: string) => void;
  hint?: string;
}

export function IconPicker({ label, value, onChange, hint }: IconPickerProps) {
  const [query, setQuery] = useState('');
  const results = useMemo(() => searchLucideIcons(query || value), [query, value]);

  return (
    <Stack gap="2">
      <Row gap="2" align="flex-end">
        <div style={{ flex: 1 }}>
          <Input
            label={label}
            value={value}
            onChange={e => {
              setQuery(e.target.value);
              onChange(e.target.value.trim().toLowerCase());
            }}
            placeholder="rotate-cw"
          />
        </div>
        <LucideIcon name={value} size={24} />
      </Row>
      {hint && (
        <Label variant="muted" size="xs">
          {hint}
        </Label>
      )}
      <Row gap="1" wrap>
        {results.map(name => (
          <Button
            key={name}
            variant={name === value ? 'secondary' : 'text'}
            size="xs"
            icon={<LucideIcon name={name} size={16} />}
            title={name}
            aria-label={name}
            onClick={() => {
              setQuery('');
              onChange(name);
            }}
          />
        ))}
      </Row>
      {value && !isLucideIconName(value) && (
        <Label variant="error" size="xs">
          {`"${value}" is not a lucide icon`}
        </Label>
      )}
    </Stack>
  );
}
