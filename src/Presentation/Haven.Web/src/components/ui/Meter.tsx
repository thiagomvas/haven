import { clsx } from 'clsx';

import styles from '@/styles/components/ui/Meter.module.css';

interface MeterProps {
  /** Utilisation, 0-100. */
  value: number;
  /** Accessible name, e.g. "Memory". */
  label: string;
  /** Switch to the warning tone at or above this value. Defaults to 75. */
  warnAt?: number;
  /** Switch to the danger tone at or above this value. Defaults to 90. */
  dangerAt?: number;
  className?: string;
}

/** A thin horizontal gauge whose tone follows the thresholds; the value is also exposed to AT. */
export function Meter({ value, label, warnAt = 75, dangerAt = 90, className }: MeterProps) {
  const clamped = Math.min(100, Math.max(0, value));
  const tone = clamped >= dangerAt ? styles.danger : clamped >= warnAt ? styles.warn : styles.ok;
  return (
    <div
      className={clsx(styles.meter, className)}
      role="meter"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(clamped)}
    >
      <div className={clsx(styles.fill, tone)} style={{ width: `${clamped}%` }} />
    </div>
  );
}

export function meterTone(value: number, warnAt = 75, dangerAt = 90): 'ok' | 'warn' | 'danger' {
  return value >= dangerAt ? 'danger' : value >= warnAt ? 'warn' : 'ok';
}
