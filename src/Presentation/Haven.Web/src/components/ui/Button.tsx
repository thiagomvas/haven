import { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from 'react'
import { clsx } from 'clsx'
import styles from './Button.module.css'

type BaseProps = {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'success' | 'warning' | 'outline' | 'text'
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl'
  align?: 'left' | 'center' | 'right'
  isLoading?: boolean
  icon?: ReactNode
  children?: ReactNode
  className?: string
}

type ButtonProps = BaseProps &
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, keyof BaseProps> & { href?: undefined }

type AnchorProps = BaseProps &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, keyof BaseProps> & { href: string }

export function Button({
  variant = 'primary',
  size = 'md',
  align = 'center',
  className,
  isLoading,
  icon,
  children,
  ...props
}: ButtonProps | AnchorProps) {
  const sharedClass = clsx(
    styles.button,
    styles[variant],
    styles[size],
    styles[align],
    (isLoading || (props as ButtonProps).disabled) && styles.disabled,
    className,
  )

  const content = isLoading ? (
    <span className={styles.loadingSpinner} />
  ) : (
    <>
      {icon && <span className={clsx(styles.icon, styles[`icon-${size}`])}>{icon}</span>}
      {children}
    </>
  )

  if ((props as AnchorProps).href !== undefined) {
    const { href, ...anchorProps } = props as AnchorProps
    return (
      <a className={sharedClass} href={href} {...anchorProps}>
        {content}
      </a>
    )
  }

  const { disabled, ...buttonProps } = props as ButtonProps
  return (
    <button className={sharedClass} disabled={disabled || isLoading} {...buttonProps}>
      {content}
    </button>
  )
}
