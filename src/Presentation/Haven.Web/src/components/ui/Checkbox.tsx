import { InputHTMLAttributes, useId } from 'react'
import styles from './Checkbox.module.css'

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string
  description?: string
}

export function Checkbox({ label, description, id: idProp, className, ...props }: CheckboxProps) {
  const generatedId = useId()
  const id = idProp ?? generatedId

  return (
    <label className={styles.wrapper} htmlFor={id}>
      <input {...props} id={id} type="checkbox" className={styles.input} />
      <span className={styles.box} aria-hidden="true">
        <svg className={styles.checkmark} viewBox="0 0 10 10">
          <polyline points="1.5,5 4,7.5 8.5,2.5" />
        </svg>
      </span>
      <span className={styles.text}>
        <span className={styles.label}>{label}</span>
        {description && <span className={styles.description}>{description}</span>}
      </span>
    </label>
  )
}
