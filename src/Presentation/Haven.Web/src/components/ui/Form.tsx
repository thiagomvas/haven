import { FormEvent, ReactNode } from 'react'
import styles from './Form.module.css'

interface FormProps {
  onSubmit: (e: FormEvent<HTMLFormElement>) => void
  children: ReactNode
  isLoading?: boolean
}

export function Form({ onSubmit, children, isLoading = false }: FormProps) {
  return (
    <form onSubmit={onSubmit} className={styles.form}>
      <fieldset disabled={isLoading}>{children}</fieldset>
    </form>
  )
}

interface FormGroupProps {
  children: ReactNode
}

export function FormGroup({ children }: FormGroupProps) {
  return <div className={styles.group}>{children}</div>
}

interface FormLabelProps {
  htmlFor: string
  children: ReactNode
  required?: boolean
}

export function FormLabel({ htmlFor, children, required }: FormLabelProps) {
  return (
    <label htmlFor={htmlFor} className={styles.label}>
      {children}
      {required && <span className={styles.required}>*</span>}
    </label>
  )
}

interface FormInputProps
  extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string
}

export function FormInput({ error, ...props }: FormInputProps) {
  return (
    <>
      <input {...props} className={styles.input} />
      {error && <p className={styles.error}>{error}</p>}
    </>
  )
}

interface FormTextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: string
}

export function FormTextarea({ error, ...props }: FormTextareaProps) {
  return (
    <>
      <textarea {...props} className={styles.textarea} />
      {error && <p className={styles.error}>{error}</p>}
    </>
  )
}
