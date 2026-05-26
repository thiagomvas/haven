import { ReactNode, CSSProperties } from 'react'
import styles from './Grid.module.css'

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12'

interface GridProps {
  children: ReactNode
  columns?: number | string
  gap?: SpacingValue
  className?: string
}

export function Grid({ children, columns = 'auto-fill', gap = '4', className = '' }: GridProps) {
  const gridStyle: CSSProperties = {}

  if (typeof columns === 'number') {
    gridStyle.gridTemplateColumns = `repeat(${columns}, 1fr)`
  } else {
    gridStyle.gridTemplateColumns = `repeat(${columns}, minmax(280px, 1fr))`
  }

  return (
    <div style={gridStyle} className={`${styles.grid} ${styles[`gap-${gap}`]} ${className}`}>
      {children}
    </div>
  )
}
