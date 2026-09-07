import { GitBranch, Loader2 } from 'lucide-react';
import { InputHTMLAttributes, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import { useFloatingPosition } from '@/hooks/useFloatingPosition';
import styles from '@/styles/components/ui/BranchInput.module.css';

interface BranchInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  branches: string[];
  isLoadingBranches?: boolean;
}

export function BranchInput({
  label,
  value,
  onChange,
  branches,
  isLoadingBranches,
  disabled,
  placeholder,
  ...props
}: BranchInputProps) {
  const [open, setOpen] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const position = useFloatingPosition(open, inputRef);

  const filtered = value.trim()
    ? branches.filter(b => b.toLowerCase().includes(value.toLowerCase()))
    : branches;

  return (
    <div className={styles.wrapper}>
      {label && <label className={styles.label}>{label}</label>}
      <div className={styles.inputWrapper}>
        <input
          ref={inputRef}
          className={styles.input}
          value={value}
          onChange={e => {
            onChange(e.target.value);
            setOpen(true);
          }}
          onFocus={() => {
            if (branches.length > 0) setOpen(true);
          }}
          onBlur={() => setTimeout(() => setOpen(false), 100)}
          placeholder={placeholder ?? 'e.g., main, develop'}
          disabled={disabled}
          autoComplete="off"
          {...props}
        />
        <span className={styles.icon}>
          {isLoadingBranches ? (
            <Loader2 size={15} className={styles.spinner} />
          ) : (
            <GitBranch size={15} />
          )}
        </span>
      </div>

      {open &&
        filtered.length > 0 &&
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
            {filtered.map(b => (
              <li key={b}>
                <button
                  type="button"
                  className={`${styles.option} ${b === value ? styles.optionActive : ''}`}
                  onMouseDown={e => {
                    e.preventDefault();
                    onChange(b);
                    setOpen(false);
                    inputRef.current?.focus();
                  }}
                >
                  <GitBranch size={13} className={styles.optionIcon} />
                  {b}
                </button>
              </li>
            ))}
          </ul>,
          document.body
        )}
    </div>
  );
}
