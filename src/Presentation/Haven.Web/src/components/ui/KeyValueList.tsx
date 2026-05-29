import { ReactNode } from 'react'
import styles from './KeyValueList.module.css'

interface KeyValueListProps {
  children: ReactNode
  className?: string
}

interface KeyValueRowProps {
  label: string
  children: ReactNode
}

export function KeyValueList({ children, className = '' }: KeyValueListProps) {
  return <div className={`${styles.list} ${className}`}>{children}</div>
}

export function KeyValueRow({ label, children }: KeyValueRowProps) {
  return (
    <div className={styles.row}>
      <span className={styles.key}>{label}</span>
      <span className={styles.value}>{children}</span>
    </div>
  )
}
