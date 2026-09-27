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
}

export function StatGrid({ items, columns }: StatGridProps) {
  return (
    <div
      className={styles.grid}
      style={{ gridTemplateColumns: `repeat(${columns ?? items.length}, 1fr)` }}
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
