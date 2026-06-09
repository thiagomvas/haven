import { ReactNode } from 'react';
import styles from './DetailsPageForm.module.css';

interface FormFieldProps {
  label: string;
  error?: string;
  children: ReactNode;
}

export function FormField({ label, error, children }: FormFieldProps) {
  return (
    <div className={styles.formGroup}>
      <label className={styles.formLabel}>{label}</label>
      {children}
      {error && <span className={styles.fieldError}>{error}</span>}
    </div>
  );
}

interface SettingsFormContainerProps {
  title: string;
  children: ReactNode;
  layout?: 'grid' | 'flex';
}

export function SettingsFormContainer({
  title,
  children,
  layout = 'flex',
}: SettingsFormContainerProps) {
  return (
    <div className={styles.settingsSection}>
      <h3 className={styles.sectionTitle}>{title}</h3>
      <div className={layout === 'grid' ? styles.settingsFormGrid : styles.settingsFormFlex}>
        {children}
      </div>
    </div>
  );
}

interface TextInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  helperText?: string;
}

export function TextInput({ label, error, helperText, ...props }: TextInputProps) {
  return (
    <FormField label={label} error={error}>
      <input type="text" className={styles.formInput} {...props} />
      {helperText && <span className={styles.helperText}>{helperText}</span>}
    </FormField>
  );
}

interface TextAreaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string;
  error?: string;
  characterLimit?: number;
  helperText?: string;
}

export function TextArea({
  label,
  error,
  characterLimit,
  helperText,
  value,
  ...props
}: TextAreaProps) {
  const charCount = typeof value === 'string' ? value.length : 0;

  return (
    <FormField label={label} error={error}>
      <textarea className={styles.formInput} value={value} {...props} />
      <div className={styles.formFooter}>
        {characterLimit && (
          <span className={styles.charCount}>
            {charCount}/{characterLimit}
          </span>
        )}
        {helperText && <span className={styles.helperText}>{helperText}</span>}
      </div>
    </FormField>
  );
}

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  error?: string;
  options: Array<{ value: string; label: string }>;
  helperText?: string;
}

export function Select({ label, error, options, helperText, ...props }: SelectProps) {
  return (
    <FormField label={label} error={error}>
      <select className={styles.formInput} {...props}>
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
      {helperText && <span className={styles.helperText}>{helperText}</span>}
    </FormField>
  );
}
