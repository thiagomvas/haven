import { HTMLAttributes } from 'react'
import { clsx } from 'clsx'
import styles from './Label.module.css'

interface LabelProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: 'primary' | 'secondary' | 'muted'
  size?: 'sm' | 'md' | 'lg'
  as?: 'span' | 'p' | 'label'
}

export function Label({
  variant = 'secondary',
  size = 'md',
  as: Tag = 'span',
  className,
  children,
  ...props
}: LabelProps) {
  return (
    <Tag className={clsx(styles.label, styles[variant], styles[size], className)} {...props}>
      {children}
    </Tag>
  )
}
