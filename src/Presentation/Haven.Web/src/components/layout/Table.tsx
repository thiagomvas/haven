import { ReactNode } from 'react';

import styles from '@/styles/components/layout/Table.module.css';

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12';

interface TableProps {
  children: ReactNode;
  striped?: boolean;
  hoverable?: boolean;
  bordered?: boolean;
  compact?: boolean;
  padding?: SpacingValue;
  className?: string;
}

interface TableHeadProps {
  children: ReactNode;
  className?: string;
}

interface TableBodyProps {
  children: ReactNode;
  className?: string;
}

interface TableRowProps {
  children: ReactNode;
  isHeader?: boolean;
  className?: string;
  highlight?: boolean;
  muted?: boolean;
  onRowClick?: (event: React.MouseEvent<HTMLTableRowElement>) => void;
  actions?: ReactNode;
  hideActions?: boolean;
  hasActionsColumn?: boolean;
}

interface TableCellProps {
  children: ReactNode;
  align?: 'left' | 'center' | 'right';
  nowrap?: boolean;
  className?: string;
  variant?: 'default' | 'highlight' | 'muted' | 'mono';
}

export function Table({
  children,
  striped = true,
  hoverable = true,
  bordered = false,
  compact = false,
  padding = '3',
  className = '',
}: TableProps) {
  const tableClasses = [
    styles.table,
    striped && styles.striped,
    hoverable && styles.hoverable,
    bordered && styles.bordered,
    compact && styles.compact,
    styles[`padding-${padding}`],
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return <table className={tableClasses}>{children}</table>;
}

export function TableHead({ children, className = '' }: TableHeadProps) {
  return <thead className={`${styles.thead} ${className}`}>{children}</thead>;
}

export function TableBody({ children, className = '' }: TableBodyProps) {
  return <tbody className={`${styles.tbody} ${className}`}>{children}</tbody>;
}

export function TableRow({
  children,
  isHeader = false,
  className = '',
  highlight = false,
  muted = false,
  onRowClick,
  actions,
  hideActions = false,
  hasActionsColumn = false,
}: TableRowProps) {
  const rowClasses = [
    isHeader ? styles.headerRow : styles.bodyRow,
    highlight && styles.rowHighlight,
    muted && styles.rowMuted,
    onRowClick && styles.clickable,
    actions != null && styles.hasActions,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  const innerClasses = [styles.actionsInner, hideActions && styles.actionsHidden]
    .filter(Boolean)
    .join(' ');

  return (
    <tr className={rowClasses} onClick={onRowClick}>
      {children}
      {actions != null && (
        <td className={styles.actionsCell}>
          <div className={innerClasses}>{actions}</div>
        </td>
      )}
      {isHeader && hasActionsColumn && (
        <th className={`${styles.headerCell} ${styles.actionsCell}`} />
      )}
    </tr>
  );
}

export function TableCell({
  children,
  align = 'left',
  nowrap = false,
  className = '',
  variant = 'default',
}: TableCellProps) {
  const cellClasses = [
    styles.cell,
    styles[`align-${align}`],
    variant !== 'default' && styles[`variant-${variant}`],
    nowrap && styles.nowrap,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return <td className={cellClasses}>{children}</td>;
}

export function TableHeader({
  children,
  align = 'left',
  nowrap = false,
  className = '',
}: TableCellProps) {
  return (
    <th
      className={`${styles.headerCell} ${styles[`align-${align}`]} ${nowrap && styles.nowrap} ${className}`}
    >
      {children}
    </th>
  );
}
