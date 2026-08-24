import { ChevronDown } from 'lucide-react';
import { useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import { useFloatingPosition } from '@/hooks/useFloatingPosition';
import styles from '@/styles/components/ui/SelectInput.module.css';

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectInputProps {
  label?: string;
  options: SelectOption[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function SelectInput({
  label,
  options,
  value,
  onChange,
  placeholder,
  disabled,
}: SelectInputProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const position = useFloatingPosition(open, triggerRef);

  const selected = options.find(o => o.value === value);
  const displayText = selected?.label ?? placeholder ?? 'Select…';

  return (
    <div className={styles.wrapper}>
      {label && <label className={styles.label}>{label}</label>}
      <div className={styles.triggerWrapper}>
        <button
          ref={triggerRef}
          type="button"
          className={styles.trigger}
          onClick={() => setOpen(o => !o)}
          onBlur={() => setTimeout(() => setOpen(false), 100)}
          disabled={disabled}
        >
          <span className={selected ? '' : styles.placeholder}>{displayText}</span>
          <ChevronDown
            size={15}
            className={`${styles.chevron} ${open ? styles.chevronOpen : ''}`}
          />
        </button>

        {open &&
          position &&
          createPortal(
            <ul
              className={styles.dropdown}
              style={{
                top: position.top,
                left: position.left,
                width: position.width,
                maxHeight: position.maxHeight,
              }}
            >
              {placeholder && (
                <li>
                  <button
                    type="button"
                    className={`${styles.option} ${!value ? styles.optionActive : ''}`}
                    onMouseDown={e => {
                      e.preventDefault();
                      onChange('');
                      setOpen(false);
                      triggerRef.current?.focus();
                    }}
                  >
                    {placeholder}
                  </button>
                </li>
              )}
              {options.map(opt => (
                <li key={opt.value}>
                  <button
                    type="button"
                    className={`${styles.option} ${opt.value === value ? styles.optionActive : ''}`}
                    onMouseDown={e => {
                      e.preventDefault();
                      onChange(opt.value);
                      setOpen(false);
                      triggerRef.current?.focus();
                    }}
                  >
                    {opt.label}
                  </button>
                </li>
              ))}
            </ul>,
            document.body
          )}
      </div>
    </div>
  );
}
