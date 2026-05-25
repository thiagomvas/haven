import { HTMLAttributes, ReactNode } from 'react'
import { clsx } from 'clsx'
import styles from './Chip.module.css'

interface ChipProps extends HTMLAttributes<HTMLDivElement> {
  icon?: ReactNode
  content: ReactNode
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'default'
  size?: 'sm' | 'md' | 'lg'
}

export function Chip({
  icon,
  content,
  variant = 'default',
  size = 'md',
  className,
  ...props
}: ChipProps) {
  return (
    <div
      className={clsx(
        styles.chip,
        styles[variant],
        styles[size],
        className,
      )}
      {...props}
    >
      {icon && <div className={styles.icon}>{icon}</div>}
      <div className={styles.content}>{content}</div>
    </div>
  )
}
