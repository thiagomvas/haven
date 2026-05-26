import { CSSProperties } from 'react'
import styles from './Spacer.module.css'

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12'
type Direction = 'horizontal' | 'vertical'

interface SpacerProps {
  size?: SpacingValue
  direction?: Direction
  expand?: boolean
}

export function Spacer({ size = '4', direction = 'vertical', expand = false }: SpacerProps) {
  const style: CSSProperties = {}

  if (!expand) {
    if (direction === 'horizontal') {
      style.width = `var(--space-${size})`
      style.color = 'red'
    } else {
      style.height = `var(--space-${size})`
    }
  }

  return <div style={style} className={`${styles.spacer} ${expand ? styles.expand : ''}`} > </div>
}
