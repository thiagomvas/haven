import { HTMLAttributes } from 'react'
import { clsx } from 'clsx'
import styles from './Label.module.css'

interface LabelProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: 'primary' | 'secondary' | 'muted' | 'subtle' | 'accent' | 'success' | 'warning' | 'error' | 'info'
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl'
  weight?: 'normal' | 'medium' | 'semibold' | 'bold'
  truncate?: boolean
  as?: 'span' | 'p' | 'label'
}

export function Label({
  variant = 'secondary',
  size = 'md',
  weight = 'normal',
  truncate = false,
  as: Tag = 'span',
  className,
  children,
  ...props
}: LabelProps) {
  return (
    <Tag
      className={clsx(
        styles.label,
        styles[variant],
        styles[size],
        styles[weight],
        truncate && styles.truncate,
        className
      )}
      {...props}
    >
      {children}
    </Tag>
  )
}
