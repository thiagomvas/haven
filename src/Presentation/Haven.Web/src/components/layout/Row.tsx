import { ReactNode } from 'react';
import styles from './Row.module.css';

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12';
type AlignValue = 'flex-start' | 'center' | 'flex-end' | 'stretch';
type JustifyValue = 'flex-start' | 'center' | 'flex-end' | 'space-between' | 'space-around';

interface RowProps {
  children: ReactNode;
  gap?: SpacingValue;
  align?: AlignValue;
  justify?: JustifyValue;
  wrap?: boolean;
  full?: boolean;
  className?: string;
}

export function Row({
  children,
  gap = '4',
  align = 'center',
  justify = 'flex-start',
  wrap = false,
  full = false,
  className = '',
}: RowProps) {
  return (
    <div
      className={`${styles.row} ${styles[`gap-${gap}`]} ${styles[`align-${align}`]} ${styles[`justify-${justify}`]} ${wrap ? styles.wrap : ''} ${full ? styles.full : ''} ${className}`}
    >
      {children}
    </div>
  );
}
