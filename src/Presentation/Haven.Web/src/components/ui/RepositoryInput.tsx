import { FolderGit2, Loader2 } from 'lucide-react';
import { InputHTMLAttributes, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import { GitRepositorySummaryDto } from '@/api/types';
import { useFloatingPosition } from '@/hooks/useFloatingPosition';
import styles from '@/styles/components/ui/RepositoryInput.module.css';

interface RepositoryInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  repositories: GitRepositorySummaryDto[];
  isLoadingRepositories?: boolean;
}

export function RepositoryInput({
  label,
  value,
  onChange,
  repositories,
  isLoadingRepositories,
  disabled,
  placeholder,
  ...props
}: RepositoryInputProps) {
  const [open, setOpen] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const position = useFloatingPosition(open, inputRef);

  const filtered = value.trim()
    ? repositories.filter(
        r =>
          r.fullName.toLowerCase().includes(value.toLowerCase()) ||
          r.cloneUrl.toLowerCase().includes(value.toLowerCase())
      )
    : repositories;

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
            if (repositories.length > 0) setOpen(true);
          }}
          onBlur={() => setTimeout(() => setOpen(false), 100)}
          placeholder={placeholder}
          disabled={disabled}
          autoComplete="off"
          {...props}
        />
        <span className={styles.icon}>
          {isLoadingRepositories ? (
            <Loader2 size={15} className={styles.spinner} />
          ) : (
            <FolderGit2 size={15} />
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
            {filtered.map(r => (
              <li key={r.fullName}>
                <button
                  type="button"
                  className={`${styles.option} ${r.cloneUrl === value ? styles.optionActive : ''}`}
                  onMouseDown={e => {
                    e.preventDefault();
                    onChange(r.cloneUrl);
                    setOpen(false);
                    inputRef.current?.focus();
                  }}
                >
                  <FolderGit2 size={13} className={styles.optionIcon} />
                  <span className={styles.optionText}>
                    <span className={styles.optionName}>{r.fullName}</span>
                  </span>
                </button>
              </li>
            ))}
          </ul>,
          document.body
        )}
    </div>
  );
}
