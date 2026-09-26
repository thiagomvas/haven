import { clsx } from 'clsx';

import styles from '@/styles/components/ui/FilterPillGroup.module.css';

export interface FilterPillOption {
  value: string;
  label: string;
}

interface FilterPillGroupProps {
  options: FilterPillOption[];
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

/** Single-select row of pills, e.g. for filtering a list by category. */
export function FilterPillGroup({ options, value, onChange, disabled }: FilterPillGroupProps) {
  return (
    <div className={styles.group} role="group">
      {options.map(option => (
        <button
          key={option.value}
          type="button"
          className={clsx(styles.pill, { [styles.active]: value === option.value })}
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          disabled={disabled}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
