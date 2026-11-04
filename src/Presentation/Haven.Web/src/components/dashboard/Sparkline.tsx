import { clsx } from 'clsx';

import styles from '@/styles/components/dashboard/Sparkline.module.css';

interface SparklineProps {
  values: number[];
  /** Fixed ceiling for the y axis; defaults to the series maximum plus headroom. */
  max?: number;
  height?: number;
  tone?: 'ok' | 'warn' | 'danger' | 'neutral';
  /** Accessible summary, since the shape alone carries no text. */
  label: string;
}

const W = 200;

/** A 24-hour trend line. Stretches to its container width; the stroke stays 1.5px. */
export function Sparkline({ values, max, height = 24, tone = 'neutral', label }: SparklineProps) {
  if (values.length < 2) return null;
  const ceiling = max ?? Math.max(...values, 1) * 1.1;
  const step = W / (values.length - 1);
  const y = (v: number) => height - 2 - (Math.min(v, ceiling) / ceiling) * (height - 4);
  const line = values
    .map((v, i) => `${i === 0 ? 'M' : 'L'}${(i * step).toFixed(1)} ${y(v).toFixed(1)}`)
    .join(' ');
  const area = `${line} L${W} ${height} L0 ${height} Z`;

  return (
    <svg
      className={clsx(styles.spark, styles[tone])}
      viewBox={`0 0 ${W} ${height}`}
      preserveAspectRatio="none"
      height={height}
      role="img"
      aria-label={label}
    >
      <path d={area} className={styles.area} />
      <path d={line} className={styles.line} />
    </svg>
  );
}
