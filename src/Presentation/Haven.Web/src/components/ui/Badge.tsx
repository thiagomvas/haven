import { clsx } from 'clsx';
import { HTMLAttributes } from 'react';

import styles from './Badge.module.css';

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: 'primary' | 'success' | 'warning' | 'danger' | 'default';
}

export function Badge({ variant = 'default', className, children, ...props }: BadgeProps) {
  return (
    <span className={clsx(styles.badge, styles[variant], className)} {...props}>
      {children}
    </span>
  );
}
