import { ReactNode } from 'react';

import styles from '@/styles/components/ui/KeyValueList.module.css';

interface KeyValueListProps {
  children: ReactNode;
  bare?: boolean;
  className?: string;
}

interface KeyValueRowProps {
  label: string;
  children: ReactNode;
}

export function KeyValueList({ children, bare = false, className = '' }: KeyValueListProps) {
  return <div className={`${bare ? styles.bare : styles.list} ${className}`}>{children}</div>;
}

export function KeyValueRow({ label, children }: KeyValueRowProps) {
  return (
    <div className={styles.row}>
      <span className={styles.key}>{label}</span>
      <span className={styles.value}>{children}</span>
    </div>
  );
}
