import { FormEvent, ReactNode } from 'react';
import { ErrorAlert } from './ErrorAlert';
import styles from './Form.module.css';

interface FormProps {
  onSubmit: (e: FormEvent<HTMLFormElement>) => void;
  children: ReactNode;
  isLoading?: boolean;
}

export function Form({ onSubmit, children, isLoading = false }: FormProps) {
  return (
    <form onSubmit={onSubmit} className={styles.form}>
      <fieldset disabled={isLoading}>{children}</fieldset>
    </form>
  );
}

interface FormGroupProps {
  children: ReactNode;
}

export function FormGroup({ children }: FormGroupProps) {
  return <div className={styles.group}>{children}</div>;
}

interface FormLabelProps {
  htmlFor?: string;
  children: ReactNode;
  required?: boolean;
  optional?: boolean;
}

export function FormLabel({ htmlFor, children, required, optional }: FormLabelProps) {
  return (
    <label htmlFor={htmlFor} className={styles.label}>
      {children}
      {required && <span className={styles.required}>*</span>}
      {(optional || !required) && <span className={styles.optional}>Optional</span>}
    </label>
  );
}

interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string;
  fieldName?: string;
  fieldErrors?: Record<string, string | undefined>;
}

export function FormInput({ error, fieldName, fieldErrors, ...props }: FormInputProps) {
  const displayError = error || (fieldName && fieldErrors?.[fieldName]);

  return (
    <>
      <input {...props} className={`${styles.input} ${displayError ? styles.inputError : ''}`} />
      {displayError && <ErrorAlert message={displayError} variant="inline" />}
    </>
  );
}

interface FormTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: string;
  fieldName?: string;
  fieldErrors?: Record<string, string | undefined>;
}

export function FormTextarea({ error, fieldName, fieldErrors, ...props }: FormTextareaProps) {
  const displayError = error || (fieldName && fieldErrors?.[fieldName]);

  return (
    <>
      <textarea
        {...props}
        className={`${styles.textarea} ${props.disabled ? styles.disabled : ''} ${displayError ? styles.inputError : ''}`}
      />
      {displayError && <ErrorAlert message={displayError} variant="inline" />}
    </>
  );
}

interface FormSelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  error?: string;
  fieldName?: string;
  fieldErrors?: Record<string, string | undefined>;
  children: ReactNode;
}

export function FormSelect({ error, fieldName, fieldErrors, children, ...props }: FormSelectProps) {
  const displayError = error || (fieldName && fieldErrors?.[fieldName]);

  return (
    <>
      <select
        {...props}
        className={`${styles.input} ${props.disabled ? styles.disabled : ''} ${displayError ? styles.inputError : ''}`}
      >
        {children}
      </select>
      {displayError && <ErrorAlert message={displayError} variant="inline" />}
    </>
  );
}
