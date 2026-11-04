import { clsx } from 'clsx';
import { HTMLAttributes, ReactNode } from 'react';

import styles from '@/styles/components/ui/Panel.module.css';

interface PanelProps extends Omit<HTMLAttributes<HTMLElement>, 'title'> {
  title?: ReactNode;
  /** Secondary text next to the title, e.g. a count or scope. */
  meta?: ReactNode;
  /** Controls aligned to the end of the header. */
  actions?: ReactNode;
  /** Remove body padding so tables and lists run edge to edge. */
  flush?: boolean;
  children: ReactNode;
}

/**
 * The standard container for a titled region of a page. Flat, hairline-bordered,
 * one header row, one body. Prefer Panel over ad-hoc card markup.
 */
export function Panel({
  title,
  meta,
  actions,
  flush = false,
  className,
  children,
  ...props
}: PanelProps) {
  return (
    <section className={clsx(styles.panel, className)} {...props}>
      {(title || actions) && (
        <header className={styles.header}>
          <h2 className={styles.title}>{title}</h2>
          {meta && <span className={styles.meta}>{meta}</span>}
          {actions && <div className={styles.actions}>{actions}</div>}
        </header>
      )}
      <div className={clsx(styles.body, flush && styles.flush)}>{children}</div>
    </section>
  );
}
