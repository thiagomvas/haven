import { HTMLAttributes } from 'react'
import { clsx } from 'clsx'
import styles from './Divider.module.css'

type DividerStyle = 'solid' | 'dotted' | 'dashed' | 'double'
type DividerOrientation = 'horizontal' | 'vertical'

interface DividerProps extends HTMLAttributes<HTMLDivElement> {
  orientation?: DividerOrientation
  variant?: DividerStyle
  margin?: string | number
  padding?: string | number
}

const getSpacingStyle = (padding?: string | number, margin?: string | number) => ({
  ...(padding !== undefined && {
    padding: typeof padding === 'number' ? `${padding}px` : padding,
  }),
  ...(margin !== undefined && {
    margin: typeof margin === 'number' ? `${margin}px` : margin,
  }),
})

export function Divider({
  className,
  orientation = 'horizontal',
  variant = 'solid',
  margin,
  padding,
  style,
  ...props
}: DividerProps) {
  return (
    <div
      className={clsx(
        styles.divider,
        styles[orientation],
        styles[variant],
        className,
      )}
      style={{ ...getSpacingStyle(padding, margin), ...style }}
      role="separator"
      aria-orientation={orientation}
      {...props}
    />
  )
}
