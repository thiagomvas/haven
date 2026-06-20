import { ReactNode } from 'react';
import styles from './Stack.module.css';

type SpacingValue = '1' | '2' | '3' | '4' | '5' | '6' | '8' | '10' | '12';
type AlignValue = 'flex-start' | 'center' | 'flex-end' | 'stretch';
type JustifyValue = 'flex-start' | 'center' | 'flex-end' | 'space-between' | 'space-around';

interface StackProps {
  children: ReactNode;
  gap?: SpacingValue;
  align?: AlignValue;
  justify?: JustifyValue;
  className?: string;
}

export function Stack({
  children,
  gap = '4',
  align = 'stretch',
  justify = 'flex-start',
  className = '',
}: StackProps) {
  return (
    <div
      className={`${styles.stack} ${styles[`gap-${gap}`]} ${styles[`align-${align}`]} ${styles[`justify-${justify}`]} ${className}`}
    >
      {children}
    </div>
  );
}
