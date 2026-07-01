import type { ReactNode } from 'react';

import styles from '@/styles/components/ui/EnvironmentStatusChip.module.css';
import { HealthIndicator } from './HealthIndicator';

export type EnvironmentStatus = 'running' | 'partial' | 'stopped' | 'empty';

interface EnvironmentStatusChipProps {
  name: string;
  status: EnvironmentStatus;
}

export function EnvironmentStatusChip({ name, status }: EnvironmentStatusChipProps) {
  return (
    <div className={`${styles.chip} ${styles[status]}`}>
      <HealthIndicator health={status === 'partial' ? 'degraded' : status} />
      {name}
    </div>
  );
}
