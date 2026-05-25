import type { ReactNode } from 'react'
import styles from './EnvironmentStatusChip.module.css'

export type EnvironmentStatus = 'running' | 'partial' | 'stopped' | 'empty'

interface EnvironmentStatusChipProps {
  name: string
  status: EnvironmentStatus
}

export function EnvironmentStatusChip({
  name,
  status,
}: EnvironmentStatusChipProps) {
  return (
    <div className={`${styles.chip} ${styles[status]}`}>
      <span className={styles.indicator} />
      {name}
    </div>
  )
}
