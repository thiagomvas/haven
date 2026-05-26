import { ReactNode } from 'react'
import styles from './Table.module.css'

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12'

interface TableProps {
  children: ReactNode
  striped?: boolean
  hoverable?: boolean
  bordered?: boolean
  compact?: boolean
  padding?: SpacingValue
  className?: string
}

interface TableHeadProps {
  children: ReactNode
  className?: string
}

interface TableBodyProps {
  children: ReactNode
  className?: string
}

interface TableRowProps {
  children: ReactNode
  isHeader?: boolean
  className?: string
}

interface TableCellProps {
  children: ReactNode
  align?: 'left' | 'center' | 'right'
  nowrap?: boolean
  className?: string
}

export function Table({
  children,
  striped = true,
  hoverable = true,
  bordered = false,
  compact = false,
  padding = '3',
  className = ''
}: TableProps) {
  const tableClasses = [
    styles.table,
    striped && styles.striped,
    hoverable && styles.hoverable,
    bordered && styles.bordered,
    compact && styles.compact,
    styles[`padding-${padding}`],
    className
  ]
    .filter(Boolean)
    .join(' ')

  return <table className={tableClasses}>{children}</table>
}

export function TableHead({ children, className = '' }: TableHeadProps) {
  return <thead className={`${styles.thead} ${className}`}>{children}</thead>
}

export function TableBody({ children, className = '' }: TableBodyProps) {
  return <tbody className={`${styles.tbody} ${className}`}>{children}</tbody>
}

export function TableRow({ children, isHeader = false, className = '' }: TableRowProps) {
  return (
    <tr className={`${isHeader ? styles.headerRow : styles.bodyRow} ${className}`}>{children}</tr>
  )
}

export function TableCell({
  children,
  align = 'left',
  nowrap = false,
  className = ''
}: TableCellProps) {
  const CellTag = 'td'

  return (
    <CellTag
      className={`${styles.cell} ${styles[`align-${align}`]} ${nowrap && styles.nowrap} ${className}`}
    >
      {children}
    </CellTag>
  )
}

export function TableHeader({
  children,
  align = 'left',
  nowrap = false,
  className = ''
}: TableCellProps) {
  return (
    <th
      className={`${styles.headerCell} ${styles[`align-${align}`]} ${nowrap && styles.nowrap} ${className}`}
    >
      {children}
    </th>
  )
}
