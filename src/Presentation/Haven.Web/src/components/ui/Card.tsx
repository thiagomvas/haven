import { HTMLAttributes, ReactNode } from 'react'
import { clsx } from 'clsx'
import styles from './Card.module.css'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
}

export function Card({
  className,
  children,
  ...props
}: CardProps) {
  return (
    <div
      className={clsx(styles.card, className)}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardHeader({
  className,
  children,
  ...props
}: CardProps) {
  return (
    <div
      className={clsx(styles.cardHeader, className)}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardContent({
  className,
  children,
  ...props
}: CardProps) {
  return (
    <div
      className={clsx(styles.cardContent, className)}
      {...props}
    >
      {children}
    </div>
  )
}

export function CardFooter({
  className,
  children,
  ...props
}: CardProps) {
  return (
    <div
      className={clsx(styles.cardFooter, className)}
      {...props}
    >
      {children}
    </div>
  )
}
