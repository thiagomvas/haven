import { ButtonHTMLAttributes, ReactNode } from 'react'
import { clsx } from 'clsx'
import styles from './Button.module.css'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'success' | 'warning' | 'outline' | 'text'
  size?: 'sm' | 'md' | 'lg'
  align?: 'left' | 'center' | 'right'
  isLoading?: boolean
  disabled?: boolean
  icon?: ReactNode
}

export function Button({
  variant = 'primary',
  size = 'md',
  align = 'center',
  className,
  isLoading,
  disabled,
  icon,
  children,
  ...props
}: ButtonProps) {
  const isDisabled = disabled || isLoading

  return (
    <button
      className={clsx(
        styles.button,
        styles[variant],
        styles[size],
        styles[align],
        isDisabled && styles.disabled,
        className,
      )}
      disabled={isDisabled}
      {...props}
    >
      {isLoading ? (
        <span className={styles.loadingSpinner} />
      ) : (
        <>
          {icon && <span className={styles.icon}>{icon}</span>}
          {children}
        </>
      )}
    </button>
  )
}
