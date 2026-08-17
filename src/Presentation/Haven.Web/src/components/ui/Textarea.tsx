import { clsx } from 'clsx';
import { TextareaHTMLAttributes } from 'react';

import styles from '@/styles/components/ui/Input.module.css';

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
}

export function Textarea({ label, error, className, ...props }: TextareaProps) {
  return (
    <div className={styles.wrapper}>
      {label && (
        <label className={styles.label} htmlFor={props.id}>
          {label}
        </label>
      )}
      <textarea className={clsx(styles.input, error && styles.inputError, className)} {...props} />
      {error && <p className={styles.errorMessage}>{error}</p>}
    </div>
  );
}
