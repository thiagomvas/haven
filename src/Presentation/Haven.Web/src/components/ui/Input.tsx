import { clsx } from 'clsx';
import { InputHTMLAttributes } from 'react';

import styles from '@/styles/components/ui/Input.module.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export function Input({ label, error, required, className, ...props }: InputProps) {
  return (
    <div className={styles.wrapper}>
      {label && (
        <label className={styles.label} htmlFor={props.id}>
          {label}
          {required && <span className={styles.required}>*</span>}
        </label>
      )}
      <input required={required} className={clsx(styles.input, error && styles.inputError, className)} {...props} />
      {error && <p className={styles.errorMessage}>{error}</p>}
    </div>
  );
}
