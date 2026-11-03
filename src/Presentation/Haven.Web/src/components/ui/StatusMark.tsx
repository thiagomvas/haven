import { clsx } from 'clsx';

import styles from '@/styles/components/ui/StatusMark.module.css';

/**
 * Status is always shape AND color, never color alone:
 * circle = running, diamond = degraded, triangle = fault, ring = stopped,
 * dashed ring = deploying, square = pending, dotted ring = unknown.
 */
export type MarkState =
  | 'running'
  | 'degraded'
  | 'fault'
  | 'stopped'
  | 'deploying'
  | 'pending'
  | 'unknown';

const STATE_ALIASES: Record<string, MarkState> = {
  running: 'running',
  healthy: 'running',
  degraded: 'degraded',
  partial: 'degraded',
  fault: 'fault',
  unhealthy: 'fault',
  crashed: 'fault',
  died: 'fault',
  stopped: 'stopped',
  deploying: 'deploying',
  starting: 'deploying',
  restarting: 'deploying',
  pending: 'pending',
  queued: 'pending',
  deploymentpending: 'pending',
};

export function toMarkState(health: string): MarkState {
  return STATE_ALIASES[health.toLowerCase()] ?? 'unknown';
}

interface StatusMarkProps {
  state: MarkState;
  /** Edge length in px. Defaults to 12. */
  size?: number;
  className?: string;
  title?: string;
}

export function StatusMark({ state, size = 12, className, title }: StatusMarkProps) {
  return (
    <svg
      className={clsx(styles.mark, styles[state], className)}
      width={size}
      height={size}
      viewBox="0 0 12 12"
      role={title ? 'img' : undefined}
      aria-label={title}
      aria-hidden={title ? undefined : true}
      focusable="false"
    >
      {state === 'running' && <circle cx="6" cy="6" r="5" className={styles.fill} />}
      {state === 'degraded' && <path d="M6 .8 11.2 6 6 11.2.8 6Z" className={styles.fill} />}
      {state === 'fault' && <path d="M6 .9 11.4 10.6H.6Z" className={styles.fill} />}
      {state === 'stopped' && <circle cx="6" cy="6" r="4.2" className={styles.ring} />}
      {state === 'deploying' && (
        <circle cx="6" cy="6" r="4.2" className={clsx(styles.ring, styles.dashed)} />
      )}
      {state === 'pending' && (
        <rect x="2" y="2" width="8" height="8" rx="1" className={styles.ring} />
      )}
      {state === 'unknown' && (
        <circle cx="6" cy="6" r="4.2" className={clsx(styles.ring, styles.dotted)} />
      )}
    </svg>
  );
}
