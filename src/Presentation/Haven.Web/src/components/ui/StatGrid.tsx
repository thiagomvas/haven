import { clsx } from 'clsx';
import { ReactNode } from 'react';

import styles from '@/styles/components/ui/StatGrid.module.css';

export interface StatItem {
  label: ReactNode;
  value: ReactNode;
  color?: string;
}

interface StatGridProps {
  items: StatItem[];
  columns?: number;
  className?: string;
}

export function StatGrid({ items, columns, className }: StatGridProps) {
  return (
    <div
      className={clsx(styles.grid, className)}
      style={{
        gridTemplateColumns: columns
          ? `repeat(${columns}, 1fr)`
          : 'repeat(auto-fit, minmax(var(--stat-min-width, 150px), 1fr))',
      }}
    >
      {items.map((item, index) => (
        <div key={index} className={styles.stat}>
          <div className={styles.value} style={item.color ? { color: item.color } : undefined}>
            {item.value}
          </div>
          <div className={styles.label}>{item.label}</div>
        </div>
      ))}
    </div>
  );
}
