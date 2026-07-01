import { HTMLAttributes } from 'react';

import styles from '@/styles/components/ui/Spinner.module.css';

interface SpinnerProps extends HTMLAttributes<HTMLDivElement> {
  size?: 'sm' | 'md' | 'lg';
}

export function Spinner({ size = 'md', ...props }: SpinnerProps) {
  return <div className={`${styles.spinner} ${styles[size]}`} {...props} />;
}
